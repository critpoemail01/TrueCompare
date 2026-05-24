using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TrueCompare.Data;
using TrueCompare.Services;
using TrueCompare.Tests.Support;

namespace TrueCompare.Tests;

public sealed class SearchQuotaServiceTests
{
    [Fact]
    public async Task TryConsumeAsync_AllowsExactlyThreeFreeSearches()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var user = new ApplicationUser
        {
            Id = "user-free",
            UserName = "free@example.com",
            Email = "free@example.com"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var service = new SearchQuotaService(dbContext);

        for (var index = 1; index <= ApplicationUser.FreeSearchLimit; index++)
        {
            var result = await service.TryConsumeAsync(user.Id, $" pesquisa {index} ");

            Assert.True(result.Allowed);
            Assert.Equal(index, result.Status.FreeSearchesUsed);
            Assert.Equal(ApplicationUser.FreeSearchLimit - index, result.Status.FreeSearchesRemaining);
            Assert.Equal(0, result.Status.Credits);
        }

        var blocked = await service.TryConsumeAsync(user.Id, "quarta pesquisa");

        Assert.False(blocked.Allowed);
        Assert.Equal(ApplicationUser.FreeSearchLimit, blocked.Status.FreeSearchesUsed);
        Assert.Equal(0, blocked.Status.FreeSearchesRemaining);
        Assert.Equal(3, await dbContext.SearchRequests.CountAsync());
        Assert.All(await dbContext.SearchRequests.ToListAsync(), request => Assert.False(request.UsedPaidCredit));
    }

    [Fact]
    public async Task TryConsumeAsync_ConsumesPaidCredit_AfterFreeLimit()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var user = new ApplicationUser
        {
            Id = "user-paid",
            UserName = "paid@example.com",
            Email = "paid@example.com",
            FreeSearchesUsed = ApplicationUser.FreeSearchLimit,
            Credits = 2
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var service = new SearchQuotaService(dbContext);

        var result = await service.TryConsumeAsync(user.Id, " comprar smartphones ");
        var request = await dbContext.SearchRequests.SingleAsync();

        Assert.True(result.Allowed);
        Assert.Equal(1, result.Status.Credits);
        Assert.True(request.UsedPaidCredit);
        Assert.Equal("comprar smartphones", request.Query);
    }

