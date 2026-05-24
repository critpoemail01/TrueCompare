using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TrueCompare.Models;
using TrueCompare.Options;

namespace TrueCompare.Services;

public sealed class StoreOfferValidationService(
    HttpClient httpClient,
    IMemoryCache cache,
    StoreValidationLimiter validationLimiter,
    ILogger<StoreOfferValidationService> logger,
    IOptions<StoreOfferValidationOptions>? optionsAccessor = null) : IStoreOfferValidationService
{
    private static readonly TimeSpan ValidCacheDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan InvalidCacheDuration = TimeSpan.FromMinutes(2);

    private readonly StoreOfferValidationOptions options = optionsAccessor?.Value ?? new();

    public async Task<IReadOnlyList<SellerOffer>> ValidateConfirmedOffersAsync(
        ProductResult? product,
        IReadOnlyList<SellerOffer> offers,
        CancellationToken cancellationToken = default)
    {
        if (product is null || offers.Count == 0)
        {
            return Array.Empty<SellerOffer>();
        }

        var maxOffers = Math.Clamp(options.MaxOffersPerProduct, 1, 12);
        var directOffers = offers
            .Where(ComparisonDataService.IsConfirmedStoreOffer)
            .OrderBy(offer => offer.PriceCents)
            .ThenByDescending(offer => offer.ReliabilityScore)
            .Take(maxOffers)
            .ToList();
        if (directOffers.Count == 0)
        {
            return Array.Empty<SellerOffer>();
        }

        using var semaphore = new SemaphoreSlim(Math.Clamp(options.MaxConcurrentRequests, 1, 8));
        var validations = await Task.WhenAll(
            directOffers.Select(offer => ValidateOfferWithConcurrencyAsync(product, offer, semaphore, cancellationToken)));

        var validOffers = validations
            .Where(offer => offer is not null)
            .Cast<SellerOffer>()
            .ToList();
        if (validOffers.Count == 0)
        {
            return Array.Empty<SellerOffer>();
        }

        var preferred = validOffers
            .Where(offer => offer.IsLivePrice && offer.ReliabilityScore >= 80)
            .OrderBy(offer => offer.PriceCents)
            .ThenByDescending(offer => offer.ReliabilityScore)
            .FirstOrDefault();

        return validOffers
            .Select(offer => offer with { Preferred = preferred is not null && offer.Seller == preferred.Seller })
            .OrderByDescending(offer => offer.Preferred)
            .ThenBy(offer => offer.PriceCents)
            .ThenByDescending(offer => offer.ReliabilityScore)
            .ToList();
    }

    private async Task<SellerOffer?> ValidateOfferWithConcurrencyAsync(
        ProductResult product,
        SellerOffer offer,
        SemaphoreSlim semaphore,
        CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            return await ValidateOfferAsync(product, offer, cancellationToken);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task<SellerOffer?> ValidateOfferAsync(
        ProductResult product,
        SellerOffer offer,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"store-offer-validation:{product.Slug}:{offer.Seller}:{offer.Url}";
        if (cache.TryGetValue(cacheKey, out OfferValidationResult? cached) && cached is not null)
        {
            return cached.Offer;
        }

        var validation = await ValidateOfferUncachedAsync(product, offer, cancellationToken);
        cache.Set(cacheKey, validation, validation.IsValid ? ValidCacheDuration : InvalidCacheDuration);
        return validation.Offer;
    }

    private async Task<OfferValidationResult> ValidateOfferUncachedAsync(
        ProductResult product,
        SellerOffer offer,
        CancellationToken cancellationToken)
    {
        Uri? offerUri = null;
        try
        {
            if (!Uri.TryCreate(offer.Url, UriKind.Absolute, out offerUri))
            {
                logger.LogWarning(
                    "Store offer rejected {Seller} for {ProductSlug}: URL is not absolute. Url={Url}",
                    offer.Seller,
                    product.Slug,
                    offer.Url);
                return OfferValidationResult.Invalid;
            }

            if (!IsSafeStoreUri(offerUri))
            {
                logger.LogWarning(
                    "Store offer rejected {Seller} for {ProductSlug}: URL host or scheme is not allowed. Url={Url}",
                    offer.Seller,
                    product.Slug,
                    offer.Url);
                return OfferValidationResult.Invalid;
            }

            var lease = await validationLimiter.TryAcquireAsync(offerUri, cancellationToken);
            if (lease is null)
            {
                logger.LogWarning(
                    "Store offer validation skipped for {Seller} and {ProductSlug}: domain backoff is active.",
                    offer.Seller,
                    product.Slug);
                return OfferValidationResult.Invalid;
            }

            using (lease)
            using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(options.RequestTimeoutSeconds, 2, 30)));

                using var request = BuildValidationRequest(offerUri);
                using var response = await httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    timeout.Token);

                if (!response.IsSuccessStatusCode)
                {
                    validationLimiter.RecordDomainFailure(offerUri);
                    logger.LogWarning(
                        "Store offer rejected {Seller} for {ProductSlug}: HTTP {StatusCode}.",
                        offer.Seller,
                        product.Slug,
                        (int)response.StatusCode);
                    return OfferValidationResult.Invalid;
                }

                validationLimiter.RecordDomainSuccess(offerUri);

                var maxResponseBytes = Math.Clamp(options.MaxResponseBytes, 32_768, 2_000_000);
                if (response.Content.Headers.ContentLength.HasValue
                    && response.Content.Headers.ContentLength.Value > maxResponseBytes)
                {
                    logger.LogWarning(
                        "Store offer rejected {Seller} for {ProductSlug}: response was too large ({ContentLength} bytes).",
                        offer.Seller,
                        product.Slug,
                        response.Content.Headers.ContentLength);
                    return OfferValidationResult.Invalid;
                }

                var finalUri = response.RequestMessage?.RequestUri ?? offerUri;
                if (!IsExpectedFinalStoreUri(offerUri, finalUri))
                {
                    logger.LogWarning(
                        "Store offer rejected {Seller} for {ProductSlug}: final URL host changed unexpectedly. InitialHost={InitialHost} FinalHost={FinalHost}",
                        offer.Seller,
                        product.Slug,
                        offerUri.Host,
                        finalUri.Host);
                    return OfferValidationResult.Invalid;
                }

                var finalUrl = finalUri.ToString();
                if (!ComparisonDataService.HasDirectProductUrl(finalUrl))
                {
                    logger.LogWarning(
                        "Store offer rejected {Seller} for {ProductSlug}: final URL is not a direct product page. FinalUrl={FinalUrl}",
                        offer.Seller,
                        product.Slug,
                        finalUrl);
                    return OfferValidationResult.Invalid;
                }

                var content = await ReadLimitedContentAsync(response, timeout.Token);
                if (content.Length == 0)
                {
                    logger.LogWarning(
                        "Store offer rejected {Seller} for {ProductSlug}: response body was empty.",
                        offer.Seller,
                        product.Slug);
                    return OfferValidationResult.Invalid;
                }

                if (!ContainsRequiredProductTerms(product, content))
                {
                    logger.LogWarning(
                        "Store offer rejected {Seller} for {ProductSlug}: product terms were not found on the page. FinalUrl={FinalUrl}",
                        offer.Seller,
                        product.Slug,
                        finalUrl);
                    return OfferValidationResult.Invalid;
                }

                var validatedUtc = DateTime.UtcNow;
                var currentPriceCents = TryExtractCurrentProductPriceCents(content);
                if (currentPriceCents is > 0)
                {
                    return new OfferValidationResult(true, offer with
                    {
                        Price = FormatCurrency(currentPriceCents.Value),
                        PriceCents = currentPriceCents.Value,
                        IsLivePrice = true,
                        ValidationState = OfferPriceValidationState.LiveValidated,
                        ValidatedUtc = validatedUtc
                    });
                }

                if (!ContainsExpectedPrice(content, offer.PriceCents))
                {
                    logger.LogWarning(
                        "Store offer rejected {Seller} for {ProductSlug}: expected price {PriceCents} was not found. FinalUrl={FinalUrl}",
                        offer.Seller,
                        product.Slug,
                        offer.PriceCents,
                        finalUrl);
                    return OfferValidationResult.Invalid;
                }

                return new OfferValidationResult(true, offer with
                {
                    IsLivePrice = true,
                    ValidationState = OfferPriceValidationState.LiveValidated,
                    ValidatedUtc = validatedUtc
                });
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            if (offerUri is not null)
            {
                validationLimiter.RecordDomainFailure(offerUri);
            }

            logger.LogWarning(
                "Store offer validation timed out for {Seller} and {ProductSlug}.",
                offer.Seller,
                product.Slug);
            return OfferValidationResult.Invalid;
        }
        catch (Exception ex)
        {
            if (offerUri is not null)
            {
                validationLimiter.RecordDomainFailure(offerUri);
            }

            logger.LogWarning(
                ex,
                "Store offer validation failed for {Seller} and {ProductSlug}.",
                offer.Seller,
                product.Slug);
            return OfferValidationResult.Invalid;
        }
    }

    private static HttpRequestMessage BuildValidationRequest(Uri url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd("Mozilla/5.0 TrueCompare/1.0");
        request.Headers.AcceptLanguage.ParseAdd("pt-PT,pt;q=0.9,en;q=0.8");
        return request;
    }

    private static bool IsSafeStoreUri(Uri uri)
    {
        if (!uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            && !uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (uri.IsLoopback || uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !IPAddress.TryParse(uri.Host, out var address) || !IsPrivateOrLocalAddress(address);
    }

    private static bool IsExpectedFinalStoreUri(Uri originalUri, Uri finalUri)
    {
        return IsSafeStoreUri(finalUri)
            && NormalizeHost(originalUri.Host).Equals(NormalizeHost(finalUri.Host), StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeHost(string host)
    {
        return host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? host[4..] : host;
    }

    private static bool IsPrivateOrLocalAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
        {
            var ipv6Bytes = address.GetAddressBytes();
            return address.IsIPv6LinkLocal
                || address.IsIPv6SiteLocal
                || (ipv6Bytes.Length > 0 && (ipv6Bytes[0] == 0xFC || ipv6Bytes[0] == 0xFD));
        }

        var bytes = address.GetAddressBytes();
        return bytes[0] == 10
            || (bytes[0] == 172 && bytes[1] is >= 16 and <= 31)
            || (bytes[0] == 192 && bytes[1] == 168)
            || (bytes[0] == 169 && bytes[1] == 254);
    }

    private async Task<string> ReadLimitedContentAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var maxBytes = Math.Clamp(options.MaxResponseBytes, 32_768, 2_000_000);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var memoryStream = new MemoryStream(capacity: Math.Min(maxBytes, 64 * 1024));
        var buffer = new byte[8192];
        var totalBytes = 0;

        while (true)
        {
            var bytesRead = await stream.ReadAsync(buffer, cancellationToken);
            if (bytesRead == 0)
            {
                break;
            }

            totalBytes += bytesRead;
            if (totalBytes > maxBytes)
            {
                throw new InvalidOperationException($"Store response exceeded the configured {maxBytes} byte limit.");
            }

            memoryStream.Write(buffer, 0, bytesRead);
        }

        return Encoding.UTF8.GetString(memoryStream.ToArray());
    }

    private static long? TryExtractCurrentProductPriceCents(string content)
    {
        var decoded = WebUtility.HtmlDecode(content);
        foreach (var pattern in CurrentProductPricePatterns)
        {
            var match = pattern.Match(decoded);
            if (!match.Success)
            {
                continue;
            }

            var price = ParsePriceCents(match.Groups["price"].Value);
            if (price > 0)
            {
                return price;
            }
        }

        return null;
    }

    private static bool ContainsRequiredProductTerms(ProductResult product, string html)
    {
        var haystack = NormalizeSearchText(WebUtility.HtmlDecode(html));
        var requiredGroups = BuildRequiredTermGroups(product).ToList();

        return requiredGroups.Count > 0
            && requiredGroups.All(group => group.Any(term => ContainsNormalizedTerm(haystack, term)));
    }

    private static IEnumerable<IReadOnlyList<string>> BuildRequiredTermGroups(ProductResult product)
    {
        var slug = NormalizeSearchText(product.Slug);
        if (slug.Contains("playstation 5 slim", StringComparison.OrdinalIgnoreCase))
        {
            yield return ["playstation", "ps5"];
            yield return ["slim"];
            yield break;
        }

        if (slug.Contains("apple usb c 20w", StringComparison.OrdinalIgnoreCase))
        {
            yield return ["apple"];
            yield return ["usb c", "usb-c"];
            yield return ["20w", "20 w"];
            yield break;
        }

        if (slug.Contains("iphone 17", StringComparison.OrdinalIgnoreCase)
            && !slug.Contains("pro", StringComparison.OrdinalIgnoreCase)
            && !slug.Contains("max", StringComparison.OrdinalIgnoreCase))
        {
            yield return ["iphone"];
            yield return ["17"];
            yield return ["6 3", "6.3"];
            yield break;
        }

        if (slug.Contains("bosch serie 6 frigorifico", StringComparison.OrdinalIgnoreCase))
        {
            yield return ["bosch"];
            yield return ["kgn39aiat", "kgn39"];
            yield return ["frigorifico", "frigorífico", "combinado"];
            yield break;
        }

        if (slug.Contains("canon eos r100", StringComparison.OrdinalIgnoreCase))
        {
            yield return ["canon"];
            yield return ["eos"];
            yield return ["r100"];
            yield break;
        }

        if (slug.Contains("samsung galaxy watch8", StringComparison.OrdinalIgnoreCase))
        {
            yield return ["samsung"];
            yield return ["galaxy"];
            yield return ["watch"];
            yield return ["8"];
            yield break;
        }

        if (slug.Contains("samsung galaxy fit3", StringComparison.OrdinalIgnoreCase))
        {
            yield return ["samsung"];
            yield return ["galaxy"];
            yield return ["fit"];
            yield return ["3"];
            yield break;
        }

        if (slug.Contains("xiaomi redmi watch 5 active", StringComparison.OrdinalIgnoreCase))
        {
            yield return ["xiaomi"];
            yield return ["redmi"];
            yield return ["watch"];
            yield return ["5"];
            yield return ["active"];
            yield break;
        }

        if (slug.Contains("xiaomi 4 lite 2nd gen trotinete", StringComparison.OrdinalIgnoreCase))
        {
            yield return ["xiaomi"];
            yield return ["4"];
            yield return ["lite"];
            yield return ["trotinete", "scooter"];
            yield break;
        }

        if (slug.Contains("livro atomic habits", StringComparison.OrdinalIgnoreCase))
        {
            yield return ["habitos", "hábitos", "atomic", "atomicos", "atómicos"];
            yield return ["9789897841741", "livro"];
            yield break;
        }

        if (slug.Contains("teka mw fs20", StringComparison.OrdinalIgnoreCase)
            && slug.Contains("microondas", StringComparison.OrdinalIgnoreCase))
        {
            yield return ["teka"];
            yield return ["fs20"];
            yield return ["microondas", "micro ondas", "micro-ondas"];
            if (slug.Contains("bk", StringComparison.OrdinalIgnoreCase))
            {
                yield return ["bk", "preto"];
            }
            else if (slug.Contains("wh", StringComparison.OrdinalIgnoreCase))
            {
                yield return ["wh", "branco"];
            }

            yield break;
        }

        var tokens = slug
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(IsUsefulProductToken)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(4)
            .Select(token => (IReadOnlyList<string>)[token]);

        foreach (var group in tokens)
        {
            yield return group;
        }
    }

    private static bool IsUsefulProductToken(string token)
    {
        if (token.Length < 3 && !token.Any(char.IsDigit))
        {
            return false;
        }

        return token is not "apple"
            and not "store"
            and not "power"
            and not "adapter"
            and not "lightspeed"
            and not "wireless";
    }

    private static bool ContainsExpectedPrice(string content, long priceCents)
    {
        var decoded = WebUtility.HtmlDecode(content);
        var euros = priceCents / 100;
        var cents = priceCents % 100;
        var commaPrice = string.Create(CultureInfo.InvariantCulture, $"{euros},{cents:00}");
        var dotPrice = string.Create(CultureInfo.InvariantCulture, $"{euros}.{cents:00}");

        return ContainsDisplayedPrice(decoded, commaPrice)
            || ContainsDisplayedPrice(decoded, dotPrice)
            || Regex.IsMatch(
                decoded,
                $"\"(?:price|price_min|price_max)\"\\s*:\\s*{priceCents}\\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static bool ContainsDisplayedPrice(string decodedContent, string price)
    {
        var escapedPrice = Regex.Escape(price);
        return Regex.IsMatch(
            decodedContent,
            $@"(?:\u20AC\s*{escapedPrice}|{escapedPrice}\s*(?:\u20AC|EUR)|(?:value|data-total)\s*=\s*[""']{escapedPrice}[""'])",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static long ParsePriceCents(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        var amount = value.Trim();
        var hasDecimalSeparator = amount.Contains(',', StringComparison.Ordinal)
            || amount.Contains('.', StringComparison.Ordinal);
        if (!hasDecimalSeparator
            && amount.Length >= 5
            && long.TryParse(amount, NumberStyles.Integer, CultureInfo.InvariantCulture, out var centsAmount))
        {
            return centsAmount;
        }

        if (amount.Contains(',', StringComparison.Ordinal))
        {
            amount = amount.Replace(".", string.Empty).Replace(',', '.');
        }
        else if (amount.Count(character => character == '.') > 1)
        {
            var lastDot = amount.LastIndexOf('.');
            amount = amount[..lastDot].Replace(".", string.Empty) + amount[lastDot..];
        }

        return decimal.TryParse(amount, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? (long)Math.Round(parsed * 100m, MidpointRounding.AwayFromZero)
            : 0;
    }

    private static string FormatCurrency(long cents)
    {
        return CultureInfo.CurrentUICulture.Name.Equals("en-US", StringComparison.OrdinalIgnoreCase)
            || CultureInfo.CurrentUICulture.Name.EndsWith("-US", StringComparison.OrdinalIgnoreCase)
            ? (cents / 100m).ToString("C", CultureInfo.GetCultureInfo("en-US"))
            : $"{(cents / 100m).ToString("N2", CultureInfo.CurrentCulture)} \u20AC";
    }

    private static bool ContainsNormalizedTerm(string haystack, string term)
    {
        var normalizedTerm = NormalizeSearchText(term);
        return haystack.Contains(normalizedTerm, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeSearchText(string value)
    {
        var decoded = WebUtility.HtmlDecode(value).ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decoded.Length);

        foreach (var character in decoded)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(char.IsLetterOrDigit(character) ? character : ' ');
        }

        return string.Join(
            ' ',
            builder
                .ToString()
                .Normalize(NormalizationForm.FormC)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private static readonly Regex[] CurrentProductPricePatterns =
    [
        new(
            @"""offers""\s*:\s*\{(?:(?!\}\s*,?\s*""sku"").)*?""price""\s*:\s*""?(?<price>\d+(?:[,.]\d{1,2})?)""?",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.Compiled),
        new(
            "Pre(?:\\u00e7|\\u00c3\\u00a7|c)o\\s+de\\s+saldo.{0,350}?(?<price>\\d{1,6}(?:[,.]\\d{2}))\\s*(?:\\u20AC|EUR)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.Compiled),
        new(
            @"class\s*=\s*[""'][^""']*scx-price--line[^""']*regular[^""']*[""'][^>]*>\s*(?<price>\d{1,6}(?:[,.]\d{2}))\s*(?:\u20AC|EUR)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.Compiled),
        new(
            @"product-opportunity-new-price[^>]*>\s*(?<price>\d{1,6}(?:[,.]\d{2}))\s*(?:\u20AC|EUR)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.Compiled),
        new(
            @"data-qa\s*=\s*[""']product-unit-price-value[""'][^>]*>\s*(?<price>\d{1,6}(?:[,.]\d{2}))\s*(?:\u20AC|EUR)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.Compiled),
        new(
            @"itemprop\s*=\s*[""']price[""'][^>]*(?:content|value)\s*=\s*[""'](?<price>\d+(?:[,.]\d{1,2})?)[""']",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.Compiled)
    ];

    private sealed record OfferValidationResult(bool IsValid, SellerOffer? Offer)
    {
        public static OfferValidationResult Invalid { get; } = new(false, null);
    }
}
