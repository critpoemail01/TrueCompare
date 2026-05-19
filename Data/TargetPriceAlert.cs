namespace TrueCompare.Data;

public sealed class TargetPriceAlert
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = default!;

    public string ProductSlug { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public long TargetPriceCents { get; set; }

    public long LastSeenPriceCents { get; set; }

    public string? LastSeenSeller { get; set; }

    public string? ProductUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public bool EmailSent { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public DateTime? TriggeredUtc { get; set; }
}
