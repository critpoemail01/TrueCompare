using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TrueCompare.Data;
using TrueCompare.Models;
using TrueCompare.Options;

namespace TrueCompare.Services;

public sealed class SearchResultSnapshotService(
    ApplicationDbContext dbContext,
    IOptions<SearchQuotaOptions> optionsAccessor,
    ILogger<SearchResultSnapshotService> logger)
{
    private readonly SearchQuotaOptions options = optionsAccessor.Value;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    static SearchResultSnapshotService()
    {
        JsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public async Task<SearchExecutionResult?> GetByIdempotencyKeyAsync(
        string userId,
        string? idempotencyKey,
        string query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return null;
        }

        var normalizedQuery = NormalizeQuery(query);
        var now = DateTime.UtcNow;
        var snapshot = await dbContext.SearchResultSnapshots
            .AsNoTracking()
            .Where(candidate => candidate.UserId == userId
                && candidate.IdempotencyKey == idempotencyKey
                && candidate.Query == normalizedQuery
                && candidate.ExpiresUtc > now)
            .OrderByDescending(candidate => candidate.CreatedUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return snapshot is null ? null : Deserialize(snapshot, isFromSnapshot: true);
    }

    public async Task<SearchExecutionResult?> GetLatestForQueryAsync(
        string userId,
        string query,
        CancellationToken cancellationToken = default)
    {
        var normalizedQuery = NormalizeQuery(query);
        var now = DateTime.UtcNow;
        var snapshot = await dbContext.SearchResultSnapshots
            .AsNoTracking()
            .Where(candidate => candidate.UserId == userId
                && candidate.Query == normalizedQuery
                && candidate.ExpiresUtc > now)
            .OrderByDescending(candidate => candidate.CreatedUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return snapshot is null ? null : Deserialize(snapshot, isFromSnapshot: true);
    }

    public async Task SaveAsync(
        string userId,
        int? searchRequestId,
        string query,
        string idempotencyKey,
        IReadOnlyList<ProductResult> products,
        IReadOnlyList<SellerOffer> offers,
        IReadOnlyDictionary<string, SearchProductOfferSummary> productOfferSummaries,
        LlmSuggestionResult? aiSuggestions,
        TimeSpan? lifetime = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedQuery = NormalizeQuery(query);
        var now = DateTime.UtcNow;
        var configuredTtlHours = Math.Clamp(options.SnapshotTtlHours, 1, 168);
        var expiresUtc = now.Add(lifetime ?? TimeSpan.FromHours(configuredTtlHours));

        var snapshot = await dbContext.SearchResultSnapshots
            .SingleOrDefaultAsync(candidate => candidate.UserId == userId && candidate.IdempotencyKey == idempotencyKey, cancellationToken);

        if (snapshot is null)
        {
            snapshot = new SearchResultSnapshot
            {
                UserId = userId,
                IdempotencyKey = idempotencyKey,
                CreatedUtc = now
            };
            dbContext.SearchResultSnapshots.Add(snapshot);
        }

        snapshot.SearchRequestId = searchRequestId;
        snapshot.Query = normalizedQuery;
        snapshot.ProductsJson = JsonSerializer.Serialize(products, JsonOptions);
        snapshot.OffersJson = JsonSerializer.Serialize(offers, JsonOptions);
        snapshot.ProductOfferSummariesJson = JsonSerializer.Serialize(productOfferSummaries.Values, JsonOptions);
        snapshot.AiSuggestionsJson = aiSuggestions is null ? null : JsonSerializer.Serialize(aiSuggestions, JsonOptions);
        snapshot.ResultCount = products.Count;
        snapshot.ExpiresUtc = expiresUtc;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> DeleteExpiredSnapshotsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var expiredSnapshots = await dbContext.SearchResultSnapshots
            .Where(snapshot => snapshot.ExpiresUtc <= now)
            .ToListAsync(cancellationToken);

        if (expiredSnapshots.Count == 0)
        {
            return 0;
        }

        dbContext.SearchResultSnapshots.RemoveRange(expiredSnapshots);
        await dbContext.SaveChangesAsync(cancellationToken);
        return expiredSnapshots.Count;
    }

    private SearchExecutionResult? Deserialize(SearchResultSnapshot snapshot, bool isFromSnapshot)
    {
        try
        {
            var products = JsonSerializer.Deserialize<List<ProductResult>>(snapshot.ProductsJson, JsonOptions)
                ?? new List<ProductResult>();
            var offers = JsonSerializer.Deserialize<List<SellerOffer>>(snapshot.OffersJson, JsonOptions)
                ?? new List<SellerOffer>();
            var summaries = JsonSerializer.Deserialize<List<SearchProductOfferSummary>>(snapshot.ProductOfferSummariesJson, JsonOptions)
                ?? new List<SearchProductOfferSummary>();
            var aiSuggestions = string.IsNullOrWhiteSpace(snapshot.AiSuggestionsJson)
                ? null
                : JsonSerializer.Deserialize<LlmSuggestionResult>(snapshot.AiSuggestionsJson, JsonOptions);

            return new SearchExecutionResult(
                Allowed: true,
                Query: snapshot.Query,
                Products: products,
                Offers: offers,
                ProductOfferSummaries: summaries
                    .GroupBy(summary => summary.Product.Slug, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase),
                AiSuggestions: aiSuggestions,
                Message: null,
                SearchRequestId: snapshot.SearchRequestId,
                WasCharged: false,
                WasRefunded: false,
                IsFromSnapshot: isFromSnapshot);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to deserialize search result snapshot {SnapshotId}.", snapshot.Id);
            return null;
        }
    }

    private static string NormalizeQuery(string query)
    {
        return query.Trim();
    }
}
