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
}
