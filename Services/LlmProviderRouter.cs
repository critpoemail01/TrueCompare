using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using TrueCompare.Options;

namespace TrueCompare.Services;

public sealed class LlmProviderRouter(
    HttpClient httpClient,
    IOptions<LlmOptions> optionsAccessor,
    LlmProviderQuotaService quotaService,
    ILogger<LlmProviderRouter> logger,
    IConfiguration? configuration = null)
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
            var candidates = await ResolveProviderCandidatesAsync(provider, operation, requiresVision, cancellationToken);
            if (provider.IsLocal && candidates.Count > 1)
            {
                var bestLocalResponse = await TryGetBestParallelResponseAsync(
                    operation,
                    candidates,
                    buildRequestJson,
                    isValidJson,
                    cancellationToken);

                if (bestLocalResponse is not null)
                {
                    return bestLocalResponse;
                }

                continue;
            }

            var response = await TryGetProviderResponseAsync(
                operation,
                candidates[0],
                buildRequestJson,
                isValidJson,
                cancellationToken);

            if (response is not null)
            {
                return response;
            }
        }

        return null;
    }

    private async Task<LlmProviderResponse?> TryGetBestParallelResponseAsync(
        string operation,
        IReadOnlyList<LlmProviderOptions> providers,
        Func<LlmProviderOptions, string> buildRequestJson,
        Func<string, bool> isValidJson,
        CancellationToken cancellationToken)
    {
        var tasks = providers
            .Select((provider, index) => TryGetProviderResponseAsync(
                    operation,
                    provider,
                    buildRequestJson,
                    isValidJson,
                    cancellationToken)
                .ContinueWith(
                    task => new ProviderCandidateResult(index, task.Status == TaskStatus.RanToCompletion ? task.Result : null),
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default))
            .ToArray();

        var results = await Task.WhenAll(tasks);
        var validResponses = results
            .Where(result => result.Response is not null)
            .OrderBy(result => result.Index)
            .Select(result => result.Response)
            .Cast<LlmProviderResponse>()
            .ToList();

        if (validResponses.Count <= 1)
        {
            return validResponses.FirstOrDefault();
        }

        var mergedJson = MergeJsonResponses(validResponses.Select(response => response.JsonContent).ToList());
        return isValidJson(mergedJson)
            ? new LlmProviderResponse($"{validResponses[0].ProviderName} + validação cruzada", mergedJson)
            : validResponses[0];
    }

    private async Task<LlmProviderResponse?> TryGetProviderResponseAsync(
        string operation,
        LlmProviderOptions provider,
        Func<LlmProviderOptions, string> buildRequestJson,
        Func<string, bool> isValidJson,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured(provider, out var apiKey))
        {
            logger.LogInformation("LLM provider {Provider} skipped because it is not configured", provider.Name);
            return null;
        }

        if (!quotaService.TryReserve(provider, out var quotaReason))
        {
            logger.LogInformation("LLM provider {Provider} skipped for {Operation}: {Reason}", provider.Name, operation, quotaReason);
            return null;
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
                MarkRateLimited(provider, response.Headers.RetryAfter?.Delta);
                logger.LogWarning("LLM provider {Provider} reached remote rate limit during {Operation}", provider.Name, operation);
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                MarkTemporarilyUnavailable(provider);
                logger.LogWarning(
                    "LLM provider {Provider} failed during {Operation} with status {StatusCode}",
                    provider.Name,
                    operation,
                    response.StatusCode);
                return null;
            }

            var responseJson = await response.Content.ReadAsStringAsync(timeout.Token);
            var content = ExtractAssistantContent(responseJson);
            if (!isValidJson(content))
            {
                MarkTemporarilyUnavailable(provider);
                logger.LogWarning("LLM provider {Provider} returned invalid JSON for {Operation}", provider.Name, operation);
                return null;
            }

            return new LlmProviderResponse(provider.Name, content);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            MarkTemporarilyUnavailable(provider);
            logger.LogWarning("LLM provider {Provider} timed out during {Operation}", provider.Name, operation);
        }
        catch (Exception ex)
        {
            MarkTemporarilyUnavailable(provider);
            logger.LogWarning(ex, "LLM provider {Provider} failed during {Operation}", provider.Name, operation);
        }

        return null;
    }

    private async Task<IReadOnlyList<LlmProviderOptions>> ResolveProviderCandidatesAsync(
        LlmProviderOptions provider,
        string operation,
        bool requiresVision,
        CancellationToken cancellationToken)
    {
        if (!provider.IsLocal || !options.UseAvailableLocalModels)
        {
            return [provider];
        }

        var availableModels = await TryGetAvailableOllamaModelsAsync(provider, cancellationToken);
        if (availableModels.Count == 0)
        {
            return [provider];
        }

        var cloudModels = availableModels
            .Where(model => model.Contains("cloud", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var modelPool = cloudModels.Count > 0 ? cloudModels : availableModels;

        var candidates = modelPool
            .Where(model => !requiresVision || LooksVisionCapable(model))
            .OrderByDescending(model => ScoreLocalModel(model, operation, requiresVision))
            .ThenBy(model => model, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Clamp(options.MaxParallelLocalModels, 1, 8))
            .Select(model => CloneProviderWithModel(provider, model))
            .ToList();

        return candidates.Count > 0 ? candidates : [provider];
    }

    private async Task<IReadOnlyList<string>> TryGetAvailableOllamaModelsAsync(
        LlmProviderOptions provider,
        CancellationToken cancellationToken)
    {
        if (!TryBuildOllamaTagsEndpoint(provider.Endpoint, out var tagsEndpoint))
        {
            return [];
        }

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(options.LocalModelDiscoveryTimeoutSeconds, 2, 20)));

            using var response = await httpClient.GetAsync(tagsEndpoint, timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogInformation("Ollama model discovery failed with status {StatusCode}", response.StatusCode);
                return [];
            }

            var json = await response.Content.ReadAsStringAsync(timeout.Token);
            return ParseOllamaModelNames(json);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("Ollama model discovery timed out");
            return [];
        }
        catch (Exception ex)
        {
            logger.LogInformation(ex, "Ollama model discovery failed");
            return [];
        }
    }

    private static IReadOnlyList<string> ParseOllamaModelNames(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("models", out var models) || models.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return models
            .EnumerateArray()
            .Select(model =>
                model.TryGetProperty("name", out var name) ? name.GetString()
                : model.TryGetProperty("model", out var modelName) ? modelName.GetString()
                : null)
            .Where(model => !string.IsNullOrWhiteSpace(model))
            .Select(model => model!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static LlmProviderOptions CloneProviderWithModel(LlmProviderOptions provider, string model)
    {
        return new LlmProviderOptions
        {
            Name = $"{provider.Name} · {model}",
            Enabled = provider.Enabled,
            Priority = provider.Priority,
            IsLocal = provider.IsLocal,
            Endpoint = provider.Endpoint,
            ApiKey = provider.ApiKey,
            ApiKeyEnvironmentVariable = provider.ApiKeyEnvironmentVariable,
            RequiresApiKey = provider.RequiresApiKey,
            Model = model,
            SupportsVision = provider.SupportsVision || LooksVisionCapable(model),
            SupportsJsonObjectResponseFormat = provider.SupportsJsonObjectResponseFormat,
            RequestsPerMinute = provider.RequestsPerMinute,
            RequestsPerDay = provider.RequestsPerDay,
            TimeoutSeconds = provider.TimeoutSeconds,
            MaxTokens = provider.MaxTokens,
            Temperature = provider.Temperature,
            Headers = new Dictionary<string, string>(provider.Headers, StringComparer.OrdinalIgnoreCase)
        };
    }

    private static int ScoreLocalModel(string model, string operation, bool requiresVision)
    {
        var score = 0;
        var isProductTask = operation.Contains("product", StringComparison.OrdinalIgnoreCase)
            || operation.Contains("suggestion", StringComparison.OrdinalIgnoreCase)
            || operation.Contains("discovery", StringComparison.OrdinalIgnoreCase)
            || operation.Contains("image", StringComparison.OrdinalIgnoreCase);

        if (model.Contains("cloud", StringComparison.OrdinalIgnoreCase))
        {
            score += 1000;
        }

        if (LooksVisionCapable(model))
        {
            score += requiresVision ? 500 : 120;
        }

        if (model.Contains("qwen", StringComparison.OrdinalIgnoreCase))
        {
            score += 180;
        }

        if (isProductTask)
        {
            if (LooksVisionCapable(model))
            {
                score += 260;
            }

            if (model.Contains("deepseek", StringComparison.OrdinalIgnoreCase))
            {
                score += 220;
            }

            if (model.Contains("gpt-oss:120b", StringComparison.OrdinalIgnoreCase)
                || model.Contains("minimax", StringComparison.OrdinalIgnoreCase)
                || model.Contains("glm", StringComparison.OrdinalIgnoreCase))
            {
                score += 160;
            }

            if (model.Contains("coder", StringComparison.OrdinalIgnoreCase))
            {
                score -= 420;
            }
        }

        if (model.Contains("coder", StringComparison.OrdinalIgnoreCase)
            && operation.Contains("code", StringComparison.OrdinalIgnoreCase))
        {
            score += 160;
        }

        score += ExtractModelSizeScore(model);
        return score;
    }

    private static int ExtractModelSizeScore(string model)
    {
        var match = System.Text.RegularExpressions.Regex.Match(model, @"(?<size>\d+)\s*b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return match.Success && int.TryParse(match.Groups["size"].Value, out var size)
            ? Math.Min(size, 400)
            : 0;
    }

    private static bool LooksVisionCapable(string model)
    {
        return model.Contains("vl", StringComparison.OrdinalIgnoreCase)
            || model.Contains("vision", StringComparison.OrdinalIgnoreCase)
            || model.Contains("llava", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryBuildOllamaTagsEndpoint(string endpoint, out Uri tagsEndpoint)
    {
        tagsEndpoint = default!;
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
        {
            return false;
        }

        tagsEndpoint = new Uri($"{uri.Scheme}://{uri.Authority}/api/tags");
        return true;
    }

    private static string MergeJsonResponses(IReadOnlyList<string> jsonResponses)
    {
        if (jsonResponses.Count == 0)
        {
            return "{}";
        }

        var merged = JsonNode.Parse(jsonResponses[0]) ?? new JsonObject();
        foreach (var jsonResponse in jsonResponses.Skip(1))
        {
            var next = JsonNode.Parse(jsonResponse);
            merged = MergeJsonNodes(merged, next);
        }

        return merged.ToJsonString();
    }

    private static JsonNode MergeJsonNodes(JsonNode? primary, JsonNode? secondary)
    {
        if (primary is JsonObject primaryObject && secondary is JsonObject secondaryObject)
        {
            var result = (JsonObject)primaryObject.DeepClone();
            foreach (var property in secondaryObject)
            {
                if (!result.TryGetPropertyValue(property.Key, out var existing) || existing is null)
                {
                    result[property.Key] = property.Value?.DeepClone();
                    continue;
                }

                result[property.Key] = MergeJsonNodes(existing, property.Value);
            }

            return result;
        }

        if (primary is JsonArray primaryArray && secondary is JsonArray secondaryArray)
        {
            return MergeJsonArrays(primaryArray, secondaryArray);
        }

        return primary?.DeepClone() ?? secondary?.DeepClone() ?? new JsonObject();
    }

    private static JsonArray MergeJsonArrays(JsonArray primary, JsonArray secondary)
    {
        var result = new JsonArray();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in primary.Concat(secondary))
        {
            if (item is null)
            {
                continue;
            }

            var key = BuildJsonArrayItemKey(item);
            if (seen.Add(key))
            {
                result.Add(item.DeepClone());
            }
        }

        return result;
    }

    private static string BuildJsonArrayItemKey(JsonNode item)
    {
        if (item is not JsonObject jsonObject)
        {
            return item.ToJsonString();
        }

        foreach (var key in new[] { "slug", "name", "seller", "searchHint", "reason" })
        {
            if (jsonObject.TryGetPropertyValue(key, out var value) && value is not null)
            {
                var normalized = value.ToString().Trim();
                if (normalized.Length > 0)
                {
                    return $"{key}:{normalized}";
                }
            }
        }

        return item.ToJsonString();
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
            .OrderBy(provider => provider.IsLocal ? 0 : 1)
            .ThenBy(provider => provider.Priority)
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
            IsLocal = IsLocalEndpoint(options.Endpoint),
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
        var name = string.IsNullOrWhiteSpace(provider.Name) ? provider.Model : provider.Name;
        var endpoint = string.IsNullOrWhiteSpace(provider.Endpoint) ? options.Endpoint : provider.Endpoint;

        return new LlmProviderOptions
        {
            Name = name,
            Enabled = provider.Enabled,
            Priority = provider.Priority,
            IsLocal = provider.IsLocal || IsLocalProvider(name, endpoint),
            Endpoint = endpoint,
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

    private void MarkRateLimited(LlmProviderOptions provider, TimeSpan? retryAfter)
    {
        if (!provider.IsLocal)
        {
            quotaService.MarkRateLimited(provider.Name, retryAfter);
        }
    }

    private void MarkTemporarilyUnavailable(LlmProviderOptions provider)
    {
        if (!provider.IsLocal)
        {
            quotaService.MarkTemporarilyUnavailable(provider.Name, TimeSpan.FromSeconds(options.ProviderCooldownSeconds));
        }
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
            var providerKey = Environment.GetEnvironmentVariable(provider.ApiKeyEnvironmentVariable)
                ?? configuration?[provider.ApiKeyEnvironmentVariable];

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

    private static bool IsLocalProvider(string name, string endpoint)
    {
        return name.Contains("ollama", StringComparison.OrdinalIgnoreCase)
            || name.Contains("local", StringComparison.OrdinalIgnoreCase)
            || IsLocalEndpoint(endpoint);
    }

    private static bool IsLocalEndpoint(string endpoint)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.IsLoopback)
        {
            return true;
        }

        if (!IPAddress.TryParse(uri.Host, out var address))
        {
            return uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase);
        }

        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        if (address.AddressFamily != AddressFamily.InterNetwork)
        {
            return false;
        }

        var bytes = address.GetAddressBytes();
        return bytes[0] == 10
            || (bytes[0] == 172 && bytes[1] is >= 16 and <= 31)
            || (bytes[0] == 192 && bytes[1] == 168)
            || (bytes[0] == 169 && bytes[1] == 254);
    }
}

public sealed record LlmProviderResponse(string ProviderName, string JsonContent);

internal sealed record ProviderCandidateResult(int Index, LlmProviderResponse? Response);
