using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using TrueCompare.Models;
using TrueCompare.Options;

namespace TrueCompare.Services;

public sealed class LlmProductImageSuggestionService(
    LlmProviderRouter providerRouter,
    IMemoryCache cache,
    ILogger<LlmProductImageSuggestionService> logger,
    AppText text) : IProductImageSuggestionService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    public async Task<ImageProductSuggestionResult> AnalyzeImageAsync(
        string fileName,
        string contentType,
        byte[] imageBytes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(imageBytes);

        var normalizedFileName = string.IsNullOrWhiteSpace(fileName)
            ? text.Pick("imagem-produto", "product-image")
            : fileName.Trim();
        var normalizedContentType = string.IsNullOrWhiteSpace(contentType)
            ? "image/jpeg"
            : contentType.Trim();

        var fallback = BuildFallback(normalizedFileName);
        if (imageBytes.Length == 0)
        {
            return fallback;
        }

        var cacheKey = $"llm-image:{normalizedFileName.ToLowerInvariant()}:{Convert.ToHexString(SHA256.HashData(imageBytes))}";
        if (cache.TryGetValue(cacheKey, out ImageProductSuggestionResult? cached) && cached is not null)
        {
            return cached;
        }

        try
        {
            var providerResponse = await providerRouter.TryGetValidJsonAsync(
                "image-product-suggestions",
                requiresVision: true,
                provider => BuildRequestJson(provider, normalizedFileName, normalizedContentType, imageBytes),
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
            logger.LogWarning("Image LLM request timed out");
            return fallback;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Image product suggestion failed");
            return fallback;
        }
    }

    private string BuildRequestJson(LlmProviderOptions provider, string fileName, string contentType, byte[] imageBytes)
    {
        var dataUrl = $"data:{contentType};base64,{Convert.ToBase64String(imageBytes)}";
        var request = new
        {
            model = provider.Model,
            temperature = provider.Temperature,
            max_tokens = Math.Clamp(provider.MaxTokens, 300, 1400),
            response_format = provider.SupportsJsonObjectResponseFormat ? new { type = "json_object" } : null,
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = """
                    Es o motor visual da TrueCompare. Analisa a imagem de produto e responde na mesma lingua da app. Devolve apenas JSON valido.
                    Identifica categoria, tipo de produto, marca/modelo visiveis e a melhor pesquisa para comparar esse produto online.
                    Nao inventes precos, stock, lojas, links ou disponibilidade. Se a imagem nao permitir identificar modelo, devolve a melhor pesquisa generica e baixa a confidence.
                    O JSON deve ter: detectedProductType, suggestedQuery, summary, confidence, warnings, productLeads.
                    productLeads deve ter no maximo 4 itens com name, reason, searchHint e source.
                    """
                },
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new
                        {
                            type = "text",
                            text = JsonSerializer.Serialize(new
                            {
                                idioma = text.IsEnglish ? "en-US" : "pt-PT",
                                ficheiro = fileName,
                                objetivo = text.Pick(
                                    "Identifica o produto da imagem e cria uma pesquisa pronta para encontrar melhores precos em vendedores autorizados.",
                                    "Identify the product in the image and create a ready search for best prices from authorized sellers.")
                            }, JsonOptions)
                        },
                        new
                        {
                            type = "image_url",
                            image_url = new
                            {
                                url = dataUrl
                            }
                        }
                    }
                }
            }
        };

        return JsonSerializer.Serialize(request, JsonOptions);
    }

    private static ImagePayload? ReadPayload(string content)
    {
        return JsonSerializer.Deserialize<ImagePayload>(content, JsonOptions);
    }

    private ImageProductSuggestionResult NormalizePayload(ImagePayload? payload, ImageProductSuggestionResult fallback, string providerName)
    {
        if (payload is null)
        {
            return fallback;
        }

        var leads = Clean(payload.ProductLeads)
            .Select(lead => new ImageProductLead(
                CleanText(lead.Name),
                CleanText(lead.Reason),
                CleanText(lead.SearchHint),
                CleanText(lead.Source, text.Pick("Sugestão IA", "AI suggestion"))))
            .Where(lead => !string.IsNullOrWhiteSpace(lead.Name) && !string.IsNullOrWhiteSpace(lead.SearchHint))
            .Take(4)
            .ToList();

        var suggestedQuery = CleanText(payload.SuggestedQuery, fallback.SuggestedQuery);
        return new ImageProductSuggestionResult(
            true,
            text.Pick($"LLM online · {providerName}", $"Online LLM · {providerName}"),
            CleanText(payload.DetectedProductType, fallback.DetectedProductType),
            suggestedQuery,
            CleanText(payload.Summary, fallback.Summary),
            payload.Confidence <= 0 ? fallback.Confidence : Math.Clamp(payload.Confidence, 0, 100),
            leads.Count > 0 ? leads : fallback.ProductLeads,
            Clean(payload.Warnings).Select(item => CleanText(item)).Where(item => item.Length > 0).Take(4).ToList());
    }

    private static bool IsValidPayload(ImagePayload? payload)
    {
        return payload is not null
            && payload.Confidence > 0
            && !string.IsNullOrWhiteSpace(payload.DetectedProductType)
            && !string.IsNullOrWhiteSpace(payload.SuggestedQuery)
            && !string.IsNullOrWhiteSpace(payload.Summary);
    }

    private ImageProductSuggestionResult BuildFallback(string fileName)
    {
        var normalizedFileName = fileName.ToLowerInvariant();

        if (ContainsAny(normalizedFileName, "frigor", "bosch", "geladeira", "combinado", "appliance"))
        {
            return BuildFallback(
                text.Pick("Eletrodoméstico / frigorífico", "Appliance / fridge"),
                text.Pick("bosch série 6 frigorífico melhor preço vendedor autorizado garantia Portugal", "bosch serie 6 fridge best price authorized seller warranty Portugal"),
                text.Pick("Imagem tratada como frigorífico ou eletrodoméstico. A pesquisa fica pronta para comparar eficiência, garantia e vendedor autorizado.", "Image treated as a fridge or appliance. The search is ready to compare efficiency, warranty and authorized sellers."),
                68,
                "Bosch Serie 6 Frigorifico");
        }

        if (ContainsAny(normalizedFileName, "iphone", "smartphone", "phone", "galaxy", "pixel", "xiaomi"))
        {
            return BuildFallback(
                text.Pick("Smartphone", "Smartphone"),
                text.Pick("smartphone premium boa câmara suporte longo vendedor autorizado", "premium smartphone good camera long support authorized seller"),
                text.Pick("Imagem tratada como smartphone. A pesquisa privilegia câmara, suporte, garantia e vendedores verificados.", "Image treated as a smartphone. The search prioritizes camera, support, warranty and verified sellers."),
                66,
                "Smartphone premium");
        }

        if (ContainsAny(normalizedFileName, "macbook", "laptop", "portatil", "notebook", "thinkpad", "xps", "zenbook"))
        {
            return BuildFallback(
                text.Pick("Portátil profissional", "Professional laptop"),
                text.Pick("portátil profissional 14 até 1500 autonomia >10h leve vendedor autorizado", "professional 14 laptop up to 1500 battery >10h light authorized seller"),
                text.Pick("Imagem tratada como portátil. A pesquisa fica orientada para autonomia, peso, preço e garantia.", "Image treated as a laptop. The search is oriented around battery, weight, price and warranty."),
                66,
                "Portátil profissional");
        }

        return BuildFallback(
            text.Pick("Produto por imagem", "Product from image"),
            text.Pick("produto da imagem melhor preço vendedor autorizado garantia Portugal", "product from image best price authorized seller warranty Portugal"),
            text.Pick("Não há chave LLM ativa ou a imagem ainda não foi identificada online. A pesquisa genérica mantém verificação de vendedor e garantia.", "No active LLM key or the image has not been identified online yet. The generic search keeps seller and warranty verification."),
            42,
            text.Pick("Produto identificado por imagem", "Image-identified product"));
    }

    private ImageProductSuggestionResult BuildFallback(
        string detectedProductType,
        string suggestedQuery,
        string summary,
        int confidence,
        string leadName)
    {
        return new ImageProductSuggestionResult(
            false,
            text.Pick("Modo local", "Local mode"),
            detectedProductType,
            suggestedQuery,
            summary,
            confidence,
            new[]
            {
                new ImageProductLead(
                    leadName,
                    text.Pick("Sugestão inicial para abrir a comparação TrueCompare.", "Initial suggestion to open the TrueCompare comparison."),
                    suggestedQuery,
                    text.Pick("Fallback local", "Local fallback"))
            },
            new[]
            {
                text.Pick("Configura uma chave LLM para reconhecimento visual online.", "Configure an LLM key for online visual recognition.")
            });
    }

    private static bool ContainsAny(string value, params string[] terms)
    {
        return terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<T> Clean<T>(IReadOnlyList<T>? values)
    {
        return values ?? Array.Empty<T>();
    }

    private static string CleanText(string? value, string fallback = "")
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private sealed class ImagePayload
    {
        public string? DetectedProductType { get; set; }

        public string? SuggestedQuery { get; set; }

        public string? Summary { get; set; }

        public int Confidence { get; set; }

        public IReadOnlyList<string>? Warnings { get; set; }

        public IReadOnlyList<ImageLeadPayload>? ProductLeads { get; set; }
    }

    private sealed class ImageLeadPayload
    {
        public string? Name { get; set; }

        public string? Reason { get; set; }

        public string? SearchHint { get; set; }

        public string? Source { get; set; }
    }
}
