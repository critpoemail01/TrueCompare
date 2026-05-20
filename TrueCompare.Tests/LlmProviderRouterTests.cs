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

    private static string WrapOpenAiResponse(string assistantJson)
    {
        return JsonSerializer.Serialize(new
        {
            choices = new[] { new { message = new { content = assistantJson } } }
        });
    }
}