    [Fact]
    public async Task TryConsumeAsync_UsesNormalQuota_WhenHostIsLocalhostButUnlimitedOptionDisabled()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var user = new ApplicationUser
        {
            Id = "user-localhost",
            UserName = "localhost@example.com",
            Email = "localhost@example.com",
            FreeSearchesUsed = ApplicationUser.FreeSearchLimit,
            Credits = 0
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                {
                    Host = new HostString("localhost:5241")
                }
            }
        };
        var service = new SearchQuotaService(dbContext, httpContextAccessor);

        var result = await service.TryConsumeAsync(user.Id, "comprar eletrodomesticos");
        var status = await service.GetStatusAsync(user.Id);
        var persistedUser = await dbContext.Users.SingleAsync(candidate => candidate.Id == user.Id);

        Assert.False(result.Allowed);
        Assert.False(result.Status.HasUnlimitedCredits);
        Assert.False(status.HasUnlimitedCredits);
        Assert.Equal(0, result.Status.Credits);
        Assert.Equal(ApplicationUser.FreeSearchLimit, persistedUser.FreeSearchesUsed);
        Assert.Equal(0, persistedUser.Credits);
        Assert.Empty(await dbContext.SearchRequests.ToListAsync());
    }

    [Fact]
    public async Task TryConsumeAsync_DoesNotAllowLocalhostSearch_WhenUserIsMissingAndOptionDisabled()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                {
                    Host = new HostString("localhost:5190")
                }
            }
        };
        var service = new SearchQuotaService(dbContext, httpContextAccessor);

        var result = await service.TryConsumeAsync("stale-cookie-user", "smartphones premium");

        Assert.False(result.Allowed);
        Assert.False(result.Status.HasUnlimitedCredits);
        Assert.Null(result.Status.UnlimitedSource);
        Assert.Empty(await dbContext.SearchRequests.ToListAsync());
    }

    [Fact]
    public async Task TryConsumeAsync_DoesNotConsumeCredits_WhenUserHasActiveUnlimitedSubscription()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var user = new ApplicationUser
        {
            Id = "user-subscription",
            UserName = "subscription@example.com",
            Email = "subscription@example.com",
            FreeSearchesUsed = ApplicationUser.FreeSearchLimit,
            Credits = 0,
            HasUnlimitedSubscription = true,
            SubscriptionPlanId = "monthly",
            SubscriptionActiveUntilUtc = DateTime.UtcNow.AddDays(20)
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var service = new SearchQuotaService(dbContext);

        var result = await service.TryConsumeAsync(user.Id, "comprar smartphones");
        var status = await service.GetStatusAsync(user.Id);
        var persistedUser = await dbContext.Users.SingleAsync(candidate => candidate.Id == user.Id);

        Assert.True(result.Allowed);
        Assert.True(result.Status.HasUnlimitedCredits);
        Assert.Equal(SearchQuotaUnlimitedSource.Subscription, result.Status.UnlimitedSource);
        Assert.Equal("monthly", result.Status.SubscriptionPlanId);
        Assert.True(status.HasUnlimitedCredits);
        Assert.Equal(SearchQuotaUnlimitedSource.Subscription, status.UnlimitedSource);
        Assert.Equal("monthly", status.SubscriptionPlanId);
        Assert.Equal(ApplicationUser.FreeSearchLimit, persistedUser.FreeSearchesUsed);
        Assert.Equal(0, persistedUser.Credits);
        Assert.Single(await dbContext.SearchRequests.ToListAsync());
    }

    [Fact]
    public async Task TryConsumeAsync_UsesNormalQuota_WhenUnlimitedSubscriptionIsExpired()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var user = new ApplicationUser
        {
            Id = "user-expired-subscription",
            UserName = "expired@example.com",
            Email = "expired@example.com",
            FreeSearchesUsed = ApplicationUser.FreeSearchLimit,
            Credits = 0,
            HasUnlimitedSubscription = true,
            SubscriptionPlanId = "monthly",
            SubscriptionActiveUntilUtc = DateTime.UtcNow.AddDays(-1)
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var service = new SearchQuotaService(dbContext);

        var result = await service.TryConsumeAsync(user.Id, "comprar smartphones");

        Assert.False(result.Allowed);
        Assert.False(result.Status.HasUnlimitedCredits);
        Assert.Empty(await dbContext.SearchRequests.ToListAsync());
    }

    [Fact]
    public async Task GetStatusAndTryConsume_ReturnSafeStatus_WhenUserDoesNotExist()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var service = new SearchQuotaService(dbContext);

        var status = await service.GetStatusAsync("missing-user");
        var result = await service.TryConsumeAsync("missing-user", "comprar smartphones");

        Assert.Equal(ApplicationUser.FreeSearchLimit, status.FreeSearchesRemaining);
        Assert.False(result.Allowed);
        Assert.Equal(0, result.Status.Credits);
        Assert.Empty(await dbContext.SearchRequests.ToListAsync());
    }
    [Fact]
    public async Task TryReserveAsync_RefundRestoresFreeSearch_WhenNoUsefulResultsAreProduced()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var user = new ApplicationUser
        {
            Id = "user-refund-free",
            UserName = "refund-free@example.com",
            Email = "refund-free@example.com"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var service = new SearchQuotaService(dbContext);

        var reservation = await service.TryReserveAsync(user.Id, "produto impossivel", "refund-free-key");

        Assert.True(reservation.Allowed);
        Assert.True(reservation.SearchRequestId.HasValue);
        Assert.Equal(1, reservation.Status.FreeSearchesUsed);

        await service.RefundAsync(reservation.SearchRequestId.Value, "No useful results.");

        var persistedUser = await dbContext.Users.SingleAsync(candidate => candidate.Id == user.Id);
        var request = await dbContext.SearchRequests.SingleAsync();
        Assert.Equal(0, persistedUser.FreeSearchesUsed);
        Assert.Equal(SearchRequestStatus.Refunded, request.Status);
        Assert.Equal("No useful results.", request.FailureReason);
    }

    [Fact]
    public async Task TryReserveAsync_ReusesIdempotencyKey_WithoutDoubleCharging()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var user = new ApplicationUser
        {
            Id = "user-idempotent",
            UserName = "idempotent@example.com",
            Email = "idempotent@example.com"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var service = new SearchQuotaService(dbContext);

        var first = await service.TryReserveAsync(user.Id, "comprar smartphone", "same-search-key");
        var second = await service.TryReserveAsync(user.Id, "comprar smartphone", "same-search-key");

        Assert.True(first.Allowed);
        Assert.True(second.Allowed);
        Assert.Equal(first.SearchRequestId, second.SearchRequestId);

        var persistedUser = await dbContext.Users.SingleAsync(candidate => candidate.Id == user.Id);
        Assert.Equal(1, persistedUser.FreeSearchesUsed);
        Assert.Single(await dbContext.SearchRequests.ToListAsync());
    }


    [Fact]
    public async Task TryReserveAsync_DoesNotReuseIdempotencyKey_ForDifferentQuery()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var user = new ApplicationUser
        {
            Id = "user-idempotent-query",
            UserName = "idempotent-query@example.com",
            Email = "idempotent-query@example.com"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var service = new SearchQuotaService(dbContext);

        var first = await service.TryReserveAsync(user.Id, "comprar smartphone", "same-search-key");
        var second = await service.TryReserveAsync(user.Id, "comprar portatil", "same-search-key");

        Assert.True(first.Allowed);
        Assert.True(second.Allowed);
        Assert.NotEqual(first.SearchRequestId, second.SearchRequestId);

        var persistedUser = await dbContext.Users.SingleAsync(candidate => candidate.Id == user.Id);
        Assert.Equal(2, persistedUser.FreeSearchesUsed);
        Assert.Equal(2, await dbContext.SearchRequests.CountAsync());
    }

    [Fact]
    public async Task CommitAsync_MarksReservationAsCommittedAndRecordsResultCount()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var user = new ApplicationUser
        {
            Id = "user-commit",
            UserName = "commit@example.com",
            Email = "commit@example.com"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var service = new SearchQuotaService(dbContext);

        var reservation = await service.TryReserveAsync(user.Id, "comprar rato", "commit-key");
        Assert.True(reservation.SearchRequestId.HasValue);

        await service.CommitAsync(reservation.SearchRequestId.Value, resultCount: 3);

        var request = await dbContext.SearchRequests.SingleAsync();
        Assert.Equal(SearchRequestStatus.Committed, request.Status);
        Assert.Equal(3, request.ResultCount);
        Assert.NotNull(request.CommittedUtc);
    }

}
