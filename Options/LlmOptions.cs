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
}
