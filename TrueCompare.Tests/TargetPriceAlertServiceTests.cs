using TrueCompare.Data;
using TrueCompare.Models;
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

        var service = new TargetPriceAlertService(dbContext, new ComparisonDataService(new AppText()));

        var alert = await service.CreateAsync(user.Id, "iphone-15-pro", "iPhone 15 Pro", 700m);

        Assert.Equal("iphone-15-pro", alert.ProductSlug);
        Assert.Equal(70000, alert.TargetPriceCents);
        Assert.Equal(97900, alert.LastSeenPriceCents);
        Assert.Equal("Amazon.es", alert.LastSeenSeller);
        Assert.True(alert.IsActive);
        Assert.False(alert.EmailSent);
    }

    [Fact]
    public void AlertEmailTemplate_UsesTrueCompareLayoutAndEscapesExternalValues()
    {
        var alert = new TargetPriceAlert
        {
            ProductName = "Disco <script>",
            TargetPriceCents = 5500
        };
        var offer = new SellerOffer(
            "Worten & Loja",
            "52,90 €",
            5290,
            "1-2 dias",
            "3 anos PT",
            "Verificado",
            "https://example.test/product?a=1&b=2",
            true,
            93,
            "Portugal",
            "Preço validado",
            true);
        var html = PriceAlertEmailTemplate.Build(alert, offer, isExample: true);

        Assert.Contains("#171a20", html);
        Assert.Contains("O pre&ccedil;o baixou", html);
        Assert.Contains("Confirmar pre&ccedil;o na loja", html);
        Assert.Contains("TrueCompare by Advance", html);
        Assert.Contains("55,00 €", html);
        Assert.Contains("52,90 €", html);
        Assert.Contains("Disco &lt;script&gt;", html);
        Assert.Contains("Worten &amp; Loja", html);
        Assert.Contains("https://example.test/product?a=1&amp;b=2", html);
        Assert.DoesNotContain("Disco <script>", html);
    }

    [Fact]
    public void AlertEmailSubject_IncludesProductName()
    {
        var subject = PriceAlertEmailTemplate.BuildSubject("Western Digital My Passport 1TB\r\nbcc:test@example.com");

        Assert.Equal("TrueCompare - Western Digital My Passport 1TB  bcc:test@example.com baixou de preço", subject);
        Assert.DoesNotContain("\r", subject);
        Assert.DoesNotContain("\n", subject);
    }
}
