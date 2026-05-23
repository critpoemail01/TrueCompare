using TrueCompare.Data;
using TrueCompare.Services;
using TrueCompare.Tests.Support;

namespace TrueCompare.Tests;

public sealed class SearchHistoryServiceTests
{
    [Fact]
    public async Task GetRecentForUserAsync_ReturnsOnlyCurrentUserHistoryProjectedToDto()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var user = new ApplicationUser { Id = "user-1", UserName = "user1@example.test", Email = "user1@example.test" };
        var otherUser = new ApplicationUser { Id = "user-2", UserName = "user2@example.test", Email = "user2@example.test" };
        dbContext.Users.AddRange(user, otherUser);
        dbContext.SearchRequests.AddRange(
            new SearchRequest
            {
                UserId = user.Id,
                Query = "iPhone 16e",
                CreatedUtc = DateTime.UtcNow.AddMinutes(-10),
                UsedPaidCredit = false
            },
            new SearchRequest
            {
                UserId = user.Id,
                Query = "Logitech G305",
                CreatedUtc = DateTime.UtcNow,
                UsedPaidCredit = true
            },
            new SearchRequest
            {
                UserId = otherUser.Id,
                Query = "Samsung S24",
                CreatedUtc = DateTime.UtcNow.AddMinutes(1),
                UsedPaidCredit = false
            });
        await dbContext.SaveChangesAsync();
        var service = new SearchHistoryService(dbContext);

        var history = await service.GetRecentForUserAsync(user.Id);

        Assert.Equal(["Logitech G305", "iPhone 16e"], history.Select(item => item.Query));
        Assert.True(history[0].UsedPaidCredit);
        Assert.All(history, item => Assert.NotEqual("Samsung S24", item.Query));
    }
}
