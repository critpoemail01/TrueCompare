namespace TrueCompare.Data;

public sealed class CreditPurchase
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = default!;

    public string StripeSessionId { get; set; } = string.Empty;

    public string? StripePaymentIntentId { get; set; }

    public string? StripeSubscriptionId { get; set; }

    public string BillingKind { get; set; } = CreditBillingKind.OneTime;

    public string? PlanId { get; set; }

    public string? BillingPeriod { get; set; }

    public int Credits { get; set; }

    public long AmountCents { get; set; }

    public string Currency { get; set; } = "eur";

    public string Status { get; set; } = PurchaseStatus.Pending;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedUtc { get; set; }
}

public static class PurchaseStatus
{
    public const string Pending = "pending";
    public const string Paid = "paid";
    public const string Failed = "failed";
}

public static class CreditBillingKind
{
    public const string OneTime = "one_time";
    public const string Subscription = "subscription";
}
