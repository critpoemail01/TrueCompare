namespace TrueCompare.Data;

public sealed class SearchRequest
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = default!;

    public string Query { get; set; } = string.Empty;

    public bool UsedPaidCredit { get; set; }

    public bool UsedFreeSearch { get; set; }

    public string Status { get; set; } = SearchRequestStatus.Committed;

    public string? IdempotencyKey { get; set; }

    public string? FailureReason { get; set; }

    public int ResultCount { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public DateTime? CommittedUtc { get; set; }

    public DateTime? RefundedUtc { get; set; }
}

public static class SearchRequestStatus
{
    public const string Pending = "Pending";
    public const string Committed = "Committed";
    public const string Refunded = "Refunded";
    public const string Failed = "Failed";
}
