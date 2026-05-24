using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using TrueCompare.Models;
using TrueCompare.Options;

namespace TrueCompare.Services;

public sealed class LlmSuggestionService(
    LlmProviderRouter providerRouter,
    IMemoryCache cache,
    ILogger<LlmSuggestionService> logger,
    AppText text) : IProductSuggestionService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    static LlmSuggestionService()
    {
        JsonOptions.Converters.Add(new FlexibleDoubleConverter());
    }

    public async Task<LlmSuggestionResult> GetSuggestionsAsync(
        string? query,
        IReadOnlyList<ProductResult> products,
        IReadOnlyList<SellerOffer> offers,
        CancellationToken cancellationToken = default)
    {
        var normalizedQuery = string.IsNullOrWhiteSpace(query)
            ? text.Pick("produto com melhor relação preço, garantia e baixo risco", "product with best price, warranty and low risk")
            : query.Trim();
        var maxBudgetCents = TryExtractMaxBudgetCents(normalizedQuery);
        var scopedProducts = FilterProductsByBudget(products, maxBudgetCents);
        var scopedOffers = FilterOffersByBudget(offers, maxBudgetCents);

        var cacheKey = $"llm-suggestions:{normalizedQuery.ToLowerInvariant()}:{string.Join('|', scopedProducts.Select(product => product.Slug))}";
        if (cache.TryGetValue(cacheKey, out LlmSuggestionResult? cached) && cached is not null)
        {
            return cached;
        }

        var fallback = BuildFallback(normalizedQuery, scopedProducts, scopedOffers, maxBudgetCents);
        if (maxBudgetCents.HasValue && scopedProducts.Count == 0 && scopedOffers.Count == 0)
        {
            return fallback;
        }

        using var suggestionTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        suggestionTimeout.CancelAfter(TimeSpan.FromSeconds(2));

        try
        {
            var providerResponse = await providerRouter.TryGetValidJsonAsync(
                "product-suggestions",
                requiresVision: false,
                provider => BuildRequestJson(provider, normalizedQuery, scopedProducts, scopedOffers, maxBudgetCents),
                content => IsValidPayload(ReadPayload(content)),
                suggestionTimeout.Token);

            if (providerResponse is null)
            {
                return fallback;
            }

            var parsed = ReadPayload(providerResponse.JsonContent);
            var result = NormalizePayload(parsed, fallback, providerResponse.ProviderName, maxBudgetCents);

            cache.Set(cacheKey, result, TimeSpan.FromMinutes(20));
            return result;
        }
        catch (OperationCanceledException) when (suggestionTimeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("LLM request timed out");
            return fallback;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "LLM suggestion generation failed");
            return fallback;
        }
    }

    private string BuildRequestJson(
        LlmProviderOptions provider,
        string query,
        IReadOnlyList<ProductResult> products,
        IReadOnlyList<SellerOffer> offers,
        long? maxBudgetCents)
    {
        var knownProducts = products.Select(product => new
        {
            product.Name,
            product.Brand,
            product.Price,
            product.Score,
            product.Badge,
            Specs = product.Specs.Take(5),
            Highlights = product.Highlights
        });

        var knownOffers = offers.Select(offer => new
        {
            offer.Seller,
            Price = offer.IsLivePrice ? offer.Price : text.Pick("confirmar na loja", "confirm in store"),
            offer.Delivery,
            offer.Warranty,
            offer.Status,
            offer.Preferred,
            offer.IsLivePrice
        });

        var request = new
        {
            model = provider.Model,
            temperature = provider.Temperature,
            max_tokens = Math.Clamp(provider.MaxTokens, 300, 1800),
            response_format = provider.SupportsJsonObjectResponseFormat ? new { type = "json_object" } : null,
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = """
                    És o motor de sugestões da TrueCompare. Responde na mesma língua do pedido do utilizador e devolve apenas JSON válido.
                    Não inventes preços em tempo real, stock, links ou vendedores. Usa preços só quando vierem em "ofertasConhecidas".
                    Se sugerires produtos fora do catálogo, marca source como "Sugestão IA" e usa targetPrice como "confirmar".
                    Se o pedido tiver um orcamento maximo, nao sugiras produtos acima desse valor. Se nao houver opcoes dentro do orcamento, explica isso sem recomendar produtos fora do limite.
                    As suggestedQueries devem ser pesquisas executáveis, curtas e específicas: modelo + prioridade + orçamento/loja/garantia quando existir.
                    Os productLeads devem ser alternativas reais e úteis, não repetir o mesmo produto com palavras diferentes.
                    O JSON deve ter: intent, summary, confidence, buyingSignals, suggestedQueries, warnings, productLeads.
                    productLeads deve ter no máximo 4 itens com name, reason, targetPrice, searchHint e source.
                    """
                },
                new
                {
                    role = "user",
                    content = JsonSerializer.Serialize(new
                    {
                        idioma = text.IsEnglish ? "en-US" : "pt-PT",
                        pedido = query,
                        orcamentoMaximoCentimos = maxBudgetCents,
                        produtosConhecidos = knownProducts,
                        ofertasConhecidas = knownOffers
                    }, JsonOptions)
                }
            }
        };

        return JsonSerializer.Serialize(request, JsonOptions);
    }

    private static LlmPayload? ReadPayload(string content)
    {
        return JsonSerializer.Deserialize<LlmPayload>(content, JsonOptions);
    }

    private LlmSuggestionResult NormalizePayload(LlmPayload? payload, LlmSuggestionResult fallback, string providerName, long? maxBudgetCents)
    {
        if (payload is null)
        {
            return fallback;
        }

        var leads = Clean(payload.ProductLeads)
            .Select(lead => new LlmProductLead(
                CleanText(lead.Name),
                CleanText(lead.Reason),
                CleanText(lead.TargetPrice, text.Pick("confirmar", "confirm")),
                CleanText(lead.SearchHint),
                CleanText(lead.Source, text.Pick("Sugestão IA", "AI suggestion"))))
            .Where(lead => !string.IsNullOrWhiteSpace(lead.Name) && !string.IsNullOrWhiteSpace(lead.Reason))
            .Where(lead => IsLeadWithinBudget(lead, maxBudgetCents))
            .Take(4)
            .ToList();

        return new LlmSuggestionResult(
            true,
            text.Pick($"LLM ativo · {providerName}", $"LLM active · {providerName}"),
            CleanText(payload.Intent, fallback.Intent),
            CleanText(payload.Summary, fallback.Summary),
            payload.Confidence <= 0
                ? fallback.Confidence
                : (int)Math.Round(Math.Clamp(payload.Confidence, 0, 100), MidpointRounding.AwayFromZero),
            Clean(payload.BuyingSignals).Select(signal => CleanText(signal)).Where(signal => signal.Length > 0).Take(5).ToList(),
            Clean(payload.SuggestedQueries).Select(item => CleanText(item)).Where(item => item.Length > 0).Take(4).ToList(),
            Clean(payload.Warnings).Select(item => CleanText(item)).Where(item => item.Length > 0).Take(4).ToList(),
            leads.Count > 0 ? leads : fallback.ProductLeads);
    }

    private static bool IsValidPayload(LlmPayload? payload)
    {
        if (payload is null
            || string.IsNullOrWhiteSpace(payload.Summary)
            || payload.Confidence <= 0)
        {
            return false;
        }

        return Clean(payload.ProductLeads).Any(lead => !string.IsNullOrWhiteSpace(lead.Name) && !string.IsNullOrWhiteSpace(lead.Reason))
            || Clean(payload.SuggestedQueries).Any(query => !string.IsNullOrWhiteSpace(query))
            || Clean(payload.BuyingSignals).Any(signal => !string.IsNullOrWhiteSpace(signal));
    }

    private LlmSuggestionResult BuildFallback(
        string query,
        IReadOnlyList<ProductResult> products,
        IReadOnlyList<SellerOffer> offers,
        long? maxBudgetCents = null)
    {
        var bestProduct = products.OrderByDescending(product => product.Score).FirstOrDefault();
        var bestKnownOffer = offers.OrderBy(offer => offer.PriceCents).FirstOrDefault();
        var bestOffer = offers
            .Where(offer => offer.IsLivePrice)
            .OrderBy(offer => offer.PriceCents)
            .FirstOrDefault();
        if (bestProduct is null && maxBudgetCents.HasValue)
        {
            return new LlmSuggestionResult(
                false,
                text.Pick("Modo local", "Local mode"),
                query,
                text.Pick(
                    "Nao encontrei produtos conhecidos dentro do orcamento indicado. Aumenta o limite ou confirma se aceitas opcoes usadas/recondicionadas.",
                    "No known products were found inside the requested budget. Increase the limit or confirm whether used/refurbished options are acceptable."),
                72,
                BuildFallbackSignals(null, null),
                BuildFallbackQueries(query),
                new[] { text.Pick("Nao foram encontrados precos validados dentro do limite indicado.", "No validated prices were found inside the requested limit.") },
                Array.Empty<LlmProductLead>());
        }

        var summary = bestProduct is null
            ? text.Pick("Define orçamento, garantia e risco para gerar uma comparação mais forte.", "Define budget, warranty and risk to generate a stronger comparison.")
            : bestOffer is not null
                ? text.Pick(
                    $"Melhor ponto de partida: {bestProduct.Name}. Melhor vendedor confirmado: {bestOffer.Seller} {bestOffer.Price}.",
                    $"Best starting point: {bestProduct.Name}. Best confirmed seller: {bestOffer.Seller} {bestOffer.Price}.")
                : text.Pick(
                    $"Melhor ponto de partida: {bestProduct.Name}. Lojas conhecidas encontradas; preço final a confirmar na loja.",
                    $"Best starting point: {bestProduct.Name}. Known stores found; final price must be confirmed in store.");

        var leadOffers = offers
            .OrderByDescending(offer => offer.IsLiveValidated)
            .ThenByDescending(offer => offer.Preferred)
            .ThenByDescending(offer => offer.IsLivePrice)
            .ThenByDescending(offer => offer.ReliabilityScore)
            .Take(4)
            .ToList();

        var leads = products
            .OrderBy(product => product.Rank)
            .ThenByDescending(product => product.Score)
            .Take(4)
            .Select((product, index) =>
            {
                var storeHint = leadOffers.ElementAtOrDefault(index % Math.Max(1, leadOffers.Count));
                var reason = storeHint is null
                    ? product.AiSummary
                    : text.Pick(
                        $"{product.AiSummary} Loja a verificar primeiro: {storeHint.Seller}.",
                        $"{product.AiSummary} First store to check: {storeHint.Seller}.");

                return new LlmProductLead(
                    product.Name,
                    reason,
                    product.Price,
                    BuildProductSearchHint(query, product, storeHint),
                    product.Rank <= 4 ? text.Pick("Resultado recomendado", "Recommended result") : text.Pick("Catálogo", "Catalog"));
            })
            .ToList();

        var warnings = products
            .SelectMany(product => product.FraudAlerts)
            .Select(alert => $"{alert.Source}: {alert.Reason}")
            .DefaultIfEmpty(text.Pick("Confirma sempre garantia, NIF do vendedor e política de devolução.", "Always confirm warranty, seller tax details and return policy."))
            .Take(4)
            .ToList();

        return new LlmSuggestionResult(
            false,
            text.Pick("Modo local", "Local mode"),
            query,
            summary.Trim(),
            bestProduct?.Score ?? 72,
            BuildFallbackSignals(bestProduct, bestOffer ?? bestKnownOffer),
            BuildFallbackQueries(query),
            warnings,
            leads);
    }

    private static IReadOnlyList<ProductResult> FilterProductsByBudget(IReadOnlyList<ProductResult> products, long? maxBudgetCents)
    {
        if (!maxBudgetCents.HasValue)
        {
            return products;
        }

        return products
            .Where(product =>
            {
                var priceCents = ParsePriceCents(product.Price);
                return priceCents > 0 && priceCents <= maxBudgetCents.Value;
            })
            .ToList();
    }

    private static IReadOnlyList<SellerOffer> FilterOffersByBudget(IReadOnlyList<SellerOffer> offers, long? maxBudgetCents)
    {
        if (!maxBudgetCents.HasValue)
        {
            return offers;
        }

        return offers
            .Where(offer => offer.PriceCents > 0 && offer.PriceCents <= maxBudgetCents.Value)
            .ToList();
    }

    private static bool IsLeadWithinBudget(LlmProductLead lead, long? maxBudgetCents)
    {
        if (!maxBudgetCents.HasValue)
        {
            return true;
        }

        var priceCents = ParsePriceCents(lead.TargetPrice);
        return priceCents <= 0 || priceCents <= maxBudgetCents.Value;
    }

    private IReadOnlyList<string> BuildFallbackSignals(ProductResult? product, SellerOffer? offer)
    {
        var signals = new List<string>();
        if (product is not null)
        {
            signals.AddRange(product.Highlights.Take(3));
            signals.Add($"{product.Score}% match");
        }

        if (offer is not null && offer.IsLivePrice)
        {
            signals.Add($"{offer.Seller}: {offer.Price}");
        }
        else if (offer is not null)
        {
            signals.Add(text.Pick("Preço final a confirmar na loja", "Final price to confirm in store"));
        }

        return signals.Count > 0
            ? signals
            : new[] { text.Pick("Preço", "Price"), text.Pick("Garantia", "Warranty"), text.Pick("Risco", "Risk"), text.Pick("Entrega", "Delivery") };
    }

    private IReadOnlyList<string> BuildFallbackQueries(string query)
    {
        var normalized = query.Trim();
        var baseQueries = text.IsEnglish
            ? new List<string>
            {
                $"{normalized} best price authorized seller",
                $"{normalized} warranty Portugal",
                $"{normalized} alternative best price",
                $"{normalized} target price alert"
            }
            : new List<string>
            {
                $"{normalized} melhor preço vendedor autorizado",
                $"{normalized} garantia Portugal",
                $"{normalized} alternativa melhor preço",
                $"{normalized} alerta preço alvo"
            };

        var lower = normalized.ToLowerInvariant();
        if (lower.Contains("barat", StringComparison.OrdinalIgnoreCase) || lower.Contains("baixo", StringComparison.OrdinalIgnoreCase))
        {
            baseQueries.Insert(0, text.IsEnglish
                ? $"{normalized} best value under budget"
                : $"{normalized} melhor valor dentro do orçamento");
        }

        if (lower.Contains("garantia", StringComparison.OrdinalIgnoreCase) || lower.Contains("oficial", StringComparison.OrdinalIgnoreCase))
        {
            baseQueries.Insert(0, text.IsEnglish
                ? $"{normalized} official warranty store"
                : $"{normalized} loja oficial garantia");
        }

        if (lower.Contains("gaming", StringComparison.OrdinalIgnoreCase) || lower.Contains("jogos", StringComparison.OrdinalIgnoreCase))
        {
            baseQueries.Insert(0, text.IsEnglish
                ? $"{normalized} gaming reviews Portugal"
                : $"{normalized} gaming reviews Portugal");
        }

        return baseQueries
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(4)
            .ToList();
    }

    private string BuildProductSearchHint(string query, ProductResult product, SellerOffer? storeHint)
    {
        var storePart = storeHint is null
            ? text.Pick("melhor preço garantia", "best price warranty")
            : text.Pick($"{storeHint.Seller} preço garantia", $"{storeHint.Seller} price warranty");

        return $"{product.Brand} {product.Name} {storePart}".Trim();
    }

    private static IReadOnlyList<T> Clean<T>(IReadOnlyList<T>? values)
    {
        return values ?? Array.Empty<T>();
    }

    private static string CleanText(string? value, string fallback = "")
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static long? TryExtractMaxBudgetCents(string query)
    {
        var normalized = RemoveDiacritics(query.Trim().ToLowerInvariant());
        var match = BudgetRegex.Match(normalized);
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

        var match = PriceRegex.Match(value);
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

    private static string RemoveDiacritics(string value)
    {
        var normalized = value.Normalize(System.Text.NormalizationForm.FormD);
        var chars = normalized
            .Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            .ToArray();

        return new string(chars).Normalize(System.Text.NormalizationForm.FormC);
    }

    private static readonly Regex BudgetRegex = new(
        @"(?:ate|under|below|maximo|max|orcamento)\s*(?:de\s*)?(?:eur|euros?|\u20ac)?\s*(?<amount>\d+(?:[\.,]\d{1,2})?)|(?<amount>\d+(?:[\.,]\d{1,2})?)\s*(?:eur|euros?|\u20ac)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex PriceRegex = new(
        @"(\d+(?:[\s\.]\d{3})*(?:[,.]\d{1,2})?|\d+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private sealed class LlmPayload
    {
        public string? Intent { get; set; }

        public string? Summary { get; set; }

        public double Confidence { get; set; }

        public IReadOnlyList<string>? BuyingSignals { get; set; }

        public IReadOnlyList<string>? SuggestedQueries { get; set; }

        public IReadOnlyList<string>? Warnings { get; set; }

        public IReadOnlyList<LlmLeadPayload>? ProductLeads { get; set; }
    }

    private sealed class FlexibleDoubleConverter : JsonConverter<double>
    {
        public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                return reader.GetDouble();
            }

            if (reader.TokenType != JsonTokenType.String)
            {
                return 0;
            }

            var value = reader.GetString();
            if (string.IsNullOrWhiteSpace(value))
            {
                return 0;
            }

            var normalized = value.Trim().TrimEnd('%').Replace(',', '.');
            if (double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }

            var numericText = new string(value
                .Where(character => char.IsDigit(character) || character is '.' or ',')
                .ToArray())
                .Replace(',', '.');

            return double.TryParse(numericText, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed)
                ? parsed
                : 0;
        }

        public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }
    }

    private sealed class LlmLeadPayload
    {
        public string? Name { get; set; }

        public string? Reason { get; set; }

        public string? TargetPrice { get; set; }

        public string? SearchHint { get; set; }

        public string? Source { get; set; }
    }
}
