using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TrueCompare.Options;

namespace TrueCompare.Services;

public sealed class LlmProviderRouter(
    HttpClient httpClient,
    IOptions<LlmOptions> optionsAccessor,
    LlmProviderQuotaService quotaService,
    ILogger<LlmProviderRouter> logger)
{
    private readonly LlmOptions options = optionsAccessor.Value;

    public async Task<LlmProviderResponse?> TryGetValidJsonAsync(
        string operation,
        bool requiresVision,
        Func<LlmProviderOptions, string> buildRequestJson,
        Func<string, bool> isValidJson,
        CancellationToken cancellationToken = default)
    {
        if (!options.Enabled)
        {
            return null;
        }

        foreach (var provider in ResolveProviders(requiresVision))
        {
            if (!IsConfigured(provider, out var apiKey))
            {
                logger.LogInformation("LLM provider {Provider} skipped because it is not configured", provider.Name);
                continue;
            }

            if (!quotaService.TryReserve(provider, out var quotaReason))
            {
                logger.LogInformation("LLM provider {Provider} skipped for {Operation}: {Reason}", provider.Name, operation, quotaReason);
                continue;
            }

            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(provider.TimeoutSeconds, 5, 90)));

                using var request = new HttpRequestMessage(HttpMethod.Post, provider.Endpoint);
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                }

                foreach (var header in provider.Headers)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                request.Content = new StringContent(buildRequestJson(provider), Encoding.UTF8, "application/json");

                using var response = await httpClient.SendAsync(request, timeout.Token);
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    quotaService.MarkRateLimited(provider.Name, response.Headers.RetryAfter?.Delta);
                    logger.LogWarning("LLM provider {Provider} reached remote rate limit during {Operation}", provider.Name, operation);
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    quotaService.MarkTemporarilyUnavailable(provider.Name, TimeSpan.FromSeconds(options.ProviderCooldownSeconds));
                    logger.LogWarning(
                        "LLM provider {Provider} failed during {Operation} with status {StatusCode}",
                        provider.Name,
                        operation,
                        response.StatusCode);
                    continue;
                }

                var responseJson = await response.Content.ReadAsStringAsync(timeout.Token);
                var content = ExtractAssistantContent(responseJson);
                if (!isValidJson(content))
                {
                    quotaService.MarkTemporarilyUnavailable(provider.Name, TimeSpan.FromSeconds(options.ProviderCooldownSeconds));
                    logger.LogWarning("LLM provider {Provider} returned invalid JSON for {Operation}", provider.Name, operation);
                    continue;
                }

                return new LlmProviderResponse(provider.Name, content);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                quotaService.MarkTemporarilyUnavailable(provider.Name, TimeSpan.FromSeconds(options.ProviderCooldownSeconds));
                logger.LogWarning("LLM provider {Provider} timed out during {Operation}", provider.Name, operation);
            }
            catch (Exception ex)
            {
                quotaService.MarkTemporarilyUnavailable(provider.Name, TimeSpan.FromSeconds(options.ProviderCooldownSeconds));
                logger.LogWarning(ex, "LLM provider {Provider} failed during {Operation}", provider.Name, operation);
            }
        }

        return null;
    }

    private IReadOnlyList<LlmProviderOptions> ResolveProviders(bool requiresVision)
    {
        var configuredProviders = options.Providers.Count > 0
            ? options.Providers
            : [BuildLegacyProvider()];

        return configuredProviders
            .Where(provider => provider.Enabled)
            .Where(provider => !requiresVision || provider.SupportsVision)
            .Select(ApplyDefaults)
            .OrderBy(provider => provider.Priority)
            .ThenBy(provider => provider.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private LlmProviderOptions BuildLegacyProvider()
    {
        return new LlmProviderOptions
        {
            Name = "OpenAI",
            Enabled = true,
            Priority = 100,
            Endpoint = options.Endpoint,
            ApiKey = options.ApiKey,
            ApiKeyEnvironmentVariable = "TRUECOMPARE_LLM_API_KEY",
            RequiresApiKey = true,
            Model = options.Model,
            SupportsVision = true,
            SupportsJsonObjectResponseFormat = true,
            TimeoutSeconds = options.TimeoutSeconds,
            MaxTokens = options.MaxTokens,
            Temperature = options.Temperature
        };
    }

    private LlmProviderOptions ApplyDefaults(LlmProviderOptions provider)
    {
        return new LlmProviderOptions
        {
            Name = string.IsNullOrWhiteSpace(provider.Name) ? provider.Model : provider.Name,
            Enabled = provider.Enabled,
            Priority = provider.Priority,
            Endpoint = string.IsNullOrWhiteSpace(provider.Endpoint) ? options.Endpoint : provider.Endpoint,
            ApiKey = provider.ApiKey,
            ApiKeyEnvironmentVariable = provider.ApiKeyEnvironmentVariable,
            RequiresApiKey = provider.RequiresApiKey,
            Model = string.IsNullOrWhiteSpace(provider.Model) ? options.Model : provider.Model,
            SupportsVision = provider.SupportsVision,
            SupportsJsonObjectResponseFormat = provider.SupportsJsonObjectResponseFormat,
            RequestsPerMinute = provider.RequestsPerMinute,
            RequestsPerDay = provider.RequestsPerDay,
            TimeoutSeconds = provider.TimeoutSeconds <= 0 ? options.TimeoutSeconds : provider.TimeoutSeconds,
            MaxTokens = provider.MaxTokens <= 0 ? options.MaxTokens : provider.MaxTokens,
            Temperature = provider.Temperature ?? options.Temperature,
            Headers = provider.Headers
        };
    }

    private bool IsConfigured(LlmProviderOptions provider, out string apiKey)
    {
        apiKey = ResolveApiKey(provider);
        return Uri.TryCreate(provider.Endpoint, UriKind.Absolute, out _)
            && !string.IsNullOrWhiteSpace(provider.Name)
            && !string.IsNullOrWhiteSpace(provider.Model)
            && (!provider.RequiresApiKey || !string.IsNullOrWhiteSpace(apiKey));
    }

    private string ResolveApiKey(LlmProviderOptions provider)
    {
        if (!string.IsNullOrWhiteSpace(provider.ApiKey))
        {
            return provider.ApiKey;
        }

        if (!string.IsNullOrWhiteSpace(provider.ApiKeyEnvironmentVariable))
        {
            var providerKey = Environment.GetEnvironmentVariable(provider.ApiKeyEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(providerKey))
            {
                return providerKey;
            }
        }

        return Environment.GetEnvironmentVariable("TRUECOMPARE_LLM_API_KEY")
            ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            ?? string.Empty;
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

    private static string ExtractJsonObject(string content)
    {
        var trimmed = content.Trim();
        var firstBrace = trimmed.IndexOf('{');
        var lastBrace = trimmed.LastIndexOf('}');

        return firstBrace >= 0 && lastBrace > firstBrace
            ? trimmed[firstBrace..(lastBrace + 1)]
            : trimmed;
    }
}

public sealed record LlmProviderResponse(string ProviderName, string JsonContent);
