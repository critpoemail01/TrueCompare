namespace TrueCompare.Data;

public sealed class SearchRequest
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = default!;

    public string Query { get; set; } = string.Empty;

    public bool UsedPaidCredit { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
