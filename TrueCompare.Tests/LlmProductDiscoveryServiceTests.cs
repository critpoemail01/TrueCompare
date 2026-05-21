using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TrueCompare.Options;
using TrueCompare.Services;
using TrueCompare.Tests.Support;

namespace TrueCompare.Tests;

public sealed class LlmProductDiscoveryServiceTests
{
    [Fact]
    public async Task DiscoverAsync_UsesLlmProducts_WhenNoLocalCatalogMatches()
    {
        var assistantJson = """
            {
              "category": "Microfones USB",
              "confidence": 88,
              "products": [
                {
                  "name": "Blue Snowball iCE",
                  "brand": "Logitech",
                  "price": "54,99 €",
                  "priceCents": 5499,
                  "score": 86,
                  "badge": "Melhor valor",
                  "specs": ["Microfone USB", "Cardioide", "Podcast", "Garantia 2 anos"],
                  "highlights": ["Dentro do orçamento", "Boa voz", "Facil de encontrar"],
                  "verificationChecks": ["Produto real", "Vendedor oficial a validar"],
                  "summary": "Bom microfone USB para podcast dentro do orçamento.",
                  "warnings": [],
                  "sellerOffers": [
                    {
                      "seller": "Worten",
                      "price": "54,99 €",
                      "priceCents": 5499,
                      "delivery": "Confirmar loja",
                      "warranty": "Validar vendedor",
                      "status": "A confirmar",
                      "url": "https://www.worten.pt/search?query=Blue%20Snowball%20iCE",
                      "preferred": true
                    }
                  ]
                }
              ]
            }
            """;
        var handler = FakeHttpMessageHandler.ReturningJson(WrapOpenAiResponse(assistantJson));
        var service = CreateService(handler);

        var result = await service.DiscoverAsync("quero um microfone usb para podcast ate 100 euros");

        Assert.True(result.FromLlm);
        Assert.Contains("Provider A", result.SourceLabel);
        Assert.Contains(result.Products, product => product.Name == "Blue Snowball iCE");
        Assert.DoesNotContain(result.Products, product => product.Name == "MacBook Air M3");
        Assert.Equal("Worten", result.Offers[0].Seller);
    }

    [Fact]
    public async Task DiscoverAsync_RejectsWrongLlmProducts_AndUsesValidatedFallback()
    {
        var assistantJson = """
            {
              "category": "Portateis",
              "confidence": 82,
              "products": [
                {
                  "name": "MacBook Air M3",
                  "brand": "Apple",
                  "price": "1299 €",
                  "priceCents": 129900,
                  "score": 90,
                  "badge": "Recomendado",
                  "specs": ["Portatil", "14 polegadas", "1.24 kg", "M3"],
                  "highlights": ["Leve"],
                  "verificationChecks": ["Produto real"],
                  "summary": "Portatil premium.",
                  "warnings": []
                }
              ]
            }
            """;
        var handler = FakeHttpMessageHandler.ReturningJson(WrapOpenAiResponse(assistantJson));
        var service = CreateService(handler);

        var result = await service.DiscoverAsync("quero um microfone usb para podcast ate 100 euros");

        Assert.False(result.FromLlm);
        Assert.DoesNotContain(result.Products, product => product.Name == "MacBook Air M3");
        Assert.Contains(result.Products, product => product.Specs.Any(spec => spec.Contains("Microfone", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task DiscoverAsync_DoesNotClampFallbackPricesToExplicitBudget()
    {
        var assistantJson = """
            {
              "category": "Discos externos",
              "confidence": 88,
              "products": [
                {
                  "name": "Western Digital My Passport 1TB",
                  "brand": "Western Digital",
                  "price": "58,80 €",
                  "priceCents": 5880,
                  "score": 88,
                  "badge": "Melhor escolha",
                  "specs": ["Disco externo", "1TB USB 3.2"],
                  "highlights": ["Compacto"],
                  "verificationChecks": ["Produto real"],
                  "summary": "Disco externo acima do orçamento."
                }
              ]
            }
            """;
        var handler = FakeHttpMessageHandler.ReturningJson(WrapOpenAiResponse(assistantJson));
        var service = CreateService(handler);

        var result = await service.DiscoverAsync("disco externo ate 20 euros");

        Assert.Empty(result.Products);
        Assert.Empty(result.Offers);
        Assert.False(result.FromLlm);
    }

    [Fact]
    public async Task DiscoverAsync_CallsOllamaCloudModelsInParallel_AndMergesOnlyRelevantValidatedProducts()
    {
        var postedModels = new List<string>();
        var currentConcurrency = 0;
        var maxConcurrency = 0;
        var handler = new FakeHttpMessageHandler(async request =>
        {
            if (request.Method == HttpMethod.Get && request.RequestUri?.AbsolutePath == "/api/tags")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new
                    {
                        models = new[]
                        {
                            new { name = "qwen3-vl:235b-cloud" },
                            new { name = "deepseek-v3.1:671b-cloud" },
                            new { name = "gpt-oss:120b-cloud" },
                            new { name = "qwen3-coder:480b-cloud" }
                        }
                    }))
                };
            }

            var active = Interlocked.Increment(ref currentConcurrency);
            try
            {
                maxConcurrency = Math.Max(maxConcurrency, active);
                var body = request.Content is null ? "{}" : await request.Content.ReadAsStringAsync();
                var model = ExtractRequestModel(body);
                lock (postedModels)
                {
                    postedModels.Add(model);
                }

                await Task.Delay(80);
                var assistantJson = model switch
                {
                    "qwen3-vl:235b-cloud" => BuildDiscoveryProductJson(
                        "Microfones USB",
                        "Blue Snowball iCE",
                        "Logitech",
                        "54,99 €",
                        5499,
                        "Microfone USB",
                        "Bom microfone USB para podcast dentro do orçamento."),
                    "gpt-oss:120b-cloud" => BuildDiscoveryProductJson(
                        "Microfones USB",
                        "Rode NT-USB Mini",
                        "Rode",
                        "89,00 €",
                        8900,
                        "Microfone USB",
                        "Microfone compacto para voz e chamadas."),
                    _ => BuildDiscoveryProductJson(
                        "Portáteis",
                        "MacBook Air M3",
                        "Apple",
                        "1299 €",
                        129900,
                        "Portátil",
                        "Produto de categoria errada para este pedido.")
                };

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(WrapOpenAiResponse(assistantJson))
                };
            }
            finally
            {
                Interlocked.Decrement(ref currentConcurrency);
            }
        });
        var service = CreateService(handler, new LlmOptions
        {
            Enabled = true,
            UseAvailableLocalModels = true,
            MaxParallelLocalModels = 3,
            Providers =
            [
                new LlmProviderOptions
                {
                    Name = "Ollama local",
                    IsLocal = true,
                    Endpoint = "http://172.20.10.55:11434/v1/chat/completions",
                    Model = "qwen3-vl:235b-cloud",
                    RequiresApiKey = false,
                    Priority = 1,
                    SupportsVision = true
                }
            ]
        });

