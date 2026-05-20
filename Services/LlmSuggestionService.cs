using System.Text.Json;
using System.Text.Json.Serialization;
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
        PropertyNameCaseInsensitive = true
    };

    public async Task<LlmSuggestionResult> GetSuggestionsAsync(
        string? query,
        IReadOnlyList<ProductResult> products,
        IReadOnlyList<SellerOffer> offers,
        CancellationToken cancellationToken = default)
    {
        var normalizedQuery = string.IsNullOrWhiteSpace(query)
            ? text.Pick("produto com melhor relação preço, garantia e baixo risco", "product with best price, warranty and low risk")
            : query.Trim();

        var cacheKey = $"llm-suggestions:{normalizedQuery.ToLowerInvariant()}:{string.Join('|', products.Select(product => product.Slug))}";
        if (cache.TryGetValue(cacheKey, out LlmSuggestionResult? cached) && cached is not null)
        {
            return cached;
        }

        var fallback = BuildFallback(normalizedQuery, products, offers);

        try
        {
            var providerResponse = await providerRouter.TryGetValidJsonAsync(
                "product-suggestions",
                requiresVision: false,
                provider => BuildRequestJson(provider, normalizedQuery, products, offers),
                content => IsValidPayload(ReadPayload(content)),
                cancellationToken);

            if (providerResponse is null)
            {
                return fallback;
            }

            var parsed = ReadPayload(providerResponse.JsonContent);
            var result = NormalizePayload(parsed, fallback, providerResponse.ProviderName);

            cache.Set(cacheKey, result, TimeSpan.FromMinutes(20));
            return result;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
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

    private string BuildRequestJson(LlmProviderOptions provider, string query, IReadOnlyList<ProductResult> products, IReadOnlyList<SellerOffer> offers)
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
            offer.Price,
            offer.Delivery,
            offer.Warranty,
            offer.Status,
            offer.Preferred
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

    private LlmSuggestionResult NormalizePayload(LlmPayload? payload, LlmSuggestionResult fallback, string providerName)
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
            .Take(4)
            .ToList();

        return new LlmSuggestionResult(
            true,
            text.Pick($"LLM ativo · {providerName}", $"LLM active · {providerName}"),
            CleanText(payload.Intent, fallback.Intent),
            CleanText(payload.Summary, fallback.Summary),
            payload.Confidence <= 0 ? fallback.Confidence : Math.Clamp(payload.Confidence, 0, 100),
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

    private LlmSuggestionResult BuildFallback(string query, IReadOnlyList<ProductResult> products, IReadOnlyList<SellerOffer> offers)
    {
        var bestProduct = products.OrderByDescending(product => product.Score).FirstOrDefault();
        var bestOffer = offers.OrderBy(offer => offer.PriceCents).FirstOrDefault();

        var summary = bestProduct is null
            ? text.Pick("Define orçamento, garantia e risco para gerar uma comparação mais forte.", "Define budget, warranty and risk to generate a stronger comparison.")
            : text.Pick(
                $"Melhor ponto de partida: {bestProduct.Name}. Melhor vendedor conhecido: {bestOffer?.Seller ?? "confirmar"} {bestOffer?.Price ?? string.Empty}.",
                $"Best starting point: {bestProduct.Name}. Best known seller: {bestOffer?.Seller ?? "confirm"} {bestOffer?.Price ?? string.Empty}.");

        var leads = products
            .OrderByDescending(product => product.Score)
            .Take(3)
            .Select(product => new LlmProductLead(
                product.Name,
                product.AiSummary,
                product.Price,
                text.Pick($"{product.Brand} {product.Name} melhor preço garantia Portugal", $"{product.Brand} {product.Name} best price warranty Portugal"),
                text.Pick("Catálogo", "Catalog")))
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
            BuildFallbackSignals(bestProduct, bestOffer),
            BuildFallbackQueries(query),
            warnings,
            leads);
    }

    private IReadOnlyList<string> BuildFallbackSignals(ProductResult? product, SellerOffer? offer)
    {
        var signals = new List<string>();
        if (product is not null)
        {
            signals.AddRange(product.Highlights.Take(3));
            signals.Add($"{product.Score}% match");
        }

        if (offer is not null)
        {
            signals.Add($"{offer.Seller}: {offer.Price}");
        }

        return signals.Count > 0
            ? signals
            : new[] { text.Pick("Preço", "Price"), text.Pick("Garantia", "Warranty"), text.Pick("Risco", "Risk"), text.Pick("Entrega", "Delivery") };
    }

    private IReadOnlyList<string> BuildFallbackQueries(string query)
    {
        return text.IsEnglish
            ? new[]
            {
                $"{query} best price authorized seller",
                $"{query} warranty Portugal",
                $"{query} alternative best price",
                $"{query} target price alert"
            }
            : new[]
            {
                $"{query} melhor preço vendedor autorizado",
                $"{query} garantia Portugal",
                $"{query} alternativa melhor preço",
                $"{query} alerta preço alvo"
            };
    }

    private static IReadOnlyList<T> Clean<T>(IReadOnlyList<T>? values)
    {
        return values ?? Array.Empty<T>();
    }

    private static string CleanText(string? value, string fallback = "")
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private sealed class LlmPayload
    {
        public string? Intent { get; set; }

        public string? Summary { get; set; }

        public int Confidence { get; set; }

        public IReadOnlyList<string>? BuyingSignals { get; set; }

        public IReadOnlyList<string>? SuggestedQueries { get; set; }

        public IReadOnlyList<string>? Warnings { get; set; }

        public IReadOnlyList<LlmLeadPayload>? ProductLeads { get; set; }
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
