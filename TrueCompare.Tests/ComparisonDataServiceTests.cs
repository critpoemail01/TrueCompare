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
