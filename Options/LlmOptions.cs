namespace TrueCompare.Options;

public sealed class LlmOptions
{
    public bool Enabled { get; set; } = true;

    public string Endpoint { get; set; } = "https://api.openai.com/v1/chat/completions";

    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "gpt-4o-mini";

    public int TimeoutSeconds { get; set; } = 20;

    public int MaxTokens { get; set; } = 900;

    public decimal Temperature { get; set; } = 0.2m;

    public int ProviderCooldownSeconds { get; set; } = 60;

    public List<LlmProviderOptions> Providers { get; set; } = [];
}

public sealed class LlmProviderOptions
{
    public string Name { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;

    public int Priority { get; set; } = 100;

    public string Endpoint { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string ApiKeyEnvironmentVariable { get; set; } = string.Empty;

    public bool RequiresApiKey { get; set; } = true;

    public string Model { get; set; } = string.Empty;

    public bool SupportsVision { get; set; }

    public bool SupportsJsonObjectResponseFormat { get; set; } = true;

    public int RequestsPerMinute { get; set; }

    public int RequestsPerDay { get; set; }

    public int TimeoutSeconds { get; set; }

    public int MaxTokens { get; set; }

    public decimal? Temperature { get; set; }

    public Dictionary<string, string> Headers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
