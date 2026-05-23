namespace TrueCompare.ReliabilityGate;

public sealed record ReliabilityFixture(
    MarketCatalog Catalog,
    IReadOnlyDictionary<string, FixtureChatGptReference> ChatGptReferences);

public sealed record FixtureChatGptReference(
    string ResponseText,
    string? ModelUsed,
    string? ReasoningMode,
    IReadOnlyList<string> RecommendedProductNames);
