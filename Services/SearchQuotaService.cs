using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TrueCompare.Data;
using TrueCompare.Options;

namespace TrueCompare.Services;

public sealed class SearchQuotaService
{
    private const int MaxQueryLength = 800;
    private const int MaxIdempotencyKeyLength = 160;
    private const int MaxFailureReasonLength = 500;

    private readonly ApplicationDbContext dbContext;
    private readonly SearchQuotaOptions options;
    private readonly IHostEnvironment? environment;

    public SearchQuotaService(ApplicationDbContext dbContext)
        : this(dbContext, null, null, null)
    {
    }

    public SearchQuotaService(ApplicationDbContext dbContext, IHttpContextAccessor? httpContextAccessor)
        : this(dbContext, httpContextAccessor, null, null)
    {
    }

    public SearchQuotaService(
        ApplicationDbContext dbContext,
        IHttpContextAccessor? httpContextAccessor,
        IOptions<SearchQuotaOptions>? optionsAccessor,
        IHostEnvironment? environment)
    {
        this.dbContext = dbContext;
        options = optionsAccessor?.Value ?? new SearchQuotaOptions();
        this.environment = environment;
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
                candidate.SubscriptionActiveUntilUtc,
                candidate.SubscriptionPlanId
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return CreateMissingUserStatus();
        }

        if (HasActiveUnlimitedSubscription(user.HasUnlimitedSubscription, user.SubscriptionActiveUntilUtc))
        {
            return CreateUnlimitedStatus(SearchQuotaUnlimitedSource.Subscription, user.SubscriptionPlanId);
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
        var reservation = await TryReserveAsync(userId, query, idempotencyKey: null, cancellationToken);
        if (!reservation.Allowed)
        {
            return new SearchConsumptionResult(false, reservation.Status);
        }

        if (reservation.SearchRequestId.HasValue)
        {
            await CommitAsync(reservation.SearchRequestId.Value, resultCount: 0, cancellationToken);
        }

        return new SearchConsumptionResult(true, reservation.Status);
    }

    public async Task<SearchReservationResult> TryReserveAsync(
        string userId,
        string query,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedQuery = NormalizeQuery(query);
        var normalizedKey = NormalizeIdempotencyKeyForStorage(idempotencyKey, normalizedQuery);

        if (HasLocalUnlimitedCredits())
        {
            var localUser = await dbContext.Users.SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);
            if (localUser is null)
            {
                return new SearchReservationResult(true, CreateLocalUnlimitedStatus(), null);
            }

            var existing = await FindRequestByIdempotencyKeyAsync(localUser.Id, normalizedKey, normalizedQuery, cancellationToken);
            if (existing is not null)
            {
                return new SearchReservationResult(true, CreateLocalUnlimitedStatus(), existing.Id);
            }

            var request = new SearchRequest
            {
                UserId = localUser.Id,
                Query = normalizedQuery,
                UsedPaidCredit = false,
                UsedFreeSearch = false,
                Status = SearchRequestStatus.Pending,
                IdempotencyKey = normalizedKey
            };
            dbContext.SearchRequests.Add(request);
            await dbContext.SaveChangesAsync(cancellationToken);

            return new SearchReservationResult(true, CreateLocalUnlimitedStatus(), request.Id);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var user = await dbContext.Users.SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);
        if (user is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return new SearchReservationResult(false, CreateMissingUserStatus(), null);
        }

        var idempotentRequest = await FindRequestByIdempotencyKeyAsync(user.Id, normalizedKey, normalizedQuery, cancellationToken);
        if (idempotentRequest is not null)
        {
            await transaction.CommitAsync(cancellationToken);
            return new SearchReservationResult(true, CreateStatusForUser(user), idempotentRequest.Id);
        }

        if (HasActiveUnlimitedSubscription(user.HasUnlimitedSubscription, user.SubscriptionActiveUntilUtc))
        {
            var request = new SearchRequest
            {
                UserId = user.Id,
                Query = normalizedQuery,
                UsedPaidCredit = false,
                UsedFreeSearch = false,
                Status = SearchRequestStatus.Pending,
                IdempotencyKey = normalizedKey
            };
            dbContext.SearchRequests.Add(request);

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new SearchReservationResult(
                true,
                CreateUnlimitedStatus(SearchQuotaUnlimitedSource.Subscription, user.SubscriptionPlanId),
                request.Id);
        }

        var usedFreeSearch = false;
        var usedPaidCredit = false;
        if (user.FreeSearchesUsed < ApplicationUser.FreeSearchLimit)
        {
            user.FreeSearchesUsed++;
            usedFreeSearch = true;
        }
        else if (user.Credits > 0)
        {
            user.Credits--;
            usedPaidCredit = true;
        }
        else
        {
            await transaction.CommitAsync(cancellationToken);
            return new SearchReservationResult(false, CreateStatusForUser(user), null);
        }

