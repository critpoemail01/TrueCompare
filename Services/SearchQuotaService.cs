using System.Data;
using Microsoft.EntityFrameworkCore;
using TrueCompare.Data;

namespace TrueCompare.Services;

public sealed class SearchQuotaService(ApplicationDbContext dbContext)
{
    public async Task<SearchQuotaStatus> GetStatusAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .Where(candidate => candidate.Id == userId)
            .Select(candidate => new
            {
                candidate.FreeSearchesUsed,
                candidate.Credits
            })
            .SingleAsync(cancellationToken);

        return new SearchQuotaStatus(
            ApplicationUser.FreeSearchLimit,
            user.FreeSearchesUsed,
            Math.Max(0, ApplicationUser.FreeSearchLimit - user.FreeSearchesUsed),
            user.Credits);
    }

    public async Task<SearchConsumptionResult> TryConsumeAsync(
        string userId,
        string query,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        var user = await dbContext.Users.SingleAsync(candidate => candidate.Id == userId, cancellationToken);

        var usedPaidCredit = false;
        if (user.FreeSearchesUsed < ApplicationUser.FreeSearchLimit)
        {
            user.FreeSearchesUsed++;
        }
        else if (user.Credits > 0)
        {
            user.Credits--;
            usedPaidCredit = true;
        }
        else
        {
            return new SearchConsumptionResult(false, await GetStatusAsync(userId, cancellationToken));
        }

        dbContext.SearchRequests.Add(new SearchRequest
        {
            UserId = userId,
            Query = query.Trim(),
            UsedPaidCredit = usedPaidCredit
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new SearchConsumptionResult(true, new SearchQuotaStatus(
            ApplicationUser.FreeSearchLimit,
            user.FreeSearchesUsed,
            Math.Max(0, ApplicationUser.FreeSearchLimit - user.FreeSearchesUsed),
            user.Credits));
    }
}

public sealed record SearchQuotaStatus(
    int FreeSearchLimit,
    int FreeSearchesUsed,
    int FreeSearchesRemaining,
    int Credits);

public sealed record SearchConsumptionResult(
    bool Allowed,
    SearchQuotaStatus Status);
