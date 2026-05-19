using TrueCompare.Services;

namespace TrueCompare.Tests;

public sealed class ComparisonDataServiceTests
{
    [Fact]
    public void GetProducts_ReturnsSmartphoneCatalog_WhenQueryMentionsSmartphones()
    {
        var service = new ComparisonDataService();

        var products = service.GetProducts("comprar smartphones");

        Assert.Contains(products, product => product.Name == "iPhone 15 Pro");
        Assert.Contains(products, product => product.Name == "Samsung Galaxy S24");
        Assert.DoesNotContain(products, product => product.Name == "MacBook Air M3");
    }

    [Fact]
    public void GetBestOffer_ReturnsLowestKnownSmartphoneOffer()
    {
        var service = new ComparisonDataService();

        var offer = service.GetBestOffer("iphone-15-pro");

        Assert.Equal("Amazon.es", offer.Seller);
        Assert.Equal(74900, offer.PriceCents);
        Assert.True(offer.Preferred);
    }
}
