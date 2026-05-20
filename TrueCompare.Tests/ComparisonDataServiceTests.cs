using System.Globalization;
using TrueCompare.Services;

namespace TrueCompare.Tests;

public sealed class ComparisonDataServiceTests
{
    [Fact]
    public void GetProducts_ReturnsSmartphoneCatalog_WhenQueryMentionsSmartphones()
    {
        var service = new ComparisonDataService(new AppText());

        var products = service.GetProducts("comprar smartphones");

        Assert.Contains(products, product => product.Name == "iPhone 15 Pro");
        Assert.Contains(products, product => product.Name == "Samsung Galaxy S24");
        Assert.DoesNotContain(products, product => product.Name == "MacBook Air M3");
    }

    [Fact]
    public void GetBestOffer_ReturnsLowestKnownSmartphoneOffer()
    {
        var service = new ComparisonDataService(new AppText());

        var offer = service.GetBestOffer("iphone-15-pro");

        Assert.Equal("Amazon.es", offer.Seller);
        Assert.Equal(74900, offer.PriceCents);
        Assert.True(offer.Preferred);
    }

    [Fact]
    public void GetProducts_RanksMatchingProducts_WhenQueryIsDescriptive()
    {
        var service = new ComparisonDataService(new AppText());

        var products = service.GetProducts("quero um android samsung com boa bateria e garantia");

        Assert.Equal("Samsung Galaxy S24", products[0].Name);
        Assert.Contains(products, product => product.Name == "Google Pixel 8");
        Assert.DoesNotContain(products, product => product.Name == "MacBook Air M3");
    }

    [Theory]
    [InlineData("eletrodomesticos eficiencia A+++")]
    [InlineData("Eletrodomésticos eficiência A+++")]
    [InlineData("comprar frigorifico classe A+++")]
    public void GetProducts_ReturnsApplianceCatalog_WhenQueryMentionsAppliances(string query)
    {
        var service = new ComparisonDataService(new AppText());

        var products = service.GetProducts(query);

        Assert.Contains(products, product => product.Name == "Bosch Serie 6 Frigorífico");
        Assert.Contains(products, product => product.Name == "Miele W1 Lavadora");
        Assert.DoesNotContain(products, product => product.Name == "iPhone 15 Pro");
        Assert.DoesNotContain(products, product => product.Name == "MacBook Air M3");
    }

    [Fact]
    public void GetProducts_PrefersAppliances_WhenDescriptionMentionsSamsungDishwasher()
    {
        var service = new ComparisonDataService(new AppText());

        var products = service.GetProducts("procuro lava loica samsung eficiente silenciosa e barata");
        var offer = service.GetBestOffer("procuro lava loica samsung eficiente silenciosa e barata");

        Assert.Equal("Samsung Bespoke Lava-loiça", products[0].Name);
        Assert.Contains(products, product => product.Name == "Bosch Serie 6 Frigorífico");
        Assert.DoesNotContain(products, product => product.Name == "Samsung Galaxy S24");
        Assert.Equal("Worten", offer.Seller);
    }

    [Fact]
    public void GetProducts_UsesProductTerms_WhenQueryDoesNotNameACategory()
    {
        var service = new ComparisonDataService(new AppText());

        var products = service.GetProducts("boa camara suporte longo vendedor autorizado");

        Assert.Contains(products.Take(2), product => product.Name == "iPhone 15 Pro");
        Assert.DoesNotContain(products, product => product.Name == "MacBook Air M3");
        Assert.DoesNotContain(products, product => product.Name == "Bosch Serie 6 Frigorífico");
    }

    [Fact]
    public void GetProducts_ReturnsMouseCatalog_WhenQueryAsksForMouseUnder50()
    {
        var service = new ComparisonDataService(new AppText());

        var products = service.GetProducts("quero um rato ate 50 euros");

        Assert.Contains(products, product => product.Name == "Logitech M650 Signature");
        Assert.Contains(products, product => product.Name == "Microsoft Bluetooth Mouse");
        Assert.DoesNotContain(products, product => product.Name == "MacBook Air M3");
        Assert.All(products, product => Assert.Contains("50", string.Join(' ', product.Specs)));
    }

    [Fact]
    public void GetProducts_ReturnsNoLocalProducts_WhenCatalogIsUnknown()
    {
        var service = new ComparisonDataService(new AppText());

        var products = service.GetProducts("quero uma cadeira ergonomica ate 100 euros");

        Assert.Empty(products);
    }

    [Fact]
    public void GetBestOffer_ReturnsLowestKnownApplianceOffer()
    {
        var service = new ComparisonDataService(new AppText());

        var offer = service.GetBestOffer("bosch-serie-6-frigorifico");

        Assert.Equal("Worten", offer.Seller);
        Assert.Equal(87900, offer.PriceCents);
        Assert.True(offer.Preferred);
    }

    [Fact]
    public void GetProducts_ReturnsEnglishCopy_WhenCultureIsEnglish()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");

        try
        {
            var service = new ComparisonDataService(new AppText());

            var products = service.GetProducts("buy phones");

            Assert.Contains("Premium smartphones", service.Categories);
            Assert.Contains(products, product => product.Name == "iPhone 15 Pro" && product.Badge == "Recommended");
            Assert.Contains(products.Single(product => product.Name == "iPhone 15 Pro").Specs, spec => spec == "23h video");
            Assert.Equal("Authorized", service.GetBestOffer("iphone-15-pro").Status);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }
}
