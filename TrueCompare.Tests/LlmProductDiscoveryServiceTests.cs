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
              "category": "Cadeiras de escritorio",
              "confidence": 88,
              "products": [
                {
                  "name": "IKEA FLINTAN",
                  "brand": "IKEA",
                  "price": "79,99 €",
                  "priceCents": 7999,
                  "score": 86,
                  "badge": "Melhor valor",
                  "specs": ["Cadeira escritorio", "Altura ajustavel", "Apoio lombar", "Garantia 3 anos"],
                  "highlights": ["Dentro do orçamento", "Boa ergonomia", "Facil de encontrar"],
                  "verificationChecks": ["Produto real", "Vendedor oficial a validar"],
                  "summary": "Boa cadeira de escritorio dentro do orçamento.",
                  "warnings": [],
                  "sellerOffers": [
                    {
                      "seller": "IKEA",
                      "price": "79,99 €",
                      "priceCents": 7999,
                      "delivery": "Confirmar loja",
                      "warranty": "Validar vendedor",
                      "status": "A confirmar",
                      "url": "https://www.ikea.com/pt/pt/search/?q=FLINTAN",
                      "preferred": true
                    }
                  ]
                }
              ]
            }
            """;
        var handler = FakeHttpMessageHandler.ReturningJson(WrapOpenAiResponse(assistantJson));
        var service = CreateService(handler);

        var result = await service.DiscoverAsync("quero uma cadeira escritorio ate 100 euros");

        Assert.True(result.FromLlm);
        Assert.Contains("Provider A", result.SourceLabel);
        Assert.Contains(result.Products, product => product.Name == "IKEA FLINTAN");
        Assert.DoesNotContain(result.Products, product => product.Name == "MacBook Air M3");
        Assert.Equal("IKEA", result.Offers[0].Seller);
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

        var result = await service.DiscoverAsync("quero uma cadeira escritorio ate 100 euros");

        Assert.False(result.FromLlm);
        Assert.DoesNotContain(result.Products, product => product.Name == "MacBook Air M3");
        Assert.Contains(result.Products, product => product.Specs.Any(spec => spec.Contains("Cadeira", StringComparison.OrdinalIgnoreCase)));
    }

    private static LlmProductDiscoveryService CreateService(HttpMessageHandler handler)
    {
        var router = new LlmProviderRouter(
            new HttpClient(handler),
            Microsoft.Extensions.Options.Options.Create(new LlmOptions
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
            }),
            new LlmProviderQuotaService(),
            NullLogger<LlmProviderRouter>.Instance);

        return new LlmProductDiscoveryService(
            new ComparisonDataService(new AppText()),
            router,
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<LlmProductDiscoveryService>.Instance,
            new AppText());
    }

    private static string WrapOpenAiResponse(string assistantJson)
    {
        return JsonSerializer.Serialize(new
        {
            choices = new[] { new { message = new { content = assistantJson } } }
        });
    }
}
