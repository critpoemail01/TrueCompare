namespace TrueCompare.Options;

public sealed class StripeOptions
{
    public string SecretKey { get; set; } = string.Empty;

    public string WebhookSecret { get; set; } = string.Empty;

    public IReadOnlyList<CreditPackage> CreditPackages { get; set; } = Array.Empty<CreditPackage>();
}

public sealed class CreditPackage
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int Credits { get; set; }

    public long AmountCents { get; set; }

    public string Currency { get; set; } = "eur";
}
