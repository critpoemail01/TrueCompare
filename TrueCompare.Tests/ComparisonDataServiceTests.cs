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
    public void GetBestOffer_ReturnsCheapestReliableSmartphoneOfferForUserMarket()
    {
        var service = new ComparisonDataService(new AppText());

        var offer = service.GetBestOffer("iphone-15-pro");

        Assert.Equal("Amazon.es", offer.Seller);
        Assert.Equal(97900, offer.PriceCents);
        Assert.True(offer.Preferred);
        Assert.False(offer.IsLivePrice);
        Assert.Equal("Portugal", offer.LocationLabel);
        Assert.True(offer.ReliabilityScore >= 85);
    }

    [Fact]
    public void GetBestOffer_ParsesThousandSeparatedPrices()
    {
        var service = new ComparisonDataService(new AppText());

        var offer = service.GetBestOffer("macbook-air-m3");

        Assert.Equal("Amazon.es", offer.Seller);
        Assert.Equal(127300, offer.PriceCents);
        Assert.Contains("273", offer.Price);
    }

    [Fact]
    public void GetProducts_RanksMatchingProducts_WhenQueryIsDescriptive()
    {
        var service = new ComparisonDataService(new AppText());

        var products = service.GetProducts("quero um android samsung com boa bateria e garantia");

        Assert.Equal("Samsung Galaxy S24", products[0].Name);
        Assert.Contains(products, product => product.Name == "Samsung Galaxy A56 5G");
        Assert.DoesNotContain(products, product => product.Name == "MacBook Air M3");
    }

    [Fact]
    public void GetProducts_ReturnsOnlyIphone17Family_WhenQueryMentionsIphone17WithTypo()
    {
        var service = new ComparisonDataService(new AppText());

        var products = service.GetProducts("telemovel ipohone 17");

        Assert.NotEmpty(products);
        Assert.Equal("iPhone 17", products[0].Name);
        Assert.Contains(products[0].OfficialSpecifications, specification => specification.Value.Contains("A19", StringComparison.OrdinalIgnoreCase));
        Assert.All(products, product => Assert.Contains("iPhone 17", product.Name));
        Assert.DoesNotContain(products, product => product.Name == "iPhone 15 Pro");
        Assert.DoesNotContain(products, product => product.Name == "Samsung Galaxy S24");
        Assert.Contains("apple.com/iphone-17/specs", products[0].OfficialSource?.Url);
    }

    [Fact]
    public void GetProducts_ReturnsChargers_WhenQueryAsksForIphoneCharger()
    {
        var service = new ComparisonDataService(new AppText());

        var products = service.GetProducts("procurar carregador de iPhone");

        Assert.NotEmpty(products);
        Assert.Equal("Apple Carregador USB-C 20W", products[0].Name);
        Assert.All(products, product =>
        {
            var productText = string.Join(' ', product.Name, product.Brand, product.Badge, product.AiSummary, string.Join(' ', product.Specs));
            Assert.Contains("iPhone", productText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Carregador", productText, StringComparison.OrdinalIgnoreCase);
        });
        Assert.DoesNotContain(products, product => product.Name.StartsWith("iPhone ", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetProducts_ReturnsNoLocalProducts_WhenExactPhoneModelIsUnknown()
    {
        var service = new ComparisonDataService(new AppText());

        var products = service.GetProducts("telemovel iphone 99");

        Assert.Empty(products);
    }

    [Theory]
    [InlineData("teclado sem fios ate 80 euros", "review typing test")]
    [InlineData("preciso de fraldas bebe tamanho 4", "review opiniao bebe")]
    [InlineData("procuro impressora multifuncoes wifi", "review teste impressao")]
    [InlineData("disco externo 1tb usb 3.2", "review benchmark")]
    public void GetProducts_BuildsProductSpecificYoutubeReviewQueries(string query, string expectedQualifier)
    {
        var service = new ComparisonDataService(new AppText());

        var product = service.GetProducts(query).First();
        var reviewUrls = string.Join(' ', product.ReviewLinks.Select(link => Uri.UnescapeDataString(link.Url)));

        Assert.Contains(expectedQualifier, reviewUrls, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("rugged tablet field test", reviewUrls, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetProducts_KeepsRuggedYoutubeQualifierForFactoryTablet()
    {
        var service = new ComparisonDataService(new AppText());

        var product = service.GetProducts("tablet todoterreno para chao de fabrica 10 polegadas").First();
        var reviewUrls = string.Join(' ', product.ReviewLinks.Select(link => Uri.UnescapeDataString(link.Url)));

        Assert.Contains("rugged tablet field test", reviewUrls, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("eletrodomesticos eficientes")]
    [InlineData("Eletrodomésticos eficientes")]
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
    public void GetProducts_ReturnsOnlyFridges_WhenQueryMentionsFridge()
    {
        var service = new ComparisonDataService(new AppText());

        var products = service.GetProducts("comprar frigorifico classe A");

        Assert.Equal("Bosch Serie 6 Frigorífico", products[0].Name);
        Assert.DoesNotContain(products, product => product.Name == "Miele W1 Lavadora");
        Assert.DoesNotContain(products, product => product.Name == "Samsung Bespoke Lava-loiça");
    }

    [Fact]
    public void GetProducts_ReturnsOnlySpecificBoschFridge_WhenQueryMentionsKnownProduct()
    {
        var service = new ComparisonDataService(new AppText());

        var products = service.GetProducts("bosch serie 6 frigorifico");

        Assert.Single(products);
        Assert.Equal("Bosch Serie 6 Frigorífico", products[0].Name);
    }

    [Fact]
    public void GetProducts_ReturnsOfficialBoschSerie6Specs_WhenQueryMentionsSpecificFridge()
    {
        var service = new ComparisonDataService(new AppText());

        var products = service.GetProducts("bosch serie 6 frigorifico");

        Assert.Equal("Bosch Serie 6 Frigorífico", products[0].Name);
        Assert.Contains("KGN39AIAT", products[0].OfficialSource?.Url);
        Assert.Contains(products[0].OfficialSpecifications, specification => specification.Value == "Classe A");
        Assert.Contains(products[0].OfficialSpecifications, specification => specification.Value.Contains("260 L", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(products[0].OfficialSpecifications, specification => specification.Value == "29 dB");
    }

    [Fact]
    public void GetProducts_PrefersAppliances_WhenDescriptionMentionsSamsungDishwasher()
    {
        var service = new ComparisonDataService(new AppText());

        var products = service.GetProducts("procuro lava loica samsung eficiente silenciosa e barata");
        var offer = service.GetBestOffer("procuro lava loica samsung eficiente silenciosa e barata");

        Assert.Equal("Samsung Bespoke Lava-loiça", products[0].Name);
        Assert.DoesNotContain(products, product => product.Name == "Bosch Serie 6 Frigorífico");
        Assert.DoesNotContain(products, product => product.Name == "Miele W1 Lavadora");
        Assert.DoesNotContain(products, product => product.Name == "Samsung Galaxy S24");
        Assert.Equal("Amazon.es", offer.Seller);
        Assert.True(offer.Preferred);
        Assert.True(offer.ReliabilityScore >= 85);
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

        Assert.Contains(products, product => product.Name == "Logitech Signature M650");
        Assert.Contains(products.Single(product => product.Name == "Logitech Signature M650").Specs, spec => spec == "101 g");
        Assert.Contains(products, product => product.Name == "Microsoft Bluetooth Mouse");
        Assert.DoesNotContain(products, product => product.Name == "MacBook Air M3");
        Assert.All(products, product => Assert.Contains("50", string.Join(' ', product.Specs)));
    }

    [Fact]
    public void GetProducts_UsesValidatedOfficialLogitechProductPage_WhenQueryAsksForG305()
    {
        var service = new ComparisonDataService(new AppText());

        var products = service.GetProducts("rato gaming logitech g305 lightspeed");
        var g305 = products.Single(product => product.Slug == "logitech-g305-lightspeed");

        Assert.Equal(
            "https://www.logitech.com/en-eu/shop/p/g305-lightspeed-wireless-gaming-mouse",
            g305.OfficialSource?.Url);
        Assert.DoesNotContain("pt-pt/search", g305.OfficialSource?.Url);
    }

    [Fact]
    public void GetProducts_UsesCheapestKnownStorePrice_WhenRetailerProductPagesAreKnown()
    {
        var service = new ComparisonDataService(new AppText());

        var products = service.GetProducts("rato gaming logitech g305 lightspeed");
        var g305 = products.Single(product => product.Slug == "logitech-g305-lightspeed");

        Assert.Equal("42,98 €", g305.Price);
    }

    [Fact]
    public void GetSellerOffers_ShowsLivePricesAndDirectProductPages_ForKnownG305Stores()
    {
        var service = new ComparisonDataService(new AppText());

        var offers = service.GetSellerOffers("logitech-g305-lightspeed");
        var kuantoKusta = offers.Single(offer => offer.Seller == "KuantoKusta");
        var pcdiga = offers.Single(offer => offer.Seller == "PCDIGA");
        var radioPopular = offers.Single(offer => offer.Seller == "Radio Popular");

        Assert.True(kuantoKusta.Preferred);
        Assert.True(kuantoKusta.IsLivePrice);
        Assert.Equal(4298, kuantoKusta.PriceCents);
        Assert.Equal("42,98 €", kuantoKusta.Price);
        Assert.Equal("https://www.kuantokusta.pt/p/199266/logitech-g305-lightspeed-wireless-gaming-910-005283", kuantoKusta.Url);

        Assert.True(pcdiga.IsLivePrice);
        Assert.Equal(5490, pcdiga.PriceCents);
        Assert.Equal("https://www.pcdiga.com/rato-gaming-logitech-g305-lightspeed-wireless-preto-910-005282", pcdiga.Url);

        Assert.True(radioPopular.IsLivePrice);
        Assert.Equal(5499, radioPopular.PriceCents);
        Assert.Equal("https://www.radiopopular.pt/produto/rato-gaming-logitech-g305", radioPopular.Url);
    }

    [Fact]
    public void GetSellerOffers_UsesDirectRetailerProductUrl_WhenKnownStoreHasExactPage()
    {
        var service = new ComparisonDataService(new AppText());

        var offers = service.GetSellerOffers("logitech-m650-signature");
        var radioPopular = offers.Single(offer => offer.Seller == "Radio Popular");

        Assert.Equal("https://www.radiopopular.pt/produto/rato-logitech-m650-graph", radioPopular.Url);
        Assert.DoesNotContain("/pesquisa/", radioPopular.Url);
        Assert.True(radioPopular.IsLivePrice);
    }

    [Fact]
    public void GetSellerOffers_ReturnsConfirmedIphoneStorePriceAndProductPage()
    {
        var service = new ComparisonDataService(new AppText());

        var offers = service.GetSellerOffers("iphone-17");
        var radioPopular = offers.Single(offer => offer.Seller == "Radio Popular");

        Assert.True(radioPopular.IsLivePrice);
        Assert.Equal(142499, radioPopular.PriceCents);
        Assert.Equal("https://www.radiopopular.pt/produto/apple-iphone-17-pro-max-256gb-lj", radioPopular.Url);
        Assert.DoesNotContain("/pesquisa/", radioPopular.Url);
    }

    [Fact]
    public void GetProducts_ReturnsRuggedTabletCatalog_WithOfficialEvidence()
    {
        var service = new ComparisonDataService(new AppText());

        var products = service.GetProducts("software em chao de fabrica tablet touch 10 polegadas todoterreno");

        Assert.Equal("Getac UX10 G3", products[0].Name);
        Assert.Contains(products[0].OfficialSpecifications, specification => specification.Value.Contains("10.1", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(products[0].OfficialSource);
        Assert.Contains("getac.com", products[0].OfficialSource!.Url);
        Assert.Equal(2, products[0].ReviewLinks.Count);
        Assert.All(products[0].ReviewLinks, review => Assert.Contains("youtube.com/results", review.Url));
    }

    [Fact]
    public void GetProducts_ReturnsConsumerTabletCatalog_WhenQueryIsNormalTablet()
    {
        var service = new ComparisonDataService(new AppText());

        var products = service.GetProducts("Apple iPad 11 A16 128GB Wi-Fi");

        Assert.NotEmpty(products);
        Assert.Equal("Apple iPad 11 A16 128GB Wi-Fi", products[0].Name);
        Assert.DoesNotContain(products, product => product.Name == "Getac UX10 G3");
    }

    [Fact]
    public void GetProducts_DoesNotClassifyXiaomiTvAsSmartphone()
    {
        var service = new ComparisonDataService(new AppText());

        var products = service.GetProducts("TV Xiaomi 55 TV A PRO QLED Smart TV 4K");

        Assert.NotEmpty(products);
        Assert.Contains(products, product => product.Name == "Samsung QN90D 55\"");
        Assert.DoesNotContain(products, product => product.Name == "Xiaomi 14");
    }

    [Fact]
    public void GetProducts_DoesNotClassifyXiaomiScaleAsSmartphone()
    {
        var service = new ComparisonDataService(new AppText());

        var products = service.GetProducts("Xiaomi Mi Smart Scale S400 balanca inteligente");

        Assert.NotEmpty(products);
        Assert.Equal("Xiaomi Mi Smart Scale S400", products[0].Name);
        Assert.DoesNotContain(products, product => product.Name == "Xiaomi 14");
    }

    [Fact]
    public void GetProducts_ReturnsHealthBeautyCatalog_WhenQueryMentionsSupplements()
    {
        var service = new ComparisonDataService(new AppText());

        var products = service.GetProducts("Now Magnesium Glycinate 1000 mg 180 Comprimidos");

        Assert.NotEmpty(products);
        Assert.Equal("Tecnifar Artrozen 60 Comprimidos", products[0].Name);
        Assert.DoesNotContain(products, product => product.Name == "Getac UX10 G3");
    }

    [Fact]
    public void GetCriteriaWeights_ReturnsRuggedTabletCriteria_ForFactoryFloorTablet()
    {
        var service = new ComparisonDataService(new AppText());

        var weights = service.GetCriteriaWeights("tablet touch para chao de fabrica todoterreno");

        Assert.Contains(weights, weight => weight.Name == "Certificação rugged");
        Assert.Contains(weights, weight => weight.Name == "Ecrã / resolução");
        Assert.DoesNotContain(weights, weight => weight.Name == "Sensor / DPI");
    }

    [Fact]
    public void GetCriteriaWeights_ReturnsMouseCriteria_WhenQueryAsksForMouseUnder50()
    {
        var service = new ComparisonDataService(new AppText());

        var weights = service.GetCriteriaWeights("quero um rato ate 50 euros");

        Assert.Contains(weights, weight => weight.Name == "Sensor / DPI");
        Assert.Contains(weights, weight => weight.Name == "Ergonomia");
        Assert.DoesNotContain(weights, weight => weight.Name == "Performance (CPU)");
    }

    [Fact]
    public void GetCriteriaWeights_ReturnsChairCriteria_WhenQueryAsksForOfficeChair()
    {
        var service = new ComparisonDataService(new AppText());

        var weights = service.GetCriteriaWeights("quero uma cadeira escritorio ate 100 euros");

        Assert.Contains(weights, weight => weight.Name == "Ergonomia / apoio lombar");
        Assert.Contains(weights, weight => weight.Name == "Ajustes");
        Assert.DoesNotContain(weights, weight => weight.Name == "Performance (CPU)");
    }

    [Fact]
    public void GetCriteriaWeights_ReturnsApplianceCriteria_WhenQueryMentionsFridge()
    {
        var service = new ComparisonDataService(new AppText());

        var weights = service.GetCriteriaWeights("bosch serie 6 frigorifico");

        Assert.Contains(weights, weight => weight.Name == "Eficiência energética");
        Assert.Contains(weights, weight => weight.Name == "Nível de ruído");
        Assert.DoesNotContain(weights, weight => weight.Name == "Performance (CPU)");
    }

    [Fact]
    public void GetProducts_DoesNotClassifyCheapLaptopAsMouse()
    {
        var service = new ComparisonDataService(new AppText());

        var products = service.GetProducts("portatil barato");

        Assert.Contains(products, product => product.Name == "MacBook Air M3");
        Assert.DoesNotContain(products, product => product.Name == "Logitech Signature M650");
    }

    [Fact]
    public void GetProducts_EnforcesExplicitBudget_WhenKnownProductsAreAboveLimit()
    {
        var service = new ComparisonDataService(new AppText());

        var products = service.GetProducts("disco externo ate 20 euros");
        var offers = service.GetSellerOffers("disco externo ate 20 euros");

        Assert.Empty(products);
        Assert.Empty(offers);
    }

    [Theory]
    [InlineData("Smartwatch XIAOMI Redmi Watch 5 Active Bluetooth Autonomia ate 18 dias Preto", "Samsung Galaxy Watch7")]
    [InlineData("Drone PRIXTON Delta 480p Autonomia Ate 10 min Cinzento", "Prixton Delta Drone")]
    [InlineData("Suporte de TV ONE FOR ALL WM4611 Fixo 32 a 90 Ate 100 kg", "TooQ Suporte TV VESA")]
    public void GetProducts_DoesNotTreatNonPriceAteValuesAsBudget(string query, string expectedProduct)
    {
        var service = new ComparisonDataService(new AppText());

        var products = service.GetProducts(query);

        Assert.NotEmpty(products);
        Assert.Contains(products, product => product.Name == expectedProduct);
    }

    [Fact]
    public void GetProducts_PrioritizesRealFourteenInchLaptop_WhenQueryRequiresFourteenInches()
    {
        var service = new ComparisonDataService(new AppText());

        var products = service.GetProducts("portatil profissional 14 ate 1500 autonomia >10h leve");

        Assert.NotEmpty(products);
        Assert.Contains("14", products[0].Specs[0], StringComparison.OrdinalIgnoreCase);
        Assert.NotEqual("MacBook Air M3", products[0].Name);
        var macBookAirM3 = products.FirstOrDefault(product => product.Name == "MacBook Air M3");
        if (macBookAirM3 is not null)
        {
            Assert.Contains(macBookAirM3.Specs, spec => spec == "13.6\"");
        }
    }

    [Fact]
    public void GetProducts_DoesNotClassifyGenericWirelessPeripheralAsMouse()
    {
        var service = new ComparisonDataService(new AppText());

        var products = service.GetProducts("teclado sem fios ate 80 euros");

        Assert.NotEmpty(products);
        Assert.Contains(products, product => product.Name == "Logitech K380");
        Assert.DoesNotContain(products, product => product.Name == "Logitech Signature M650");
    }

    [Fact]
    public void GetCriteriaWeights_ReturnsKeyboardCriteria_WhenQueryAsksForKeyboard()
    {
        var service = new ComparisonDataService(new AppText());

        var weights = service.GetCriteriaWeights("teclado sem fios ate 80 euros");

        Assert.Contains(weights, weight => weight.Name == "Tipo de switch");
        Assert.Contains(weights, weight => weight.Name == "Layout / compatibilidade");
        Assert.DoesNotContain(weights, weight => weight.Name == "Sensor / DPI");
    }

    [Fact]
    public void GetProducts_ReturnsOfficeChairCatalog_WhenQueryAsksForChair()
    {
        var service = new ComparisonDataService(new AppText());

        var products = service.GetProducts("quero uma cadeira ergonomica ate 100 euros");

        Assert.NotEmpty(products);
        Assert.Contains(products, product => product.Name == "SONGMICS OBG22B");
        Assert.DoesNotContain(products, product => product.Name == "MacBook Air M3");
    }

    [Theory]
    [InlineData("disco externo 1tb usb 3.2", "Western Digital My Passport 1TB", "MacBook Air M3")]
    [InlineData("monitor 27 polegadas 144hz", "LG UltraGear 27GP850-B", "iPhone 17")]
    [InlineData("auscultadores bluetooth noise cancelling", "Sony WH-1000XM5", "Samsung Galaxy S24")]
    [InlineData("televisor 55 polegadas oled", "LG OLED C4 55\"", "Dell XPS 14")]
    [InlineData("maquina de cafe automatica", "De'Longhi Magnifica Start", "Bosch Serie 6 Frigorífico")]
    [InlineData("berbequim sem fios 18v", "Bosch Professional GSB 18V-55", "MacBook Air M3")]
    [InlineData("pneu 205 55 r16", "Michelin Primacy 4+ 205/55 R16", "Samsung Galaxy S24")]
    [InlineData("impressora multifuncoes wifi", "Epson EcoTank L3250", "MacBook Air M3")]
    [InlineData("racao cao adulto 15kg", "Royal Canin Medium Adult 15kg", "Bosch Serie 6 Frigorífico")]
    [InlineData("fraldas bebe tamanho 4", "Pampers Premium Protection T4", "iPhone 17")]
    [InlineData("mesa escritorio ate 100 euros", "IKEA LAGKAPTEN / ADILS", "Sony WH-1000XM5")]
    [InlineData("aspirador robot com mapeamento", "Roborock Q8 Max", "Bosch Serie 6 Frigorífico")]
    [InlineData("consola ps5", "PlayStation 5 Slim", "Nintendo Switch OLED")]
    [InlineData("smartwatch android", "Samsung Galaxy Watch7", "MacBook Air M3")]
    [InlineData("camera vigilancia interior wifi", "TP-Link Tapo C200", "iPhone 17")]
    [InlineData("trotinete eletrica cidade", "Xiaomi Electric Scooter 4", "Bosch Serie 6 Frigorífico")]
    public void GetProducts_ReturnsKuantoKustaCategoryMatches(string query, string expectedProduct, string wrongProduct)
    {
        var service = new ComparisonDataService(new AppText());

        var products = service.GetProducts(query);

        Assert.NotEmpty(products);
        Assert.Contains(products, product => product.Name == expectedProduct);
        Assert.DoesNotContain(products, product => product.Name == wrongProduct);
    }

    [Fact]
    public void GetBestOffer_ReturnsCheapestReliableApplianceOffer()
    {
        var service = new ComparisonDataService(new AppText());

        var offer = service.GetBestOffer("bosch-serie-6-frigorifico");

        Assert.Equal("Amazon.es", offer.Seller);
        Assert.Equal(174300, offer.PriceCents);
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

    [Fact]
    public void GetSellerOffers_ReturnsStoresForEnglishUserMarket()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");

        try
        {
            var service = new ComparisonDataService(new AppText());

            var offers = service.GetSellerOffers("macbook-air-m3");

            Assert.Contains(offers, offer => offer.Seller == "Best Buy");
            Assert.All(offers, offer => Assert.Equal("United States", offer.LocationLabel));
            Assert.True(offers[0].Preferred);
            Assert.True(offers[0].ReliabilityScore >= 85);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }
}
