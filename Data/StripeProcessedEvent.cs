namespace TrueCompare.Data;

public sealed class StripeProcessedEvent
{
    public int Id { get; set; }

    public string StripeEventId { get; set; } = string.Empty;

    public string EventType { get; set; } = string.Empty;

    public string Status { get; set; } = StripeProcessedEventStatus.Processing;

    public string? Error { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public DateTime? ProcessedUtc { get; set; }
}

public static class StripeProcessedEventStatus
{
    public const string Processing = "processing";
    public const string Processed = "processed";
    public const string Failed = "failed";
}
