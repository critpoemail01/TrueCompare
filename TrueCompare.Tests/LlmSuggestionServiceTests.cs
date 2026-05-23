using System.Globalization;
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
        using var culture = UseCulture("pt-PT");
        var data = new ComparisonDataService(new AppText());
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
    public async Task GetSuggestionsAsync_DoesNotExposeNonLiveSellerPricesAsConfirmed()
    {
        using var culture = UseCulture("pt-PT");
        var data = new ComparisonDataService(new AppText());
        var service = CreateService(new FakeHttpMessageHandler(_ => throw new InvalidOperationException("HTTP should not be called.")), new LlmOptions
        {
            ApiKey = string.Empty,
            Enabled = true
        });

        var result = await service.GetSuggestionsAsync(
            "macbook air m3",
            data.GetProducts("macbook air m3"),
            data.GetSellerOffers("macbook-air-m3"));

        Assert.False(result.FromLlm);
        Assert.Contains("confirmar na loja", result.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("€", result.Summary);
        Assert.Contains(result.BuyingSignals, signal => signal.Contains("confirmar na loja", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(result.BuyingSignals, signal => signal.Contains("€", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetSuggestionsAsync_ParsesOpenAiCompatibleJsonResponse()
    {
        using var culture = UseCulture("pt-PT");
        var data = new ComparisonDataService(new AppText());
        var assistantJson = """
            {
              "intent": "Smartphone premium",
              "summary": "Escolhe um modelo com suporte longo e vendedor autorizado.",
              "confidence": 91,
              "buyingSignals": ["garantia UE", "vendedor autorizado"],
              "suggestedQueries": ["smartphone premium garantia Portugal"],
              "warnings": ["Desconfia de pre\u00e7os 40% abaixo do mercado"],
              "productLeads": [
                {
                  "name": "iPhone 15 Pro",
                  "reason": "Melhor equil\u00edbrio entre suporte, c\u00e2mara e revenda.",
                  "targetPrice": "at\u00e9 900 \u20ac",
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
        Assert.StartsWith("LLM ativo", result.SourceLabel);
        Assert.Equal("Smartphone premium", result.Intent);
        Assert.Equal(91, result.Confidence);
        Assert.Contains(result.ProductLeads, lead => lead.Name == "iPhone 15 Pro" && lead.TargetPrice == "at\u00e9 900 \u20ac");
        Assert.Equal(new AuthenticationHeaderValue("Bearer", "test-key"), handler.LastRequest?.Headers.Authorization);
        Assert.Contains("comprar smartphones", handler.LastBody);
    }

    [Fact]
    public async Task GetSuggestionsAsync_AcceptsConfidenceAsPercentString()
    {
        using var culture = UseCulture("pt-PT");
        var data = new ComparisonDataService(new AppText());
        var assistantJson = """
            {
              "intent": "iPhone 17",
              "summary": "Resposta local aproveitavel.",
              "confidence": "91%",
              "buyingSignals": ["modelo exato"],
              "suggestedQueries": ["iPhone 17 vendedor autorizado"],
              "warnings": [],
              "productLeads": [
                {
                  "name": "iPhone 17",
                  "reason": "Corresponde ao modelo pedido.",
                  "targetPrice": "confirmar",
                  "searchHint": "iPhone 17 Apple",
                  "source": "Ollama local"
                }
              ]
            }
            """;
        var openAiResponse = JsonSerializer.Serialize(new
        {
            choices = new[] { new { message = new { content = assistantJson } } }
        });
        var handler = FakeHttpMessageHandler.ReturningJson(openAiResponse);
        var service = CreateService(handler, new LlmOptions
        {
            Enabled = true,
            Providers =
            [
                new LlmProviderOptions
                {
                    Name = "Ollama local",
                    IsLocal = true,
                    Endpoint = "http://172.20.10.55:11434/v1/chat/completions",
                    Model = "qwen3-vl:235b-cloud",
                    RequiresApiKey = false,
                    Priority = 10
                }
            ]
        });

        var result = await service.GetSuggestionsAsync(
            "telemovel iphone 17",
            data.GetProducts("telemovel iphone 17"),
            data.GetSellerOffers("telemovel iphone 17"));

        Assert.True(result.FromLlm);
        Assert.Contains("Ollama local", result.SourceLabel);
        Assert.Equal(91, result.Confidence);
    }

    [Fact]
    public async Task GetSuggestionsAsync_FallsBack_WhenProviderFails()
    {
        using var culture = UseCulture("pt-PT");
        var data = new ComparisonDataService(new AppText());
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

    [Fact]
    public async Task GetSuggestionsAsync_UsesNextProvider_WhenFirstResponseIsInvalid()
    {
        using var culture = UseCulture("pt-PT");
        var data = new ComparisonDataService(new AppText());
        var validAssistantJson = """
            {
              "intent": "Smartphone premium",
              "summary": "Resposta validada no segundo provider.",
              "confidence": 90,
              "buyingSignals": ["suporte longo"],
              "suggestedQueries": ["smartphone premium vendedor autorizado"],
              "warnings": [],
              "productLeads": [
                {
                  "name": "iPhone 15 Pro",
                  "reason": "Boa c\u00e2mara e suporte longo.",
                  "targetPrice": "confirmar",
                  "searchHint": "iPhone 15 Pro vendedor autorizado",
                  "source": "Provider B"
                }
              ]
            }
            """;
        var invalidResponse = JsonSerializer.Serialize(new
        {
            choices = new[] { new { message = new { content = "{\"summary\":\"sem campos obrigatorios\"}" } } }
        });
        var validResponse = JsonSerializer.Serialize(new
        {
            choices = new[] { new { message = new { content = validAssistantJson } } }
        });
        var callCount = 0;
        var handler = new FakeHttpMessageHandler(_ =>
        {
            callCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(callCount == 1 ? invalidResponse : validResponse)
            });
        });
        var service = CreateService(handler, OptionsWithProviders());

        var result = await service.GetSuggestionsAsync(
            "comprar smartphones",
            data.GetProducts("comprar smartphones"),
            data.GetSellerOffers("comprar smartphones"));

        Assert.True(result.FromLlm);
        Assert.Contains("Provider B", result.SourceLabel);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task GetSuggestionsAsync_UsesNextProvider_WhenFirstProviderIsRateLimited()
    {
        using var culture = UseCulture("pt-PT");
        var data = new ComparisonDataService(new AppText());
        var validAssistantJson = """
            {
              "intent": "Smartphone premium",
              "summary": "Resposta validada ap\u00f3s rate limit.",
              "confidence": 90,
              "buyingSignals": ["suporte longo"],
              "suggestedQueries": ["smartphone premium vendedor autorizado"],
              "warnings": [],
              "productLeads": [
                {
                  "name": "iPhone 15 Pro",
                  "reason": "Boa c\u00e2mara e suporte longo.",
                  "targetPrice": "confirmar",
                  "searchHint": "iPhone 15 Pro vendedor autorizado",
                  "source": "Provider B"
                }
              ]
            }
            """;
        var validResponse = JsonSerializer.Serialize(new
        {
            choices = new[] { new { message = new { content = validAssistantJson } } }
        });
        var callCount = 0;
        var handler = new FakeHttpMessageHandler(_ =>
        {
            callCount++;
            return Task.FromResult(callCount == 1
                ? new HttpResponseMessage(HttpStatusCode.TooManyRequests)
                : new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(validResponse)
                });
        });
        var service = CreateService(handler, OptionsWithProviders());

        var result = await service.GetSuggestionsAsync(
            "comprar smartphones",
            data.GetProducts("comprar smartphones"),
            data.GetSellerOffers("comprar smartphones"));

        Assert.True(result.FromLlm);
        Assert.Contains("Provider B", result.SourceLabel);
        Assert.Equal(2, callCount);
    }

    private static LlmSuggestionService CreateService(HttpMessageHandler handler, LlmOptions options)
    {
        var router = new LlmProviderRouter(
            new HttpClient(handler),
            Microsoft.Extensions.Options.Options.Create(options),
            new LlmProviderQuotaService(),
            NullLogger<LlmProviderRouter>.Instance);

        return new LlmSuggestionService(
            router,
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<LlmSuggestionService>.Instance,
            new AppText());
    }

    private static LlmOptions OptionsWithProviders()
    {
        return new LlmOptions
        {
            Enabled = true,
            Providers =
            [
                new LlmProviderOptions
                {
                    Name = "Provider A",
                    Endpoint = "https://provider-a.example.test/v1/chat/completions",
                    Model = "provider-a-model",
                    RequiresApiKey = false,
                    Priority = 1
                },
                new LlmProviderOptions
                {
                    Name = "Provider B",
                    Endpoint = "https://provider-b.example.test/v1/chat/completions",
                    Model = "provider-b-model",
                    RequiresApiKey = false,
                    Priority = 2
                }
            ]
        };
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


