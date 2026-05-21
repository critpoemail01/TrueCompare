namespace TrueCompare.Models;

public sealed record ProductResult(
    string Slug,
    int Rank,
    int Score,
    string Name,
    string Brand,
    string Price,
    string Accent,
    string Badge,
    IReadOnlyList<string> Specs,
    IReadOnlyList<string> Highlights,
    IReadOnlyList<string> VerificationChecks,
    IReadOnlyList<FraudAlert> FraudAlerts,
    string AiSummary)
{
    public ProductSourceLink? OfficialSource { get; init; }

    public IReadOnlyList<ProductSpecification> OfficialSpecifications { get; init; } = Array.Empty<ProductSpecification>();

    public IReadOnlyList<ProductReviewLink> ReviewLinks { get; init; } = Array.Empty<ProductReviewLink>();
}

public sealed record ProductSourceLink(
    string Label,
    string Url,
    string Description);

public sealed record ProductSpecification(
    string Label,
    string Value);

public sealed record ProductReviewLink(
    string Title,
    string Channel,
    string Url,
    string ViewSignal);

public sealed record CriteriaWeight(
    string Name,
    int Value,
    string Accent);

public sealed record FeatureItem(
    string Number,
    string Title,
    string Description,
    string Accent);

public sealed record SellerOffer(
    string Seller,
    string Price,
    long PriceCents,
    string Delivery,
    string Warranty,
    string Status,
    string Url,
    bool Preferred,
    int ReliabilityScore = 0,
    string LocationLabel = "",
    string Evidence = "",
    bool IsLivePrice = false);

public sealed record TutorialItem(
    string Category,
    string Title,
    string Duration);

public sealed record FraudAlert(
    string Source,
    string Reason);
