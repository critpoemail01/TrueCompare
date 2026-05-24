using System.Globalization;
using TrueCompare.Data;
using TrueCompare.Models;
using TrueCompare.Services;
using TrueCompare.Tests.Support;

namespace TrueCompare.Tests;

public sealed class TargetPriceAlertServiceTests
{
    static TargetPriceAlertServiceTests()
    {
        var culture = CultureInfo.GetCultureInfo("pt-PT");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    public TargetPriceAlertServiceTests()
    {
        var culture = CultureInfo.GetCultureInfo("pt-PT");
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

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
    public async Task CreateAsync_StoresAlertWithCurrentBestLiveValidatedOffer()
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

        var service = new TargetPriceAlertService(
            dbContext,
            new ComparisonDataService(new AppText()),
            new LiveValidatingStoreOfferValidator());

        var expectedOffer = await service.GetBestLiveValidatedOfferAsync("logitech-g305-lightspeed");

        var alert = await service.CreateAsync(user.Id, "logitech-g305-lightspeed", "Logitech G305 Lightspeed", 35m);

        Assert.Equal("logitech-g305-lightspeed", alert.ProductSlug);
        Assert.Equal(3500, alert.TargetPriceCents);
        Assert.NotNull(expectedOffer);
        Assert.Equal(expectedOffer!.PriceCents, alert.LastSeenPriceCents);
        Assert.Equal(expectedOffer.Seller, alert.LastSeenSeller);
        Assert.True(alert.IsActive);
        Assert.False(alert.EmailSent);
        Assert.True(alert.IsLiveValidated);
        Assert.Equal(nameof(OfferPriceValidationState.LiveValidated), alert.LastValidationState);
        Assert.NotNull(alert.LastValidatedUtc);

        var storedAlert = Assert.Single(dbContext.TargetPriceAlerts);
        Assert.Equal(alert.ProductSlug, storedAlert.ProductSlug);
        Assert.Equal(alert.TargetPriceCents, storedAlert.TargetPriceCents);
    }

    [Fact]
    public async Task CreateAsync_CreatesPendingAlert_WhenLiveValidationIsUnavailable()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var user = new ApplicationUser
        {
            Id = "alert-pending-user",
            UserName = "alert-pending@example.com",
            Email = "alert-pending@example.com"
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var service = new TargetPriceAlertService(
            dbContext,
            new ComparisonDataService(new AppText()),
            new RejectingStoreOfferValidator());

        var alert = await service.CreateAsync(user.Id, "logitech-g305-lightspeed", "Logitech G305 Lightspeed", 35m);

        Assert.False(alert.IsLiveValidated);
        Assert.Equal(nameof(OfferPriceValidationState.PendingValidation), alert.LastValidationState);
        Assert.Null(alert.LastValidatedUtc);
    }

    [Fact]
    public async Task CreateAsync_RejectsProductWithoutConfirmedStoreOffer()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var service = new TargetPriceAlertService(dbContext, new ComparisonDataService(new AppText()));

        var exception = await Assert.ThrowsAsync<PriceAlertCreationException>(() =>
            service.CreateAsync("alert-user", "iphone-15-pro", "iPhone 15 Pro", 700m));
        Assert.Equal(PriceAlertCreationError.NoConfirmedStoreOffer, exception.Code);
    }

    [Fact]
    public void AlertEmailTemplate_UsesTrueCompareLayoutAndEscapesExternalValues()
    {
        var alert = new PriceAlertEmailModel("Disco <script>", 5500);
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

    private sealed class LiveValidatingStoreOfferValidator : IStoreOfferValidationService
    {
        public Task<IReadOnlyList<SellerOffer>> ValidateConfirmedOffersAsync(
            ProductResult? product,
            IReadOnlyList<SellerOffer> offers,
            CancellationToken cancellationToken = default)
        {
            var validatedUtc = DateTime.UtcNow;
            return Task.FromResult((IReadOnlyList<SellerOffer>)offers
                .Select(offer => offer with
                {
                    IsLivePrice = true,
                    ValidationState = OfferPriceValidationState.LiveValidated,
                    ValidatedUtc = validatedUtc
                })
                .ToList());
        }
    }

    private sealed class RejectingStoreOfferValidator : IStoreOfferValidationService
    {
        public Task<IReadOnlyList<SellerOffer>> ValidateConfirmedOffersAsync(
            ProductResult? product,
            IReadOnlyList<SellerOffer> offers,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult((IReadOnlyList<SellerOffer>)Array.Empty<SellerOffer>());
        }
    }
}