        var searchRequest = new SearchRequest
        {
            UserId = user.Id,
            Query = normalizedQuery,
            UsedPaidCredit = usedPaidCredit,
            UsedFreeSearch = usedFreeSearch,
            Status = SearchRequestStatus.Pending,
            IdempotencyKey = normalizedKey
        };
        dbContext.SearchRequests.Add(searchRequest);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new SearchReservationResult(true, CreateStatusForUser(user), searchRequest.Id);
    }

    public async Task CommitAsync(
        int searchRequestId,
        int resultCount = 0,
        CancellationToken cancellationToken = default)
    {
        var request = await dbContext.SearchRequests.SingleOrDefaultAsync(
            candidate => candidate.Id == searchRequestId,
            cancellationToken);
        if (request is null || request.Status != SearchRequestStatus.Pending)
        {
            return;
        }

        request.Status = SearchRequestStatus.Committed;
        request.CommittedUtc = DateTime.UtcNow;
        request.ResultCount = Math.Max(0, resultCount);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RefundAsync(
        int searchRequestId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var request = await dbContext.SearchRequests.SingleOrDefaultAsync(
            candidate => candidate.Id == searchRequestId,
            cancellationToken);
        if (request is null || request.Status != SearchRequestStatus.Pending)
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        var user = await dbContext.Users.SingleOrDefaultAsync(candidate => candidate.Id == request.UserId, cancellationToken);
        if (user is not null)
        {
            if (request.UsedPaidCredit)
            {
                user.Credits++;
            }
            else if (request.UsedFreeSearch && user.FreeSearchesUsed > 0)
            {
                user.FreeSearchesUsed--;
            }
        }

        request.Status = SearchRequestStatus.Refunded;
        request.RefundedUtc = DateTime.UtcNow;
        request.FailureReason = Truncate(reason, MaxFailureReasonLength);
        request.ResultCount = 0;

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public Task CommitReservationAsync(int? reservationId, int resultCount = 0, CancellationToken cancellationToken = default)
    {
        return reservationId.HasValue
            ? CommitAsync(reservationId.Value, resultCount, cancellationToken)
            : Task.CompletedTask;
    }

    public Task RefundReservationAsync(int? reservationId, string reason, CancellationToken cancellationToken = default)
    {
        return reservationId.HasValue
            ? RefundAsync(reservationId.Value, reason, cancellationToken)
            : Task.CompletedTask;
    }

    public async Task<int> RefundExpiredPendingReservationsAsync(CancellationToken cancellationToken = default)
    {
        var expiryMinutes = Math.Clamp(options.PendingReservationExpiryMinutes, 5, 24 * 60);
        var cutoffUtc = DateTime.UtcNow.AddMinutes(-expiryMinutes);
        var pendingIds = await dbContext.SearchRequests
            .AsNoTracking()
            .Where(request => request.Status == SearchRequestStatus.Pending && request.CreatedUtc <= cutoffUtc)
            .OrderBy(request => request.CreatedUtc)
            .Select(request => request.Id)
            .ToListAsync(cancellationToken);

        foreach (var requestId in pendingIds)
        {
            await RefundAsync(requestId, "Expired pending reservation was refunded automatically.", cancellationToken);
        }

        return pendingIds.Count;
    }

    private Task<SearchRequest?> FindRequestByIdempotencyKeyAsync(
        string userId,
        string? idempotencyKey,
        string normalizedQuery,
        CancellationToken cancellationToken)
    {
        return string.IsNullOrWhiteSpace(idempotencyKey)
            ? Task.FromResult<SearchRequest?>(null)
            : dbContext.SearchRequests
                .Where(request => request.UserId == userId
                    && request.IdempotencyKey == idempotencyKey
                    && request.Query == normalizedQuery)
                .OrderByDescending(request => request.CreatedUtc)
                .FirstOrDefaultAsync(cancellationToken);
    }

    private bool HasLocalUnlimitedCredits()
    {
        return options.LocalUnlimitedEnabled
            && environment is not null
            && (environment.IsDevelopment() || environment.IsEnvironment("Testing"));
    }

    private static SearchQuotaStatus CreateMissingUserStatus()
    {
        return new SearchQuotaStatus(
            ApplicationUser.FreeSearchLimit,
            0,
            ApplicationUser.FreeSearchLimit,
            0);
    }

    private static SearchQuotaStatus CreateStatusForUser(ApplicationUser user)
    {
        if (HasActiveUnlimitedSubscription(user.HasUnlimitedSubscription, user.SubscriptionActiveUntilUtc))
        {
            return CreateUnlimitedStatus(SearchQuotaUnlimitedSource.Subscription, user.SubscriptionPlanId);
        }

        return new SearchQuotaStatus(
            ApplicationUser.FreeSearchLimit,
            user.FreeSearchesUsed,
            Math.Max(0, ApplicationUser.FreeSearchLimit - user.FreeSearchesUsed),
            user.Credits);
    }

    private static SearchQuotaStatus CreateLocalUnlimitedStatus()
    {
        return CreateUnlimitedStatus(SearchQuotaUnlimitedSource.Localhost);
    }

    private static SearchQuotaStatus CreateUnlimitedStatus(string source, string? subscriptionPlanId = null)
    {
        return new SearchQuotaStatus(
            ApplicationUser.FreeSearchLimit,
            0,
            ApplicationUser.FreeSearchLimit,
            int.MaxValue,
            true,
            source,
            subscriptionPlanId);
    }

    private static bool HasActiveUnlimitedSubscription(bool hasUnlimitedSubscription, DateTime? activeUntilUtc)
    {
        return hasUnlimitedSubscription
            && (!activeUntilUtc.HasValue || activeUntilUtc.Value > DateTime.UtcNow);
    }

    private static string NormalizeQuery(string query)
    {
        return Truncate(query.Trim(), MaxQueryLength);
    }

    public static string? NormalizeIdempotencyKeyForStorage(string? idempotencyKey, string normalizedQuery)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return null;
        }

        var queryHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(NormalizeQuery(normalizedQuery))))[..16];
        var prefix = Truncate(idempotencyKey.Trim(), MaxIdempotencyKeyLength - queryHash.Length - 1);
        return $"{prefix}:{queryHash}";
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
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
    string? UnlimitedSource = null,
    string? SubscriptionPlanId = null);

public sealed record SearchConsumptionResult(
    bool Allowed,
    SearchQuotaStatus Status);

public sealed record SearchReservationResult(
    bool Allowed,
    SearchQuotaStatus Status,
    int? SearchRequestId)
{
    public int? ReservationId => SearchRequestId;
}
