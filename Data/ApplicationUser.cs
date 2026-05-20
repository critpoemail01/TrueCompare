using Microsoft.AspNetCore.Identity;

namespace TrueCompare.Data;

public sealed class ApplicationUser : IdentityUser
{
    public const int FreeSearchLimit = 3;

    public int FreeSearchesUsed { get; set; }

    public int Credits { get; set; }

    public bool HasUnlimitedSubscription { get; set; }

    public string? SubscriptionPlanId { get; set; }

    public string? StripeSubscriptionId { get; set; }

    public DateTime? SubscriptionActiveUntilUtc { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
