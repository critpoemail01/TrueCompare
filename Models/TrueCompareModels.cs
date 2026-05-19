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
    string AiSummary);

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
    bool Preferred);

public sealed record TutorialItem(
    string Category,
    string Title,
    string Duration);

public sealed record FraudAlert(
    string Source,
    string Reason);