        var result = await service.DiscoverAsync("quero um microfone usb para podcast ate 100 euros");

        Assert.True(maxConcurrency > 1);
        Assert.Contains("qwen3-vl:235b-cloud", postedModels);
        Assert.Contains("gpt-oss:120b-cloud", postedModels);
        Assert.DoesNotContain("qwen3-coder:480b-cloud", postedModels);
        Assert.True(result.FromLlm);
        Assert.Contains("valida", result.SourceLabel, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(result.Products, product => product.Name == "Blue Snowball iCE");
        Assert.Contains(result.Products, product => product.Name == "Rode NT-USB Mini");
        Assert.DoesNotContain(result.Products, product => product.Name == "MacBook Air M3");
        Assert.All(result.Products, product =>
        {
            Assert.Contains(product.Specs, spec => spec.Contains("Microfone", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(product.Specs, spec => spec.Contains("Portátil", StringComparison.OrdinalIgnoreCase));
        });
    }

    [Theory]
    [InlineData("quero um microfone usb para podcast ate 100 euros", "Microfone", "Portátil")]
    [InlineData("procuro uma impressora multifuncoes wifi", "Multifun", "Smartphone")]
    [InlineData("preciso de fraldas bebe tamanho 4", "Fralda", "Ração")]
    [InlineData("teclado sem fios ate 80 euros", "Logitech", "Rato")]
    [InlineData("procurar carregador de iphone", "Carregador", "iPhone 17")]
    public async Task DiscoverAsync_ReturnsProductsThatMatchDifferentUserQuestions(string query, string expectedTerm, string rejectedTerm)
    {
        var service = CreateService(new FakeHttpMessageHandler(_ => throw new InvalidOperationException("Local catalog or validated fallback should answer.")));

        var result = await service.DiscoverAsync(query);

        Assert.NotEmpty(result.Products);
        Assert.All(result.Products, product =>
        {
            var productText = string.Join(' ', product.Name, product.Brand, product.Badge, product.AiSummary, string.Join(' ', product.Specs));
            Assert.Contains(expectedTerm, productText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(rejectedTerm, productText, StringComparison.OrdinalIgnoreCase);
        });
    }

    private static LlmProductDiscoveryService CreateService(HttpMessageHandler handler)
    {
        return CreateService(handler, new LlmOptions
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
                }
            ]
        });
    }

    private static LlmProductDiscoveryService CreateService(HttpMessageHandler handler, LlmOptions options)
    {
        var router = new LlmProviderRouter(
            new HttpClient(handler),
            Microsoft.Extensions.Options.Options.Create(options),
            new LlmProviderQuotaService(),
            NullLogger<LlmProviderRouter>.Instance);

        return new LlmProductDiscoveryService(
            new ComparisonDataService(new AppText()),
            router,
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<LlmProductDiscoveryService>.Instance,
            new AppText());
    }

    private static string BuildDiscoveryProductJson(
        string category,
        string name,
        string brand,
        string price,
        int priceCents,
        string productType,
        string summary)
    {
        return JsonSerializer.Serialize(new
        {
            category,
            confidence = 88,
            products = new[]
            {
                new
                {
                    name,
                    brand,
                    price,
                    priceCents,
                    score = 86,
                    badge = "Validado",
                    specs = new[] { productType, "Garantia 2 anos", "Vendedor a confirmar", "Preço plausível" },
                    highlights = new[] { "Boa correspondência com o pedido", "Preço dentro do orçamento" },
                    verificationChecks = new[] { "Produto real", "Categoria validada" },
                    summary,
                    warnings = Array.Empty<string>(),
                    sellerOffers = Array.Empty<object>()
                }
            }
        });
    }

    private static string ExtractRequestModel(string body)
    {
        using var document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty("model").GetString() ?? string.Empty;
    }

    private static string WrapOpenAiResponse(string assistantJson)
    {
        return JsonSerializer.Serialize(new
        {
            choices = new[] { new { message = new { content = assistantJson } } }
        });
    }
}
