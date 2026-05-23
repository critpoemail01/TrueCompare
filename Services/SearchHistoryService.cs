using Microsoft.EntityFrameworkCore;
using TrueCompare.Data;

namespace TrueCompare.Services;

public sealed class SearchHistoryService(ApplicationDbContext dbContext)
{
    public async Task<IReadOnlyList<SearchHistoryItem>> GetRecentForUserAsync(
        string userId,
        int take = 30,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Array.Empty<SearchHistoryItem>();
        }

        return await dbContext.SearchRequests
            .AsNoTracking()
            .Where(search => search.UserId == userId)
            .OrderByDescending(search => search.CreatedUtc)
            .Take(take)
            .Select(search => new SearchHistoryItem(
                search.Query,
                search.UsedPaidCredit,
                search.CreatedUtc))
            .ToListAsync(cancellationToken);
    }
}

public sealed record SearchHistoryItem(
    string Query,
    bool UsedPaidCredit,
    DateTime CreatedUtc);
