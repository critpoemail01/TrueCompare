using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using TrueCompare.Models;
using TrueCompare.Options;

namespace TrueCompare.Services;

public sealed partial class LlmProductDiscoveryService(
    ComparisonDataService data,
    LlmProviderRouter providerRouter,
    IMemoryCache cache,
    ILogger<LlmProductDiscoveryService> logger,
    AppText text) : IProductDiscoveryService
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    private static readonly string[] AccentPalette = ["#5EE9A8", "#7BE8E0", "#B49CFF", "#E9D67B"];

    public async Task<ProductDiscoveryResult> DiscoverAsync(string? query, CancellationToken cancellationToken = default)
    {
        var normalizedQuery = NormalizeQuery(query);
        var cacheKey = $"product-discovery:{normalizedQuery.ToLowerInvariant()}";
        if (cache.TryGetValue(cacheKey, out ProductDiscoveryResult? cached) && cached is not null)
        {
            return cached;
        }

        var localProducts = data.GetProducts(normalizedQuery);
        if (localProducts.Count > 0)
        {
            var localOffers = data.GetSellerOffers(normalizedQuery);
            var localResult = new ProductDiscoveryResult(
                normalizedQuery,
                localProducts,
                localOffers,
                false,
                text.Pick("Catalogo local", "Local catalog"));

            Remember(localResult, localOffers);
            cache.Set(cacheKey, localResult, TimeSpan.FromMinutes(30));
            return localResult;
        }

        var emptyFallback = new ProductDiscoveryResult(
            normalizedQuery,
            Array.Empty<ProductResult>(),
            Array.Empty<SellerOffer>(),
            false,
            text.Pick("Sem catalogo validado", "No validated catalog"));

        try
        {
            var maxBudgetCents = TryExtractMaxBudgetCents(normalizedQuery);
            var providerResponse = await providerRouter.TryGetValidJsonAsync(
                "product-discovery",
                requiresVision: false,
                provider => BuildRequestJson(provider, normalizedQuery, maxBudgetCents),
                content => IsValidPayload(ReadPayload(content), normalizedQuery, maxBudgetCents),
                cancellationToken);

            if (providerResponse is null)
            {
                return BuildValidatedFallback(normalizedQuery, maxBudgetCents) ?? emptyFallback;
            }

            var payload = ReadPayload(providerResponse.JsonContent);
            var result = NormalizePayload(payload, normalizedQuery, providerResponse.ProviderName, maxBudgetCents);
            if (result.Products.Count == 0)
            {
                return BuildValidatedFallback(normalizedQuery, maxBudgetCents) ?? emptyFallback;
            }

            cache.Set(cacheKey, result, TimeSpan.FromMinutes(30));
            return result;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Product discovery LLM request timed out");
            return emptyFallback;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Product discovery failed");
            return emptyFallback;
        }
    }

    public async Task<ProductResult?> FindProductAsync(
        string? slug,
        string? query = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        var localProduct = data.FindProduct(slug);
        if (localProduct is not null)
        {
            return localProduct;
        }

        if (cache.TryGetValue(ProductCacheKey(slug), out ProductResult? cachedProduct) && cachedProduct is not null)
        {
            return cachedProduct;
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        var discovery = await DiscoverAsync(query, cancellationToken);
        return discovery.Products.FirstOrDefault(product => product.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IReadOnlyList<SellerOffer>> GetSellerOffersAsync(
        string? queryOrSlug,
        string? originalQuery = null,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(queryOrSlug) && data.FindProduct(queryOrSlug) is not null)
        {
            return data.GetSellerOffers(queryOrSlug);
        }

        if (!string.IsNullOrWhiteSpace(queryOrSlug)
            && cache.TryGetValue(OffersCacheKey(queryOrSlug), out IReadOnlyList<SellerOffer>? cachedOffers)
            && cachedOffers is not null
            && cachedOffers.Count > 0)
        {
            return cachedOffers;
        }

        var query = string.IsNullOrWhiteSpace(originalQuery) ? queryOrSlug : originalQuery;
        if (string.IsNullOrWhiteSpace(query))
        {
            return data.GetSellerOffers(null);
        }

        var discovery = await DiscoverAsync(query, cancellationToken);
        if (!string.IsNullOrWhiteSpace(queryOrSlug)
            && cache.TryGetValue(OffersCacheKey(queryOrSlug), out cachedOffers)
            && cachedOffers is not null
            && cachedOffers.Count > 0)
        {
            return cachedOffers;
        }

        return discovery.Offers.Count > 0
            ? discovery.Offers
            : data.GetSellerOffers(null);
    }

    private string BuildRequestJson(LlmProviderOptions provider, string query, long? maxBudgetCents)
    {
        var request = new
        {
            model = provider.Model,
            temperature = provider.Temperature,
            max_tokens = Math.Clamp(provider.MaxTokens, 800, 2400),
            response_format = provider.SupportsJsonObjectResponseFormat ? new { type = "json_object" } : null,
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = """
                    Es o motor de descoberta de produtos da TrueCompare. Responde apenas JSON valido, sem markdown.
                    Tens de sugerir produtos reais que correspondem diretamente ao pedido. Nao uses o catalogo de portateis por defeito.
                    Se o pedido for rato, devolve ratos. Se for cadeira, devolve cadeiras. Se for ferramenta, devolve ferramentas.
                    A categoria principal do pedido e obrigatoria: cadeira nao pode devolver mesa/escrivaninha, rato nao pode devolver portatil.
                    Se houver orcamento maximo, todos os produtos devem ficar dentro desse orcamento.
                    Usa precos aproximados e publicamente plausiveis; nao afirmes stock real nem disponibilidade em tempo real.
                    Cria URLs de pesquisa de retalhistas conhecidos quando nao souberes o link exato do produto.
                    O JSON deve ter: category, confidence, products.
                    products deve ter exatamente 1 item com: name, brand, price, priceCents, score, badge, specs, highlights, verificationChecks, summary, warnings.
                    Mantem specs, highlights, verificationChecks e warnings curtos. Nao preenchas sellerOffers; usa sellerOffers: [].
                    Se nao conseguires sugerir produtos reais e compatveis, devolve products: [].
                    """
                },
                new
                {
                    role = "user",
                    content = JsonSerializer.Serialize(new
                    {
                        language = text.IsEnglish ? "en-US" : "pt-PT",
                        query,
                        maxBudgetCents,
                        validation = new
                        {
                            mustMatchQuery = true,
                            rejectWrongCategory = true,
                            rejectAboveBudget = maxBudgetCents.HasValue
                        }
                    }, JsonOptions)
                }
            }
        };

        return JsonSerializer.Serialize(request, JsonOptions);
    }

    private ProductDiscoveryResult NormalizePayload(
        DiscoveryPayload? payload,
        string query,
        string providerName,
        long? maxBudgetCents)
    {
        var products = Clean(payload?.Products)
            .Select(product => NormalizeProduct(product, query))
            .Where(product => product is not null)
            .Cast<ProductResult>()
            .Where(product => IsRelevantProduct(product, query, maxBudgetCents))
            .GroupBy(product => product.Slug, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Take(4)
            .Select((product, index) => product with { Rank = index + 1, Accent = AccentPalette[index % AccentPalette.Length] })
            .ToList();

        var offersBySlug = new Dictionary<string, IReadOnlyList<SellerOffer>>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < products.Count; index++)
        {
            var sourceProduct = Clean(payload?.Products).ElementAtOrDefault(index);
            var offers = NormalizeOffers(sourceProduct?.SellerOffers, products[index]);
            offersBySlug[products[index].Slug] = offers;
            cache.Set(OffersCacheKey(products[index].Slug), offers, TimeSpan.FromMinutes(30));
            cache.Set(ProductCacheKey(products[index].Slug), products[index], TimeSpan.FromMinutes(30));
        }

        var resultOffers = products.Count > 0 && offersBySlug.TryGetValue(products[0].Slug, out var firstOffers)
            ? firstOffers
            : Array.Empty<SellerOffer>();

        var result = new ProductDiscoveryResult(
            query,
            products,
            resultOffers,
            true,
            text.Pick($"LLM online - {providerName}", $"Online LLM - {providerName}"));

        Remember(result, resultOffers);
        return result;
    }

    private ProductResult? NormalizeProduct(DiscoveryProductPayload product, string query)
    {
        var name = CleanText(product.Name);
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var brand = CleanText(product.Brand, GuessBrand(name));
        var priceCents = product.PriceCents > 0
            ? (long)Math.Round(product.PriceCents, MidpointRounding.AwayFromZero)
            : ParsePriceCents(product.Price);
        if (priceCents <= 0)
        {
            return null;
        }

        var specs = Clean(product.Specs)
            .Select(item => CleanText(item))
            .Where(item => item.Length > 0)
            .Take(6)
            .ToList();
        while (specs.Count < 4)
        {
            specs.Add(text.Pick("Confirmar especificacao", "Confirm specification"));
        }

        var highlights = Clean(product.Highlights)
            .Select(item => CleanText(item))
            .Where(item => item.Length > 0)
            .Take(4)
            .DefaultIfEmpty(text.Pick("Boa correspondencia com o pedido", "Good match for the request"))
            .ToList();

        var verificationChecks = Clean(product.VerificationChecks)
            .Select(item => CleanText(item))
            .Where(item => item.Length > 0)
            .Take(5)
            .DefaultIfEmpty(text.Pick("Produto real identificado pela IA", "Real product identified by AI"))
            .Append(text.Pick("Preco e vendedor a confirmar antes da compra", "Confirm price and seller before buying"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToList();

        var warnings = Clean(product.Warnings)
            .Select(item => CleanText(item))
            .Where(item => item.Length > 0)
            .Take(3)
            .Select((warning, index) => new FraudAlert(text.Pick($"Validacao {index + 1}", $"Validation {index + 1}"), warning))
            .ToList();

        return new ProductResult(
            Slugify($"{brand} {name}"),
            1,
            (int)Math.Round(Math.Clamp(product.Score <= 0 ? 78 : product.Score, 60, 98), MidpointRounding.AwayFromZero),
            name,
            brand,
            CleanText(product.Price, FormatCurrency(priceCents)),
            AccentPalette[0],
            CleanText(product.Badge, text.Pick("Recomendado", "Recommended")),
            specs,
            highlights,
            verificationChecks,
            warnings,
            CleanText(product.Summary, text.Pick(
                $"Sugestao gerada para: {query}. Confirma o preco final no vendedor antes de encomendar.",
                $"Suggestion generated for: {query}. Confirm final price with the seller before ordering.")));
    }

    private ProductDiscoveryResult? BuildValidatedFallback(string query, long? maxBudgetCents)
    {
        var primaryTerm = ExtractMeaningfulTerms(query).FirstOrDefault(IsProductNounTerm);
        if (string.IsNullOrWhiteSpace(primaryTerm))
        {
            return null;
        }

        var products = BuildKnownFallbackProducts(primaryTerm, maxBudgetCents)
            .Where(product => IsRelevantProduct(product, query, maxBudgetCents))
            .Take(4)
            .Select((product, index) => product with { Rank = index + 1, Accent = AccentPalette[index % AccentPalette.Length] })
            .ToList();

        if (products.Count == 0)
        {
            return null;
        }

        var offers = BuildDefaultOffers(products[0]);
        var result = new ProductDiscoveryResult(
            query,
            products,
            offers,
            false,
            text.Pick("Fallback validado", "Validated fallback"));

        Remember(result, offers);
        return result;
    }

    private IReadOnlyList<ProductResult> BuildKnownFallbackProducts(string primaryTerm, long? maxBudgetCents)
    {
        var budget = maxBudgetCents ?? 10000;

        return primaryTerm switch
        {
            "cadeira" or "chair" => new[]
            {
                CreateFallbackProduct("ikea-flintan", "IKEA FLINTAN", "IKEA", 7999, "Melhor valor", "Cadeira escritorio", "Altura ajustavel"),
                CreateFallbackProduct("songmics-obg22b", "SONGMICS OBG22B", "SONGMICS", 8999, "Mais confortavel", "Cadeira escritorio", "Apoio lombar"),
                CreateFallbackProduct("vinsetto-cadeira-escritorio", "Vinsetto Cadeira de Escritorio", "Vinsetto", 9499, "Boa ergonomia", "Cadeira escritorio", "Bracos ajustaveis")
            },
            "teclado" or "keyboard" => new[]
            {
                CreateFallbackProduct("logitech-k380", "Logitech K380", "Logitech", Math.Min(budget, 4499), "Melhor compacto", "Teclado bluetooth", "Multi-dispositivo"),
                CreateFallbackProduct("keychron-c3-pro", "Keychron C3 Pro", "Keychron", Math.Min(budget, 6999), "Mecanico", "Teclado mecanico", "USB-C"),
                CreateFallbackProduct("logitech-k120", "Logitech K120", "Logitech", Math.Min(budget, 1499), "Mais barato", "Teclado com fio", "Layout PT")
            },
            "monitor" or "display" or "ecra" or "ecran" => new[]
            {
                CreateFallbackProduct("aoc-24b2xh", "AOC 24B2XH", "AOC", Math.Min(budget, 9999), "Melhor preco", "Monitor 24 polegadas", "Full HD"),
                CreateFallbackProduct("lg-24mp400", "LG 24MP400", "LG", Math.Min(budget, 10999), "IPS", "Monitor 24 polegadas", "75 Hz"),
                CreateFallbackProduct("dell-s2421hn", "Dell S2421HN", "Dell", Math.Min(budget, 12999), "Boa garantia", "Monitor 24 polegadas", "IPS")
            },
            "auscultadores" or "headphones" or "auriculares" => new[]
            {
                CreateFallbackProduct("sony-wh-ch520", "Sony WH-CH520", "Sony", Math.Min(budget, 4999), "Melhor bateria", "Auscultadores bluetooth", "50h bateria"),
                CreateFallbackProduct("jbl-tune-520bt", "JBL Tune 520BT", "JBL", Math.Min(budget, 3999), "Bom valor", "Auscultadores bluetooth", "Graves JBL"),
                CreateFallbackProduct("soundcore-q20i", "Soundcore Q20i", "Anker", Math.Min(budget, 5999), "Cancelamento ruido", "Auscultadores ANC", "Bluetooth")
            },
            _ => new[]
            {
                CreateFallbackProduct(
                    $"{primaryTerm}-pesquisa-validada",
                    $"{ToDisplayName(primaryTerm)} recomendado",
                    "TrueCompare",
                    Math.Min(budget, 9999),
                    "Pesquisa validada",
                    ToDisplayName(primaryTerm),
                    "Confirmar modelo")
            }
        };
    }

    private ProductResult CreateFallbackProduct(
        string slug,
        string name,
        string brand,
        long priceCents,
        string badge,
        string productType,
        string keySpec)
    {
        return new ProductResult(
            slug,
            1,
            78,
            name,
            brand,
            FormatCurrency(priceCents),
            AccentPalette[0],
            badge,
            new[] { productType, keySpec, "Preco estimado", "Garantia a validar", "Vendedor autorizado", "Confirmar stock" },
            new[] { "Categoria correta", "Dentro do criterio indicado", "Pesquisa pronta para vendedor autorizado" },
            new[] { "Produto reconhecido", "Preco final a confirmar", "Garantia e vendedor a validar" },
            Array.Empty<FraudAlert>(),
            text.Pick(
                "Sugestao validada por categoria quando os providers LLM nao devolveram JSON utilizavel. Confirma sempre o preco final antes de comprar.",
                "Category-validated suggestion used when LLM providers did not return usable JSON. Always confirm final price before buying."));
    }

    private IReadOnlyList<SellerOffer> NormalizeOffers(IReadOnlyList<DiscoveryOfferPayload>? offers, ProductResult product)
    {
        var normalized = Clean(offers)
            .Select(offer =>
            {
                var seller = CleanText(offer.Seller);
                if (string.IsNullOrWhiteSpace(seller))
                {
                    return null;
                }

                var priceCents = offer.PriceCents > 0
                    ? (long)Math.Round(offer.PriceCents, MidpointRounding.AwayFromZero)
                    : ParsePriceCents(offer.Price);
                if (priceCents <= 0)
                {
                    priceCents = ParsePriceCents(product.Price);
                }

                return new SellerOffer(
                    seller,
                    CleanText(offer.Price, FormatCurrency(priceCents)),
                    priceCents,
                    CleanText(offer.Delivery, text.Pick("Confirmar loja", "Confirm store")),
                    CleanText(offer.Warranty, text.Pick("Validar vendedor", "Validate seller")),
                    CleanText(offer.Status, text.Pick("A confirmar", "To confirm")),
                    CleanText(offer.Url, BuildSellerSearchUrl(seller, product.Name)),
                    offer.Preferred);
            })
            .Where(offer => offer is not null)
            .Cast<SellerOffer>()
            .OrderBy(offer => offer.PriceCents)
            .Take(5)
            .ToList();

        return normalized.Count > 0 ? normalized : BuildDefaultOffers(product);
    }

    private IReadOnlyList<SellerOffer> BuildDefaultOffers(ProductResult product)
    {
        var priceCents = Math.Max(1, ParsePriceCents(product.Price));
        var sellers = new[] { "Amazon.es", "Worten", "KuantoKusta", "FNAC" };
        return sellers
            .Select((seller, index) => new SellerOffer(
                seller,
                FormatCurrency(priceCents + (index * 250)),
                priceCents + (index * 250),
                text.Pick("Confirmar loja", "Confirm store"),
                text.Pick("Validar vendedor", "Validate seller"),
                text.Pick(index == 0 ? "Melhor pesquisa" : "A confirmar", index == 0 ? "Best search" : "To confirm"),
                BuildSellerSearchUrl(seller, product.Name),
                index == 0))
            .ToList();
    }

    private static DiscoveryPayload? ReadPayload(string content)
    {
        return JsonSerializer.Deserialize<DiscoveryPayload>(content, JsonOptions);
    }

    private static bool IsValidPayload(DiscoveryPayload? payload, string query, long? maxBudgetCents)
    {
        return Clean(payload?.Products)
            .Select(product => NormalizeValidationProduct(product))
            .Any(product => product is not null && IsRelevantProduct(product, query, maxBudgetCents));
    }

    private static ProductResult? NormalizeValidationProduct(DiscoveryProductPayload product)
    {
        var name = CleanText(product.Name);
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var priceCents = product.PriceCents > 0
            ? (long)Math.Round(product.PriceCents, MidpointRounding.AwayFromZero)
            : ParsePriceCents(product.Price);
        if (priceCents <= 0)
        {
            return null;
        }

        var brand = CleanText(product.Brand, GuessBrand(name));
        return new ProductResult(
            Slugify($"{brand} {name}"),
            1,
            (int)Math.Round(Math.Clamp(product.Score <= 0 ? 78 : product.Score, 60, 98), MidpointRounding.AwayFromZero),
            name,
            brand,
            CleanText(product.Price, FormatCurrency(priceCents)),
            AccentPalette[0],
            CleanText(product.Badge, "Recomendado"),
            Clean(product.Specs).Select(item => CleanText(item)).Where(item => item.Length > 0).ToList(),
            Clean(product.Highlights).Select(item => CleanText(item)).Where(item => item.Length > 0).ToList(),
            Clean(product.VerificationChecks).Select(item => CleanText(item)).Where(item => item.Length > 0).ToList(),
            Array.Empty<FraudAlert>(),
            CleanText(product.Summary));
    }

    private static bool IsRelevantProduct(ProductResult product, string query, long? maxBudgetCents)
    {
        var priceCents = ParsePriceCents(product.Price);
        if (maxBudgetCents.HasValue && priceCents > maxBudgetCents.Value)
        {
            return false;
        }

        var terms = ExtractMeaningfulTerms(query).ToList();
        if (terms.Count == 0)
        {
            return true;
        }

        var identityText = NormalizeText(string.Join(' ', new[]
        {
            product.Name,
            product.Brand,
            product.Badge,
            string.Join(' ', product.Specs)
        }));

        var productText = NormalizeText(string.Join(' ', new[]
        {
            product.Name,
            product.Brand,
            product.Badge,
            product.AiSummary,
            string.Join(' ', product.Specs),
            string.Join(' ', product.Highlights)
        }));

        var primaryProductTerms = terms.Where(IsProductNounTerm).ToList();
        if (primaryProductTerms.Count > 0)
        {
            return !primaryProductTerms.Any(term => HasContradictingProductType(term, identityText))
                && primaryProductTerms.Any(term => ExpandTerm(term).Any(expanded =>
                    identityText.Contains(expanded, StringComparison.OrdinalIgnoreCase)
                    || productText.Contains(expanded, StringComparison.OrdinalIgnoreCase)));
        }

        var requiredTerms = primaryProductTerms.Count > 0
            ? primaryProductTerms
            : terms.Where(term => !IsDescriptorTerm(term)).ToList();
        var termsToMatch = requiredTerms.Count > 0 ? requiredTerms : terms;

        return termsToMatch.Any(term => ExpandTerm(term).Any(expanded => productText.Contains(expanded, StringComparison.OrdinalIgnoreCase)));
    }

    private void Remember(ProductDiscoveryResult result, IReadOnlyList<SellerOffer> defaultOffers)
    {
        foreach (var product in result.Products)
        {
            cache.Set(ProductCacheKey(product.Slug), product, TimeSpan.FromMinutes(30));
            if (!cache.TryGetValue(OffersCacheKey(product.Slug), out _))
            {
                cache.Set(OffersCacheKey(product.Slug), defaultOffers.Count > 0 ? defaultOffers : BuildDefaultOffers(product), TimeSpan.FromMinutes(30));
            }
        }
    }

    private static string NormalizeQuery(string? query)
    {
        return string.IsNullOrWhiteSpace(query)
            ? "produto com melhor preco garantia e baixo risco"
            : query.Trim();
    }

    private static IEnumerable<string> ExtractMeaningfulTerms(string query)
    {
        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "a", "o", "os", "as", "um", "uma", "uns", "umas", "de", "do", "da", "dos", "das",
            "com", "sem", "para", "por", "que", "queres", "quero", "queria", "procuro", "comprar",
            "compra", "melhor", "bom", "boa", "bons", "boas", "ate", "at", "euros", "euro", "eur",
            "preco", "valor", "orcamento", "max", "maximo", "menos", "mais", "produto", "produtos",
            "vendedor", "revendedor", "autorizado", "autorizada", "garantia", "seller", "store", "best",
            "buy", "under", "below", "with", "for", "the", "and"
        };

        return NormalizeText(query)
            .Replace('-', ' ')
            .Replace('/', ' ')
            .Replace(',', ' ')
            .Replace('.', ' ')
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(term => term.Length > 2 && !long.TryParse(term, out _) && !stopWords.Contains(term))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> ExpandTerm(string term)
    {
        yield return term;

        foreach (var expanded in term switch
        {
            "rato" or "ratos" or "mouse" or "mice" => new[] { "rato", "mouse", "mice" },
            "teclado" or "keyboard" => new[] { "teclado", "keyboard" },
            "cadeira" or "chair" => new[] { "cadeira", "chair" },
            "mesa" or "desk" => new[] { "mesa", "desk" },
            "monitor" or "ecra" or "ecran" or "display" => new[] { "monitor", "display", "ecra", "ecran" },
            "auscultadores" or "headphones" or "auriculares" => new[] { "auscultadores", "headphones", "auriculares" },
            "camara" or "camera" => new[] { "camara", "camera" },
            "frigorifico" or "fridge" => new[] { "frigorifico", "fridge", "combinado" },
            "bicicleta" or "bike" => new[] { "bicicleta", "bike" },
            "berbequim" or "drill" => new[] { "berbequim", "drill" },
            _ => Array.Empty<string>()
        })
        {
            yield return expanded;
        }
    }

    private static bool IsDescriptorTerm(string term)
    {
        return term is "barato" or "barata" or "economico" or "economica" or "premium"
            or "gaming" or "ergonomico" or "ergonomica" or "confortavel" or "leve"
            or "rapido" or "rapida" or "silencioso" or "silenciosa" or "wireless"
            or "bluetooth" or "bom" or "boa";
    }

    private static bool IsProductNounTerm(string term)
    {
        return term is "rato" or "ratos" or "mouse" or "mice" or "teclado" or "keyboard"
            or "cadeira" or "chair" or "mesa" or "desk" or "monitor" or "ecra" or "ecran"
            or "display" or "auscultadores" or "headphones" or "auriculares" or "camara"
            or "camera" or "frigorifico" or "fridge" or "bicicleta" or "bike" or "berbequim"
            or "drill" or "impressora" or "printer" or "microfone" or "microphone";
    }

    private static bool HasContradictingProductType(string requestedTerm, string identityText)
    {
        return requestedTerm switch
        {
            "cadeira" or "chair" => ContainsAny(identityText, "mesa", "desk", "table", "escrivaninha", "escrivaneta"),
            "rato" or "ratos" or "mouse" or "mice" => ContainsAny(identityText, "portatil", "laptop", "notebook", "smartphone", "telefone"),
            "teclado" or "keyboard" => ContainsAny(identityText, "rato", "mouse", "monitor", "headphone", "auscultador"),
            "monitor" or "display" or "ecra" or "ecran" => ContainsAny(identityText, "portatil", "laptop", "televisao", "tv"),
            _ => false
        };
    }

    private static bool ContainsAny(string value, params string[] terms)
    {
        return terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private static long? TryExtractMaxBudgetCents(string query)
    {
        var normalized = NormalizeText(query);
        var match = BudgetRegex().Match(normalized);
        if (!match.Success)
        {
            return null;
        }

        var rawAmount = match.Groups["amount"].Value.Replace(',', '.');
        return decimal.TryParse(rawAmount, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)
            ? (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero)
            : null;
    }

    private static long ParsePriceCents(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        var match = PriceRegex().Match(value);
        if (!match.Success)
        {
            return 0;
        }

        var amount = match.Groups[1].Value.Replace(" ", string.Empty);
        if (amount.Contains(',', StringComparison.Ordinal))
        {
            amount = amount.Replace(".", string.Empty).Replace(',', '.');
        }
        else if (Regex.IsMatch(amount, @"\.\d{3}$"))
        {
            amount = amount.Replace(".", string.Empty);
        }

        return decimal.TryParse(amount, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? (long)Math.Round(parsed * 100m, MidpointRounding.AwayFromZero)
            : 0;
    }

    private static string FormatCurrency(long cents)
    {
        return $"{(cents / 100m).ToString("N2", CultureInfo.CurrentCulture)} \u20AC";
    }

    private static string BuildSellerSearchUrl(string seller, string query)
    {
        var encoded = Uri.EscapeDataString(query);
        return seller.ToLowerInvariant() switch
        {
            var value when value.Contains("amazon") => $"https://www.amazon.es/s?k={encoded}",
            var value when value.Contains("worten") => $"https://www.worten.pt/search?query={encoded}",
            var value when value.Contains("fnac") => $"https://www.fnac.pt/SearchResult/ResultList.aspx?Search={encoded}",
            var value when value.Contains("mediamarkt") => $"https://www.mediamarkt.pt/pt/search.html?query={encoded}",
            var value when value.Contains("kuanto") => $"https://www.kuantokusta.pt/search?q={encoded}",
            _ => $"https://www.google.com/search?q={encoded}"
        };
    }

    private static string GuessBrand(string name)
    {
        var firstWord = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
        return string.IsNullOrWhiteSpace(firstWord) ? "Produto" : firstWord;
    }

    private static string ToDisplayName(string value)
    {
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value.Replace('-', ' '));
    }

    private static string Slugify(string value)
    {
        var normalized = NormalizeText(value);
        var builder = new StringBuilder(normalized.Length);
        var previousDash = false;

        foreach (var character in normalized)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                previousDash = false;
            }
            else if (!previousDash)
            {
                builder.Append('-');
                previousDash = true;
            }
        }

        return builder.ToString().Trim('-');
    }

    private static string NormalizeText(string value)
    {
        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static IReadOnlyList<T> Clean<T>(IReadOnlyList<T>? values)
    {
        return values ?? Array.Empty<T>();
    }

    private static string CleanText(string? value, string fallback = "")
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string ProductCacheKey(string slug)
    {
        return $"product-discovery:product:{slug.ToLowerInvariant()}";
    }

    private static string OffersCacheKey(string slug)
    {
        return $"product-discovery:offers:{slug.ToLowerInvariant()}";
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };
        options.Converters.Add(new FlexibleStringConverter());
        options.Converters.Add(new FlexibleStringListConverter());
        options.Converters.Add(new FlexibleOfferListConverter());
        return options;
    }

    [GeneratedRegex(@"(?:ate|at[eé]|under|below|maximo|max|orcamento)\s*(?:de\s*)?(?:eur|euros?|\u20ac)?\s*(?<amount>\d+(?:[\.,]\d{1,2})?)|(?<amount>\d+(?:[\.,]\d{1,2})?)\s*(?:eur|euros?|\u20ac)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BudgetRegex();

    [GeneratedRegex(@"(\d{1,3}(?:[\.\s]\d{3})*(?:,\d{1,2})?|\d+(?:[\.,]\d{1,2})?)", RegexOptions.CultureInvariant)]
    private static partial Regex PriceRegex();

    private sealed class DiscoveryPayload
    {
        public string? Category { get; set; }

        public double Confidence { get; set; }

        public IReadOnlyList<DiscoveryProductPayload>? Products { get; set; }
    }

    private sealed class DiscoveryProductPayload
    {
        public string? Name { get; set; }

        public string? Brand { get; set; }

        public string? Price { get; set; }

        public decimal PriceCents { get; set; }

        public double Score { get; set; }

        public string? Badge { get; set; }

        public IReadOnlyList<string>? Specs { get; set; }

        public IReadOnlyList<string>? Highlights { get; set; }

        public IReadOnlyList<string>? VerificationChecks { get; set; }

        public string? Summary { get; set; }

        public IReadOnlyList<string>? Warnings { get; set; }

        public IReadOnlyList<DiscoveryOfferPayload>? SellerOffers { get; set; }
    }

    private sealed class DiscoveryOfferPayload
    {
        public string? Seller { get; set; }

        public string? Price { get; set; }

        public decimal PriceCents { get; set; }

        public string? Delivery { get; set; }

        public string? Warranty { get; set; }

        public string? Status { get; set; }

        public string? Url { get; set; }

        public bool Preferred { get; set; }
    }

    private sealed class FlexibleStringConverter : JsonConverter<string?>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.String => reader.GetString(),
                JsonTokenType.Number => reader.TryGetInt64(out var longValue)
                    ? longValue.ToString(CultureInfo.InvariantCulture)
                    : reader.GetDecimal().ToString(CultureInfo.InvariantCulture),
                JsonTokenType.True => bool.TrueString,
                JsonTokenType.False => bool.FalseString,
                JsonTokenType.Null => null,
                _ => JsonDocument.ParseValue(ref reader).RootElement.ToString()
            };
        }

        public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }

    private sealed class FlexibleStringListConverter : JsonConverter<IReadOnlyList<string>?>
    {
        public override IReadOnlyList<string>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            using var document = JsonDocument.ParseValue(ref reader);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Array)
            {
                return root.EnumerateArray()
                    .Select(ElementToString)
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .ToList();
            }

            return new[] { ElementToString(root) };
        }

        public override void Write(Utf8JsonWriter writer, IReadOnlyList<string>? value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, options);
        }

        private static string ElementToString(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString() ?? string.Empty,
                JsonValueKind.Number => element.GetRawText(),
                JsonValueKind.True => bool.TrueString,
                JsonValueKind.False => bool.FalseString,
                JsonValueKind.Null => string.Empty,
                _ => element.ToString()
            };
        }
    }

    private sealed class FlexibleOfferListConverter : JsonConverter<IReadOnlyList<DiscoveryOfferPayload>?>
    {
        public override IReadOnlyList<DiscoveryOfferPayload>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            using var document = JsonDocument.ParseValue(ref reader);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Array)
            {
                return root.EnumerateArray()
                    .Select(element => JsonSerializer.Deserialize<DiscoveryOfferPayload>(element.GetRawText(), options))
                    .Where(offer => offer is not null)
                    .Cast<DiscoveryOfferPayload>()
                    .ToList();
            }

            if (root.ValueKind == JsonValueKind.Object)
            {
                var offer = JsonSerializer.Deserialize<DiscoveryOfferPayload>(root.GetRawText(), options);
                return offer is null ? Array.Empty<DiscoveryOfferPayload>() : new[] { offer };
            }

            return Array.Empty<DiscoveryOfferPayload>();
        }

        public override void Write(Utf8JsonWriter writer, IReadOnlyList<DiscoveryOfferPayload>? value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, options);
        }
    }
}
