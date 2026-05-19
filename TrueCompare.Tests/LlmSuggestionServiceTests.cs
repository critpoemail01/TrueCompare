using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TrueCompare.Options;
using TrueCompare.Services;
using TrueCompare.Tests.Support;

namespace TrueCompare.Tests;

public sealed class LlmSuggestionServiceTests
{
    [Fact]
    public async Task GetSuggestionsAsync_UsesLocalFallback_WhenLlmIsNotConfigured()
    {
        var data = new ComparisonDataService();
        var service = CreateService(new FakeHttpMessageHandler(_ => throw new InvalidOperationException("HTTP should not be called.")), new LlmOptions
        {
            ApiKey = string.Empty,
            Enabled = true
        });

        var result = await service.GetSuggestionsAsync(
            "comprar smartphones",
            data.GetProducts("comprar smartphones"),
            data.GetSellerOffers("comprar smartphones"));

        Assert.False(result.FromLlm);
        Assert.Equal("Modo local", result.SourceLabel);
        Assert.Contains(result.ProductLeads, lead => lead.Name == "iPhone 15 Pro");
        Assert.Contains(result.SuggestedQueries, query => query.Contains("melhor preço", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetSuggestionsAsync_ParsesOpenAiCompatibleJsonResponse()
    {
        var data = new ComparisonDataService();
        var assistantJson = """
            {
              "intent": "Smartphone premium",
              "summary": "Escolhe um modelo com suporte longo e vendedor autorizado.",
              "confidence": 91,
              "buyingSignals": ["garantia UE", "vendedor autorizado"],
              "suggestedQueries": ["smartphone premium garantia Portugal"],
              "warnings": ["Desconfia de preços 40% abaixo do mercado"],
              "productLeads": [
                {
                  "name": "iPhone 15 Pro",
                  "reason": "Melhor equilíbrio entre suporte, câmara e revenda.",
                  "targetPrice": "até 900 €",
                  "searchHint": "iPhone 15 Pro vendedor autorizado",
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

        var result = await service.GetSuggestionsAsync(
            "comprar smartphones",
            data.GetProducts("comprar smartphones"),
            data.GetSellerOffers("comprar smartphones"));

        Assert.True(result.FromLlm);
        Assert.Equal("LLM ativo", result.SourceLabel);
        Assert.Equal("Smartphone premium", result.Intent);
        Assert.Equal(91, result.Confidence);
        Assert.Contains(result.ProductLeads, lead => lead.Name == "iPhone 15 Pro" && lead.TargetPrice == "até 900 €");
        Assert.Equal(new AuthenticationHeaderValue("Bearer", "test-key"), handler.LastRequest?.Headers.Authorization);
        Assert.Contains("comprar smartphones", handler.LastBody);
    }

    [Fact]
    public async Task GetSuggestionsAsync_FallsBack_WhenProviderFails()
    {
        var data = new ComparisonDataService();
        var handler = new FakeHttpMessageHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));
        var service = CreateService(handler, new LlmOptions
        {
            ApiKey = "test-key",
            Enabled = true,
            Endpoint = "https://llm.example.test/v1/chat/completions",
            Model = "test-model"
        });

        var result = await service.GetSuggestionsAsync(
            "comprar smartphones",
            data.GetProducts("comprar smartphones"),
            data.GetSellerOffers("comprar smartphones"));

        Assert.False(result.FromLlm);
        Assert.Equal("Modo local", result.SourceLabel);
    }

    private static LlmSuggestionService CreateService(HttpMessageHandler handler, LlmOptions options)
    {
        return new LlmSuggestionService(
            new HttpClient(handler),
            Microsoft.Extensions.Options.Options.Create(options),
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<LlmSuggestionService>.Instance);
    }
}
