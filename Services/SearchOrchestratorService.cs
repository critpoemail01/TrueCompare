using System.Security.Cryptography;
using System.Text;
using TrueCompare.Models;

namespace TrueCompare.Services;

public sealed class SearchOrchestratorService(
    IProductDiscoveryService discoveryService,
    IProductSuggestionService suggestionService,
    IStoreOfferValidationService storeOfferValidator,
    SearchQuotaService quotaService,
    SearchResultSnapshotService snapshotService,
    ILogger<SearchOrchestratorService> logger,
    AppText text)
{
    public async Task<SearchExecutionResult> ExecuteAsync(
        string userId,
        string query,
        string? requestId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedQuery = NormalizeQuery(query);
        var rawIdempotencyKey = BuildSearchIdempotencyKey(userId, normalizedQuery, requestId);
        var storedIdempotencyKey = SearchQuotaService.NormalizeIdempotencyKeyForStorage(rawIdempotencyKey, normalizedQuery);

        var snapshot = await snapshotService.GetByIdempotencyKeyAsync(
            userId,
            storedIdempotencyKey,
            normalizedQuery,
            cancellationToken)
            ?? await snapshotService.GetLatestForQueryAsync(userId, normalizedQuery, cancellationToken);
        if (snapshot is not null)
        {
            return snapshot;
        }

        SearchReservationResult? reservation = null;
        var committed = false;
        try
        {
            reservation = await quotaService.TryReserveAsync(
                userId,
                normalizedQuery,
                rawIdempotencyKey,
                cancellationToken);
            if (!reservation.Allowed)
            {
                return SearchExecutionResult.Blocked(normalizedQuery, reservation.Status);
            }

            var discovery = await discoveryService.DiscoverAsync(normalizedQuery, cancellationToken);
            var validatedProducts = await ValidateResultProductsAsync(
                normalizedQuery,
                discovery.Products,
                cancellationToken);

            var processedSummaries = validatedProducts
                .Select((item, index) => new SearchProductOfferSummary(
                    ApplyResultOfferPrice(item.Product, item.Offers) with { Rank = index + 1 },
                    item.CandidateOffers,
                    item.Offers))
                .ToList();
            var productOfferSummaries = processedSummaries
                .GroupBy(item => item.Product.Slug, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
            var products = processedSummaries
                .Select(item => item.Product)
                .ToList();
            var offers = processedSummaries
                .SelectMany(item => item.Offers)
                .ToList();

            if (products.Count == 0)
            {
                await quotaService.RefundReservationAsync(
                    reservation.ReservationId,
                    "No products were available for the requested query.",
                    cancellationToken);

                return new SearchExecutionResult(
                    Allowed: true,
                    Query: normalizedQuery,
                    Products: Array.Empty<ProductResult>(),
                    Offers: Array.Empty<SellerOffer>(),
                    ProductOfferSummaries: new Dictionary<string, SearchProductOfferSummary>(StringComparer.OrdinalIgnoreCase),
                    AiSuggestions: null,
                    Message: text.Pick(
                        "Esta pesquisa não foi debitada porque não encontrei resultados úteis.",
                        "This search was not charged because no useful results were found."),
                    SearchRequestId: reservation.ReservationId,
                    WasCharged: false,
                    WasRefunded: true,
                    IsFromSnapshot: false);
            }

            LlmSuggestionResult? suggestions = null;
            try
            {
                suggestions = await suggestionService.GetSuggestionsAsync(normalizedQuery, products, offers, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Failed to build AI suggestions for results query {Query}.", normalizedQuery);
            }

            if (!string.IsNullOrWhiteSpace(storedIdempotencyKey))
            {
                try
                {
                    await snapshotService.SaveAsync(
                        userId,
                        reservation.ReservationId,
                        normalizedQuery,
                        storedIdempotencyKey,
                        products,
                        offers,
                        productOfferSummaries,
                        suggestions,
                        cancellationToken: cancellationToken);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    logger.LogWarning(
                        exception,
                        "Failed to persist search result snapshot for query {Query}; refunding the reserved quota before returning live results.",
                        normalizedQuery);

                    await quotaService.RefundReservationAsync(
                        reservation.ReservationId,
                        "Search result snapshot failed before quota commit.",
                        CancellationToken.None);

                    return new SearchExecutionResult(
                        Allowed: true,
                        Query: normalizedQuery,
                        Products: products,
                        Offers: offers,
                        ProductOfferSummaries: productOfferSummaries,
                        AiSuggestions: suggestions,
                        Message: text.Pick(
                            "A pesquisa gerou resultados, mas não foi debitada porque não consegui guardar o snapshot para reabrir depois.",
                            "The search produced results, but was not charged because the result snapshot could not be saved for replay."),
                        SearchRequestId: reservation.ReservationId,
                        WasCharged: false,
                        WasRefunded: true,
                        IsFromSnapshot: false);
                }
            }

            await quotaService.CommitReservationAsync(reservation.ReservationId, products.Count, cancellationToken);
            committed = true;

            return new SearchExecutionResult(
                Allowed: true,
                Query: normalizedQuery,
                Products: products,
                Offers: offers,
                ProductOfferSummaries: productOfferSummaries,
                AiSuggestions: suggestions,
                Message: null,
                SearchRequestId: reservation.ReservationId,
                WasCharged: true,
                WasRefunded: false,
                IsFromSnapshot: false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            if (!committed && reservation is not null)
            {
                await quotaService.RefundReservationAsync(
                    reservation.ReservationId,
                    "Search failed before useful results were produced.",
                    CancellationToken.None);
            }

            logger.LogError(exception, "Failed to execute results query {Query}.", normalizedQuery);
            return new SearchExecutionResult(
                Allowed: true,
                Query: normalizedQuery,
                Products: Array.Empty<ProductResult>(),
                Offers: Array.Empty<SellerOffer>(),
                ProductOfferSummaries: new Dictionary<string, SearchProductOfferSummary>(StringComparer.OrdinalIgnoreCase),
                AiSuggestions: null,
                Message: text.Pick(
                    "Esta pesquisa falhou antes de gerar resultados úteis e não foi debitada.",
                    "This search failed before useful results were produced and was not charged."),
                SearchRequestId: reservation?.ReservationId,
                WasCharged: false,
                WasRefunded: true,
                IsFromSnapshot: false);
        }
    }

    public static string BuildSearchIdempotencyKey(string userId, string query, string? requestId = null)
    {
        var normalizedQuery = NormalizeQuery(query);
        if (!string.IsNullOrWhiteSpace(requestId))
        {
            var safeRequestId = requestId.Trim();
            var requestHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedQuery.ToUpperInvariant())));
            return $"results:{userId}:{safeRequestId}:{requestHash[..16]}";
        }

        var directWindow = DateTime.UtcNow.ToString("yyyyMMddHHmm", System.Globalization.CultureInfo.InvariantCulture);
        var raw = $"{userId}|{normalizedQuery.ToUpperInvariant()}|{directWindow}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
        return $"results:direct:{hash[..32]}";
    }

    public static string NormalizeQuery(string query)
    {
        return query.Trim();
    }

    private async Task<IReadOnlyList<SearchProductOfferSummary>> ValidateResultProductsAsync(
        string query,
        IReadOnlyList<ProductResult> products,
        CancellationToken cancellationToken)
    {
        var validations = await Task.WhenAll(products.Select(async product =>
        {
            IReadOnlyList<SellerOffer> candidateOffers = Array.Empty<SellerOffer>();
            try
            {
                candidateOffers = (await discoveryService.GetSellerOffersAsync(product.Slug, query, cancellationToken))
                    .Where(IsUsefulStoreCandidate)
                    .Take(6)
                    .ToList();

                var directOffers = candidateOffers
                    .Where(ComparisonDataService.IsConfirmedStoreOffer)
                    .ToList();

                var validatedOffers = await storeOfferValidator.ValidateConfirmedOffersAsync(product, directOffers, cancellationToken);
                var displayOffers = MergeValidatedOffersWithStoreCandidates(validatedOffers, candidateOffers);
                return new SearchProductOfferSummary(product, candidateOffers, displayOffers);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "Failed to validate store offers for product {ProductSlug}.", product.Slug);
                return new SearchProductOfferSummary(product, candidateOffers, candidateOffers);
            }
        }));

        return validations.ToList();
    }

    private static IReadOnlyList<SellerOffer> MergeValidatedOffersWithStoreCandidates(
        IReadOnlyList<SellerOffer> validatedOffers,
        IReadOnlyList<SellerOffer> candidateOffers)
    {
        var merged = new List<SellerOffer>();
        foreach (var offer in validatedOffers.OrderByDescending(offer => offer.IsLiveValidated).ThenBy(offer => offer.PriceCents))
        {
            merged.Add(offer);
        }

        foreach (var candidate in candidateOffers)
        {
            if (merged.Any(offer => offer.Seller.Equals(candidate.Seller, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            merged.Add(candidate with
            {
                Preferred = false,
                ValidationState = candidate.IsLivePrice
                    ? OfferPriceValidationState.ValidationFailed
                    : OfferPriceValidationState.PendingValidation
            });
        }

        var preferred = merged
            .OrderByDescending(offer => offer.IsLiveValidated)
            .ThenByDescending(offer => offer.Preferred)
            .ThenBy(offer => offer.IsLiveValidated ? offer.PriceCents : long.MaxValue)
            .ThenByDescending(offer => offer.ReliabilityScore)
            .FirstOrDefault();

        return merged
            .Select(offer => offer with { Preferred = preferred is not null && offer.Seller.Equals(preferred.Seller, StringComparison.OrdinalIgnoreCase) })
            .Take(6)
            .ToList();
    }

    private static bool IsUsefulStoreCandidate(SellerOffer offer)
    {
        if (string.IsNullOrWhiteSpace(offer.Seller) || string.IsNullOrWhiteSpace(offer.Url))
        {
            return false;
        }

        if (!Uri.TryCreate(offer.Url, UriKind.Absolute, out var parsed))
        {
            return false;
        }

        return parsed.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || parsed.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase);
    }

    private static ProductResult ApplyResultOfferPrice(ProductResult product, IReadOnlyList<SellerOffer> offers)
    {
        var bestOffer = offers
            .Where(offer => offer.IsLiveValidated)
            .OrderByDescending(offer => offer.Preferred)
            .ThenBy(offer => offer.PriceCents)
            .ThenByDescending(offer => offer.ReliabilityScore)
            .FirstOrDefault();

        return bestOffer is null ? product : product with { Price = bestOffer.Price };
    }
}

public sealed record SearchProductOfferSummary(
    ProductResult Product,
    IReadOnlyList<SellerOffer> CandidateOffers,
    IReadOnlyList<SellerOffer> Offers);

public sealed record SearchExecutionResult(
    bool Allowed,
    string Query,
    IReadOnlyList<ProductResult> Products,
    IReadOnlyList<SellerOffer> Offers,
    IReadOnlyDictionary<string, SearchProductOfferSummary> ProductOfferSummaries,
    LlmSuggestionResult? AiSuggestions,
    string? Message,
    int? SearchRequestId,
    bool WasCharged,
    bool WasRefunded,
    bool IsFromSnapshot)
{
    public SearchQuotaStatus? QuotaStatus { get; init; }

    public static SearchExecutionResult Blocked(string query, SearchQuotaStatus quotaStatus)
    {
        return new SearchExecutionResult(
            Allowed: false,
            Query: query,
            Products: Array.Empty<ProductResult>(),
            Offers: Array.Empty<SellerOffer>(),
            ProductOfferSummaries: new Dictionary<string, SearchProductOfferSummary>(StringComparer.OrdinalIgnoreCase),
            AiSuggestions: null,
            Message: null,
            SearchRequestId: null,
            WasCharged: false,
            WasRefunded: false,
            IsFromSnapshot: false)
        {
            QuotaStatus = quotaStatus
        };
    }
}
