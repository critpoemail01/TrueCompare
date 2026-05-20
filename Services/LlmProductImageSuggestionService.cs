using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TrueCompare.Models;
using TrueCompare.Options;

namespace TrueCompare.Services;

public sealed class LlmProductImageSuggestionService(
    HttpClient httpClient,
    IOptions<LlmOptions> optionsAccessor,
    IMemoryCache cache,
    ILogger<LlmProductImageSuggestionService> logger,
    AppText text) : IProductImageSuggestionService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    private readonly LlmOptions options = optionsAccessor.Value;

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
        if (imageBytes.Length == 0 || !IsConfigured())
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
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 5, 60)));

            using var request = new HttpRequestMessage(HttpMethod.Post, options.Endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ResolveApiKey());
            request.Content = new StringContent(
                BuildRequestJson(normalizedFileName, normalizedContentType, imageBytes),
                Encoding.UTF8,
                "application/json");

            using var response = await httpClient.SendAsync(request, timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Image LLM request failed with status {StatusCode}", response.StatusCode);
                return fallback;
            }

            var responseJson = await response.Content.ReadAsStringAsync(timeout.Token);
            var content = ExtractAssistantContent(responseJson);
            var parsed = JsonSerializer.Deserialize<ImagePayload>(content, JsonOptions);
            var result = NormalizePayload(parsed, fallback);

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

    private bool IsConfigured()
    {
        return options.Enabled
            && Uri.TryCreate(options.Endpoint, UriKind.Absolute, out _)
            && !string.IsNullOrWhiteSpace(options.Model)
            && !string.IsNullOrWhiteSpace(ResolveApiKey());
    }

    private string ResolveApiKey()
    {
        if (!string.IsNullOrWhiteSpace(options.ApiKey))
        {
            return options.ApiKey;
        }

        return Environment.GetEnvironmentVariable("TRUECOMPARE_LLM_API_KEY")
            ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            ?? string.Empty;
    }

    private string BuildRequestJson(string fileName, string contentType, byte[] imageBytes)
    {
        var dataUrl = $"data:{contentType};base64,{Convert.ToBase64String(imageBytes)}";
        var request = new
        {
            model = options.Model,
            temperature = options.Temperature,
            max_tokens = Math.Clamp(options.MaxTokens, 300, 1400),
            response_format = new { type = "json_object" },
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

    private static string ExtractAssistantContent(string responseJson)
    {
        using var document = JsonDocument.Parse(responseJson);
        var content = document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("LLM response did not include assistant content.");
        }

        return ExtractJsonObject(content);
    }

    private ImageProductSuggestionResult NormalizePayload(ImagePayload? payload, ImageProductSuggestionResult fallback)
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
            text.Pick("LLM online", "Online LLM"),
            CleanText(payload.DetectedProductType, fallback.DetectedProductType),
            suggestedQuery,
            CleanText(payload.Summary, fallback.Summary),
            payload.Confidence <= 0 ? fallback.Confidence : Math.Clamp(payload.Confidence, 0, 100),
            leads.Count > 0 ? leads : fallback.ProductLeads,
            Clean(payload.Warnings).Select(item => CleanText(item)).Where(item => item.Length > 0).Take(4).ToList());
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

    private static string ExtractJsonObject(string content)
    {
        var trimmed = content.Trim();
        var firstBrace = trimmed.IndexOf('{');
        var lastBrace = trimmed.LastIndexOf('}');

        return firstBrace >= 0 && lastBrace > firstBrace
            ? trimmed[firstBrace..(lastBrace + 1)]
            : trimmed;
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
