using System.Text.Json.Serialization;

namespace TrueCompare.ReliabilityGate;

public enum ReliabilityVerdict
{
    Passou,
    Falhou,
    Inconclusivo
}

public enum EvidenceStatus
{
    Validated,
    Blocked,
    Inconclusive,
    NotValidable
}

public sealed record ReliabilityRunOptions(
    bool Live,
    bool LiveKuantoKusta,
    bool LiveChatGptBrowser,
    string? CategoryFilter,
    int MaxSubcategories,
    int MaxProductsPerSubcategory,
    int PromptsPerSubcategory,
    string OutputDirectory,
    string FixturePath,
    string ChatGptModelPreference,
    string ChatGptReasoningMode,
    string? ChromeCdpEndpoint,
    string? ChromeUserDataDir,
    string AppBaseUrl);

public sealed record MarketCatalog(
    IReadOnlyList<MarketCategory> Categories,
    EvidenceStatus Status = EvidenceStatus.Validated,
    string? BlockReason = null,
    DateTimeOffset CapturedAt = default);

public sealed record MarketCategory(
    string Name,
    string Url,
    IReadOnlyList<MarketSubcategory> Subcategories);

public sealed record MarketSubcategory(
    string Category,
    string Name,
    string Url,
    IReadOnlyList<MarketProduct> Products,
    EvidenceStatus Status = EvidenceStatus.Validated,
    string? NotValidableReason = null,
    string? HtmlSnapshotPath = null,
    string? ScreenshotPath = null);

public sealed record MarketProduct(
    string Name,
    string Brand,
    string Model,
    long MinPriceCents,
    long? MaxPriceCents,
    string Url,
    IReadOnlyList<string> Stores,
    IReadOnlyList<string> Badges,
    string? Rating,
    string? Availability);

public sealed record ReliabilityCase(
    string Id,
    string Category,
    string Subcategory,
    string SubcategoryUrl,
    string UserPrompt,
    IReadOnlyList<MarketProduct> Products);

public sealed record ChatGptReference(
    EvidenceStatus Status,
    string PromptSent,
    string ResponseText,
    string? ModelUsed,
    string? ReasoningMode,
    DateTimeOffset Timestamp,
    string? ScreenshotPath,
    string? BlockReason,
    IReadOnlyList<string> RecommendedProductNames);

public sealed record AppRecommendation(
    EvidenceStatus Status,
    string PromptSent,
    string ResponseText,
    DateTimeOffset Timestamp,
    IReadOnlyList<AppRecommendedProduct> Products,
    string? ScreenshotPath = null,
    string? BlockReason = null);

public sealed record AppRecommendedProduct(
    string Name,
    string Brand,
    string Price,
    long PriceCents,
    string Badge,
    string Justification);

public sealed record CaseComparisonResult(
    string CaseId,
    string Category,
    string Subcategory,
    string UserPrompt,
    EvidenceStatus Status,
    int Score,
    bool CriticalFailure,
    IReadOnlyList<string> CriticalFailures,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<ProductMatchResult> ProductMatches,
    ChatGptReference ChatGpt,
    AppRecommendation App,
    IReadOnlyList<MarketProduct> MarketProducts);

public sealed record ProductMatchResult(
    string AppProduct,
    string? MatchedMarketProduct,
    string? MatchedChatGptProduct,
    int NameSimilarity,
    int PriceScore,
    long? AppPriceCents,
    long? MarketPriceCents,
    bool ExistsInMarket,
    bool CategoryCorrect,
    bool PriceCompatible);

public sealed record ReliabilityReport(
    ReliabilityVerdict Verdict,
    string VerdictText,
    DateTimeOffset StartedAt,
    DateTimeOffset FinishedAt,
    int TotalCategories,
    int TotalSubcategories,
    int ValidableSubcategories,
    int NotValidableSubcategories,
    int TotalCases,
    int BlockedCases,
    int InconclusiveCases,
    int CriticalFailureCount,
    double AggregateScore,
    IReadOnlyDictionary<string, double> ScoreByCategory,
    IReadOnlyList<CaseComparisonResult> Cases,
    IReadOnlyList<MarketSubcategory> NotValidableSubcategoriesList,
    IReadOnlyList<string> RunWarnings)
{
    [JsonIgnore]
    public bool Passed => Verdict == ReliabilityVerdict.Passou;
}
