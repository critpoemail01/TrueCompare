namespace TrueCompare.Data;

public sealed class SearchResultSnapshot
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = default!;

    public int? SearchRequestId { get; set; }

    public SearchRequest? SearchRequest { get; set; }

    public string Query { get; set; } = string.Empty;

    public string IdempotencyKey { get; set; } = string.Empty;

    public string ProductsJson { get; set; } = "[]";

    public string OffersJson { get; set; } = "[]";

    public string ProductOfferSummariesJson { get; set; } = "[]";

    public string? AiSuggestionsJson { get; set; }

    public int ResultCount { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresUtc { get; set; } = DateTime.UtcNow.AddHours(6);
}
