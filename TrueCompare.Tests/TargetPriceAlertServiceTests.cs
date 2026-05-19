using TrueCompare.Data;
using TrueCompare.Services;
using TrueCompare.Tests.Support;

namespace TrueCompare.Tests;

public sealed class TargetPriceAlertServiceTests
{
    [Theory]
    [InlineData("699,50 €", 69950)]
    [InlineData("1199.99", 119999)]
    public void TryParsePrice_AcceptsPortugueseAndInvariantFormats(string rawValue, long expectedCents)
    {
        var parsed = TargetPriceAlertService.TryParsePrice(rawValue, out var price);

        Assert.True(parsed);
        Assert.Equal(expectedCents, TargetPriceAlertService.ToCents(price));
    }

    [Fact]
    public async Task CreateAsync_StoresAlertWithCurrentBestOffer()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var user = new ApplicationUser
        {
            Id = "alert-user",
            UserName = "alert@example.com",
            Email = "alert@example.com"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var service = new TargetPriceAlertService(dbContext, new ComparisonDataService());

        var alert = await service.CreateAsync(user.Id, "iphone-15-pro", "iPhone 15 Pro", 700m);

        Assert.Equal("iphone-15-pro", alert.ProductSlug);
        Assert.Equal(70000, alert.TargetPriceCents);
        Assert.Equal(74900, alert.LastSeenPriceCents);
        Assert.Equal("Amazon.es", alert.LastSeenSeller);
        Assert.True(alert.IsActive);
        Assert.False(alert.EmailSent);
    }
}
