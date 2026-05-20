using System.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TrueCompare.Data;

namespace TrueCompare.Services;

public sealed class SearchQuotaService
{
    private readonly ApplicationDbContext dbContext;
    private readonly IHttpContextAccessor? httpContextAccessor;

    public SearchQuotaService(ApplicationDbContext dbContext)
        : this(dbContext, null)
    {
    }

    public SearchQuotaService(ApplicationDbContext dbContext, IHttpContextAccessor? httpContextAccessor)
    {
        this.dbContext = dbContext;
        this.httpContextAccessor = httpContextAccessor;
    }

    public async Task<SearchQuotaStatus> GetStatusAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (HasLocalUnlimitedCredits())
        {
            return CreateLocalUnlimitedStatus();
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .Where(candidate => candidate.Id == userId)
            .Select(candidate => new
            {
                candidate.FreeSearchesUsed,
                candidate.Credits,
                candidate.HasUnlimitedSubscription,
                candidate.SubscriptionActiveUntilUtc
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return new SearchQuotaStatus(
                ApplicationUser.FreeSearchLimit,
                0,
                ApplicationUser.FreeSearchLimit,
                0);
        }

        if (HasActiveUnlimitedSubscription(user.HasUnlimitedSubscription, user.SubscriptionActiveUntilUtc))
        {
            return CreateUnlimitedStatus(SearchQuotaUnlimitedSource.Subscription);
        }

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
        if (HasLocalUnlimitedCredits())
        {
            var localUserExists = await dbContext.Users
                .AsNoTracking()
                .AnyAsync(candidate => candidate.Id == userId, cancellationToken);
            if (!localUserExists)
            {
                return new SearchConsumptionResult(false, CreateLocalUnlimitedStatus());
            }

            return new SearchConsumptionResult(true, CreateLocalUnlimitedStatus());
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        var user = await dbContext.Users.SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);
        if (user is null)
        {
            return new SearchConsumptionResult(false, new SearchQuotaStatus(
                ApplicationUser.FreeSearchLimit,
                0,
                ApplicationUser.FreeSearchLimit,
                0));
        }

        if (HasActiveUnlimitedSubscription(user.HasUnlimitedSubscription, user.SubscriptionActiveUntilUtc))
        {
            dbContext.SearchRequests.Add(new SearchRequest
            {
                UserId = userId,
                Query = query.Trim(),
                UsedPaidCredit = false
            });

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new SearchConsumptionResult(true, CreateUnlimitedStatus(SearchQuotaUnlimitedSource.Subscription));
        }

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

    private bool HasLocalUnlimitedCredits()
    {
        var host = httpContextAccessor?.HttpContext?.Request.Host.Host;
        return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "::1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "[::1]", StringComparison.OrdinalIgnoreCase);
    }

    private static SearchQuotaStatus CreateLocalUnlimitedStatus()
    {
        return CreateUnlimitedStatus(SearchQuotaUnlimitedSource.Localhost);
    }

    private static SearchQuotaStatus CreateUnlimitedStatus(string source)
    {
        return new SearchQuotaStatus(
            ApplicationUser.FreeSearchLimit,
            0,
            ApplicationUser.FreeSearchLimit,
            int.MaxValue,
            true,
            source);
    }

    private static bool HasActiveUnlimitedSubscription(bool hasUnlimitedSubscription, DateTime? activeUntilUtc)
    {
        return hasUnlimitedSubscription
            && (!activeUntilUtc.HasValue || activeUntilUtc.Value > DateTime.UtcNow);
    }
}

public static class SearchQuotaUnlimitedSource
{
    public const string Localhost = "localhost";
    public const string Subscription = "subscription";
}

public sealed record SearchQuotaStatus(
    int FreeSearchLimit,
    int FreeSearchesUsed,
    int FreeSearchesRemaining,
    int Credits,
    bool HasUnlimitedCredits = false,
    string? UnlimitedSource = null);

public sealed record SearchConsumptionResult(
    bool Allowed,
    SearchQuotaStatus Status);
