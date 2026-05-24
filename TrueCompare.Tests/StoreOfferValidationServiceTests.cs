using System.Globalization;
using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TrueCompare.Models;
using TrueCompare.Options;
using TrueCompare.Services;
using TrueCompare.Tests.Support;

namespace TrueCompare.Tests;

public sealed class StoreOfferValidationServiceTests
{
    static StoreOfferValidationServiceTests()
    {
        var culture = CultureInfo.GetCultureInfo("pt-PT");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }

    [Fact]
    public async Task ValidateConfirmedOffersAsync_UsesCurrentPriceFromDirectProductPage()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var handler = new FakeHttpMessageHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
                <html>
                <head><title>Rato Logitech G305 Lightspeed Wireless Gaming Preto</title></head>
                <body>
                <script type="application/ld+json">
                {"@type":"Product","name":"Logitech G305 Lightspeed","offers":{"price":"45.90","priceCurrency":"EUR"}}
                </script>
                </body>
                </html>
                """)
        }));
        var service = CreateService(handler, cache);

        var offers = await service.ValidateConfirmedOffersAsync(
            Product("logitech-g305-lightspeed", "Logitech G305 Lightspeed"),
            [LiveOffer("Globaldata", 5490, "https://www.globaldata.pt/rato-logitech-g-series-g305-lightspeed-wireless-gaming-preto/910-005283.html")]);

        var offer = Assert.Single(offers);
        Assert.Equal(4590, offer.PriceCents);
        Assert.Contains("45,90", offer.Price);
        Assert.True(offer.Preferred);
    }

    [Fact]
    public async Task ValidateConfirmedOffersAsync_PrefersDiscountPriceOverOriginalPrice()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var handler = new FakeHttpMessageHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                "<html><head><title>Rato Gaming Wireless Logitech G305 Preto</title></head>"
                + "<body><span>Pre\u00e7o de saldo</span><strong>57,99\u20ac</strong><small>PVP 59,99\u20ac</small></body></html>")
        }));
        var service = CreateService(handler, cache);

        var offers = await service.ValidateConfirmedOffersAsync(
            Product("logitech-g305-lightspeed", "Logitech G305 Lightspeed"),
            [LiveOffer("Darty", 5999, "https://darty.pt/products/rato-gaming-logitech-lightspeed-g305")]);

        var offer = Assert.Single(offers);
        Assert.Equal(5799, offer.PriceCents);
        Assert.Contains("57,99", offer.Price);
    }

    [Fact]
    public async Task ValidateConfirmedOffersAsync_AcceptsMicrowavePagesWithHyphenatedCategoryText()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var handler = new FakeHttpMessageHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                "<html><head><title>Micro-ondas Teka MW FS20 G WH Grill 20 L 700 W Branco</title></head>"
                + "<body><span>Preço de saldo</span><strong>64,99€</strong><p>Capacidade 20 litros</p></body></html>")
        }));
        var service = CreateService(handler, cache);

        var offers = await service.ValidateConfirmedOffersAsync(
            Product("teka-mw-fs20-g-wh-microondas", "Teka MW FS20 G WH Microondas Grill 20L"),
            [LiveOffer("Darty", 6499, "https://darty.pt/products/teka-microond-mw-fs20-g-wh-grill-20")]);

        var offer = Assert.Single(offers);
        Assert.Equal(6499, offer.PriceCents);
        Assert.Contains("64,99", offer.Price);
    }

    [Fact]
    public async Task ValidateConfirmedOffersAsync_RejectsProductPageWithWrongProduct()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var handler = new FakeHttpMessageHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
                <html>
                <head><title>Apple iPhone 16e 128GB Branco</title></head>
                <body><script>{"offers":{"price":"599.99"}}</script></body>
                </html>
                """)
        }));
        var service = CreateService(handler, cache);

        var offers = await service.ValidateConfirmedOffersAsync(
            Product("logitech-g305-lightspeed", "Logitech G305 Lightspeed"),
            [LiveOffer("Darty", 5999, "https://darty.pt/products/rato-gaming-logitech-lightspeed-g305")]);

        Assert.Empty(offers);
    }

    [Fact]
    public async Task ValidateConfirmedOffersAsync_RejectsSearchUrlsBeforeCallingStore()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var handler = new FakeHttpMessageHandler(_ => throw new InvalidOperationException("Search URLs must not be fetched."));
        var service = CreateService(handler, cache);

        var offers = await service.ValidateConfirmedOffersAsync(
            Product("logitech-g305-lightspeed", "Logitech G305 Lightspeed"),
            [LiveOffer("Worten", 5499, "https://www.worten.pt/search?query=logitech%20g305")]);

        Assert.Empty(offers);
    }

    private static StoreOfferValidationService CreateService(FakeHttpMessageHandler handler, IMemoryCache cache)
    {
        var limiter = new StoreValidationLimiter(Microsoft.Extensions.Options.Options.Create(new StoreOfferValidationOptions
        {
            DomainBackoffSeconds = 0,
            MaxGlobalConcurrentRequests = 8
        }));

        return new StoreOfferValidationService(
            new HttpClient(handler),
            cache,
            limiter,
            NullLogger<StoreOfferValidationService>.Instance);
    }

    private static ProductResult Product(string slug, string name)
    {
        return new ProductResult(
            slug,
            1,
            90,
            name,
            name.Split(' ')[0],
            "0,00 EUR",
            "#7BE8E0",
            "Teste",
            [],
            [],
            [],
            [],
            "Teste");
    }

    private static SellerOffer LiveOffer(string seller, long priceCents, string url)
    {
        return new SellerOffer(
            seller,
            $"{priceCents / 100m:N2} EUR",
            priceCents,
            "1-2 dias",
            "3 anos PT",
            "Verificado",
            url,
            false,
            90,
            "Portugal",
            "Teste",
            true);
    }
}
