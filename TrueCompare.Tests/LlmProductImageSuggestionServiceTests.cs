using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using TrueCompare.Options;
using TrueCompare.Services;
using TrueCompare.Tests.Support;

namespace TrueCompare.Tests;

public sealed class LlmProductImageSuggestionServiceTests
{
    [Fact]
    public async Task AnalyzeImageAsync_UsesLocalFallback_WhenLlmIsNotConfigured()
    {
        using var culture = UseCulture("pt-PT");
        var handler = new FakeHttpMessageHandler(_ => throw new InvalidOperationException("HTTP should not be called."));
        var service = CreateService(handler, new LlmOptions
        {
            ApiKey = string.Empty,
            Enabled = true
        });

        var result = await service.AnalyzeImageAsync(
            "bosch-serie-6-frigorifico.jpg",
            "image/jpeg",
            [1, 2, 3]);

        Assert.False(result.FromLlm);
        Assert.Equal("Modo local", result.SourceLabel);
        Assert.Contains("frigor", result.DetectedProductType, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("bosch", result.SuggestedQuery, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AnalyzeImageAsync_SendsVisionPayload_AndParsesSuggestion()
    {
        using var culture = UseCulture("pt-PT");
        var assistantJson = """
            {
              "detectedProductType": "Frigorifico Bosch Serie 6",
              "suggestedQuery": "bosch serie 6 frigorifico melhor preco vendedor autorizado",
              "summary": "Imagem compatível com frigorifico Bosch Serie 6.",
              "confidence": 88,
              "warnings": ["Confirmar etiqueta energetica"],
              "productLeads": [
                {
                  "name": "Bosch Serie 6 Frigorifico",
                  "reason": "Modelo provável pela imagem.",
                  "searchHint": "bosch serie 6 frigorifico",
                  "source": "LLM"
                }
              ]
            }
            """;
        var openAiResponse = JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        content = assistantJson
                    }
                }
            }
        });
        var handler = FakeHttpMessageHandler.ReturningJson(openAiResponse);
        var service = CreateService(handler, new LlmOptions
        {
            ApiKey = "test-key",
            Enabled = true,
            Endpoint = "https://llm.example.test/v1/chat/completions",
            Model = "test-model"
        });

        var result = await service.AnalyzeImageAsync(
            "produto.png",
            "image/png",
            [10, 20, 30]);

        Assert.True(result.FromLlm);
        Assert.Equal("LLM online", result.SourceLabel);
        Assert.Equal("Frigorifico Bosch Serie 6", result.DetectedProductType);
        Assert.Equal("bosch serie 6 frigorifico melhor preco vendedor autorizado", result.SuggestedQuery);
        Assert.Equal(88, result.Confidence);
        Assert.Contains(result.ProductLeads, lead => lead.Name == "Bosch Serie 6 Frigorifico");
        Assert.Equal(new AuthenticationHeaderValue("Bearer", "test-key"), handler.LastRequest?.Headers.Authorization);
        Assert.Contains("\"type\":\"image_url\"", handler.LastBody);
        Assert.Contains("data:image/png;base64", handler.LastBody);
    }

    private static LlmProductImageSuggestionService CreateService(HttpMessageHandler handler, LlmOptions options)
    {
        return new LlmProductImageSuggestionService(
            new HttpClient(handler),
            Microsoft.Extensions.Options.Options.Create(options),
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<LlmProductImageSuggestionService>.Instance,
            new AppText());
    }

    private static CultureScope UseCulture(string cultureName)
    {
        return new CultureScope(cultureName);
    }

    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo originalCulture = CultureInfo.CurrentCulture;
        private readonly CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;

        public CultureScope(string cultureName)
        {
            var culture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }
}
