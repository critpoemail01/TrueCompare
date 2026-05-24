namespace TrueCompare.Options;

public sealed class SearchQuotaOptions
{
    public bool LocalUnlimitedEnabled { get; set; }

    public int PendingReservationExpiryMinutes { get; set; } = 60;

    public int SnapshotTtlHours { get; set; } = 6;

    public int SnapshotCleanupIntervalHours { get; set; } = 6;
}
