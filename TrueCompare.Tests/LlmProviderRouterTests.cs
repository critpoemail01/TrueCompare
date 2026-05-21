using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TrueCompare.Options;
using TrueCompare.Services;
using TrueCompare.Tests.Support;

namespace TrueCompare.Tests;

public sealed class LlmProviderRouterTests
{
    [Fact]
    public async Task TryGetValidJsonAsync_UsesApiKeyFromConfiguration_WhenEnvironmentVariableNameIsConfigured()
    {
        const string apiKeySettingName = "TRUECOMPARE_TEST_PROVIDER_KEY";
        var handler = FakeHttpMessageHandler.ReturningJson(WrapOpenAiResponse("{\"ok\":true}"));
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [apiKeySettingName] = "config-secret"
            })
            .Build();

        var router = new LlmProviderRouter(
            new HttpClient(handler),
            Microsoft.Extensions.Options.Options.Create(new LlmOptions
            {
                Enabled = true,
                Providers =
                [
                    new LlmProviderOptions
                    {
                        Name = "Config Provider",
                        Endpoint = "https://provider.example.test/v1/chat/completions",
                        ApiKeyEnvironmentVariable = apiKeySettingName,
                        Model = "config-model",
                        RequiresApiKey = true,
                        Priority = 1
                    }
                ]
            }),
            new LlmProviderQuotaService(),
            NullLogger<LlmProviderRouter>.Instance,
            configuration);

        var response = await router.TryGetValidJsonAsync(
            "test",
            requiresVision: false,
            _ => "{\"model\":\"config-model\",\"messages\":[]}",
            content => content.Contains("\"ok\":true", StringComparison.Ordinal));

        Assert.NotNull(response);
        Assert.Equal("Config Provider", response.ProviderName);
        Assert.Equal(new AuthenticationHeaderValue("Bearer", "config-secret"), handler.LastRequest?.Headers.Authorization);
    }

    [Fact]
    public async Task TryGetValidJsonAsync_TriesLocalProviderFirstOnEveryCallBeforeFreeFallback()
    {
        var requests = new List<string>();
        var handler = new FakeHttpMessageHandler(request =>
        {
            requests.Add(request.RequestUri?.Host ?? string.Empty);
            return Task.FromResult(request.RequestUri?.Host == "172.20.10.55"
                ? new HttpResponseMessage(HttpStatusCode.NotFound)
                : new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(WrapOpenAiResponse("{\"ok\":true}"))
                });
        });

        var router = new LlmProviderRouter(
            new HttpClient(handler),
            Microsoft.Extensions.Options.Options.Create(new LlmOptions
            {
                Enabled = true,
                ProviderCooldownSeconds = 600,
                Providers =
                [
                    new LlmProviderOptions
                    {
                        Name = "Groq free",
                        Endpoint = "https://api.groq.com/openai/v1/chat/completions",
                        Model = "free-model",
                        RequiresApiKey = false,
                        Priority = 1
                    },
                    new LlmProviderOptions
                    {
                        Name = "Ollama local",
                        IsLocal = true,
                        Endpoint = "http://172.20.10.55:11434/v1/chat/completions",
                        Model = "qwen3-vl:235b-cloud",
                        RequiresApiKey = false,
                        Priority = 50
                    }
                ]
            }),
            new LlmProviderQuotaService(),
            NullLogger<LlmProviderRouter>.Instance);

        for (var index = 0; index < 2; index++)
        {
            var response = await router.TryGetValidJsonAsync(
                "test",
                requiresVision: false,
                _ => "{\"messages\":[]}",
                content => content.Contains("\"ok\":true", StringComparison.Ordinal));

            Assert.NotNull(response);
            Assert.Equal("Groq free", response.ProviderName);
        }

        Assert.Equal(
            new[] { "172.20.10.55", "api.groq.com", "172.20.10.55", "api.groq.com" },
            requests);
    }

    [Fact]
    public async Task TryGetValidJsonAsync_ExpandsAvailableOllamaCloudModels_AndChoosesBestValidatedCandidate()
    {
        var postedModels = new List<string>();
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
                            new { name = "llama3.2-vision:11b-cloud" },
                            new { name = "qwen3-vl:235b-cloud" },
                            new { name = "qwen2.5-coder:7b" }
                        }
                    }))
                };
            }

            var body = request.Content is null ? "{}" : await request.Content.ReadAsStringAsync();
            var model = ExtractRequestModel(body);
            lock (postedModels)
            {
                postedModels.Add(model);
            }

            var content = model == "qwen3-vl:235b-cloud"
                ? "{\"ok\":true}"
                : "{\"ok\":false}";

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(WrapOpenAiResponse(content))
            };
        });

        var router = new LlmProviderRouter(
            new HttpClient(handler),
            Microsoft.Extensions.Options.Options.Create(new LlmOptions
            {
                Enabled = true,
                UseAvailableLocalModels = true,
                MaxParallelLocalModels = 2,
                Providers =
                [
                    new LlmProviderOptions
                    {
                        Name = "Ollama local",
                        IsLocal = true,
                        Endpoint = "http://172.20.10.55:11434/v1/chat/completions",
                        Model = "fallback-model",
                        RequiresApiKey = false,
                        Priority = 1,
                        SupportsVision = true
                    }
                ]
            }),
            new LlmProviderQuotaService(),
            NullLogger<LlmProviderRouter>.Instance);

        var response = await router.TryGetValidJsonAsync(
            "product-discovery",
            requiresVision: false,
            provider => JsonSerializer.Serialize(new { model = provider.Model, messages = Array.Empty<object>() }),
            content => content.Contains("\"ok\":true", StringComparison.Ordinal));

        Assert.NotNull(response);
        Assert.Contains("qwen3-vl:235b-cloud", response.ProviderName);
        Assert.Contains("qwen3-vl:235b-cloud", postedModels);
        Assert.Contains("llama3.2-vision:11b-cloud", postedModels);
        Assert.DoesNotContain("qwen2.5-coder:7b", postedModels);
    }

    [Fact]
    public async Task TryGetValidJsonAsync_MergesMultipleValidatedLocalResponses()
    {
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
                            new { name = "deepseek-v3.1:671b-cloud" }
                        }
                    }))
                };
            }

            var body = request.Content is null ? "{}" : await request.Content.ReadAsStringAsync();
            var model = ExtractRequestModel(body);
            var content = model.Contains("qwen", StringComparison.OrdinalIgnoreCase)
                ? "{\"confidence\":88,\"products\":[{\"name\":\"Produto A\"}]}"
                : "{\"confidence\":91,\"products\":[{\"name\":\"Produto B\"}]}";

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(WrapOpenAiResponse(content))
            };
        });

        var router = new LlmProviderRouter(
            new HttpClient(handler),
            Microsoft.Extensions.Options.Options.Create(new LlmOptions
            {
                Enabled = true,
                UseAvailableLocalModels = true,
                MaxParallelLocalModels = 2,
                Providers =
                [
                    new LlmProviderOptions
                    {
                        Name = "Ollama local",
                        IsLocal = true,
                        Endpoint = "http://172.20.10.55:11434/v1/chat/completions",
                        Model = "fallback-model",
                        RequiresApiKey = false,
                        Priority = 1,
                        SupportsVision = true
                    }
                ]
            }),
            new LlmProviderQuotaService(),
            NullLogger<LlmProviderRouter>.Instance);

        var response = await router.TryGetValidJsonAsync(
            "product-discovery",
            requiresVision: false,
            provider => JsonSerializer.Serialize(new { model = provider.Model, messages = Array.Empty<object>() }),
            content => content.Contains("\"products\"", StringComparison.Ordinal));

        Assert.NotNull(response);
        Assert.Contains("validação cruzada", response.ProviderName);
        Assert.Contains("Produto A", response.JsonContent);
        Assert.Contains("Produto B", response.JsonContent);
    }

    private static string WrapOpenAiResponse(string assistantJson)
    {
        return JsonSerializer.Serialize(new
        {
            choices = new[] { new { message = new { content = assistantJson } } }
        });
    }

    private static string ExtractRequestModel(string body)
    {
        using var document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty("model").GetString() ?? string.Empty;
    }
}
