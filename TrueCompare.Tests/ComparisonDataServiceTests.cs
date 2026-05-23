using System.Globalization;
using TrueCompare.Services;

namespace TrueCompare.Tests;

public sealed class ComparisonDataServiceTests
{
    static ComparisonDataServiceTests()
    {
        var culture = CultureInfo.GetCultureInfo("pt-PT");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    public ComparisonDataServiceTests()
    {
        var culture = CultureInfo.GetCultureInfo("pt-PT");
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    [Fact]
    public void CategorySuggestions_ReturnRelevantProductsWithConfirmedStoreOffers()
    {
        var service = new ComparisonDataService(new AppText());
        var expectations = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Smartphones premium"] = ["iphone", "galaxy", "smartphone"],
            ["Telemóveis e smartwatches"] = ["iphone", "smartphone", "galaxy", "watch"],
            ["Smartphones e acessórios"] = ["iphone", "smartphone", "carregador", "galaxy"],
            ["Carregadores e cabos"] = ["carregador", "usb-c", "20w"],
            ["Informática e portáteis"] = ["portatil", "asus", "macbook"],
            ["Computadores e tablets"] = ["asus", "lenovo", "tablet", "portatil"],
            ["Ratos e periféricos até 50€"] = ["rato", "logitech", "mouse"],
            ["Armazenamento externo"] = ["disco", "externo", "passport"],
            ["Monitores"] = ["monitor", "ultragear", "27"],
            ["Impressoras"] = ["impressora", "hp", "deskjet"],
            ["Imagem, TV e Som"] = ["tv", "som", "jbl", "auscultadores"],
            ["Gaming e consolas"] = ["playstation", "ps5", "consola"],
            ["Gaming"] = ["playstation", "ps5", "consola"],
            ["Jogos e brinquedos"] = ["lego", "brinquedo", "jogo"],
            ["Fotografia, drones e vídeo"] = ["canon", "drone", "fotografia", "video"],
            ["Eletrodomésticos"] = ["frigorifico", "bosch", "microondas", "balanca", "xiaomi", "secar", "beko"],
            ["Grandes eletrodomésticos"] = ["frigorifico", "bosch", "combinado", "lavar", "becken", "secar", "beko"],
            ["Pequenos eletrodomésticos"] = ["microondas", "teka", "cafe", "nespresso", "balanca", "xiaomi"],
            ["Máquinas de lavar"] = ["maquina de lavar", "lavar", "becken", "roupa"],
            ["Frigoríficos"] = ["frigorifico", "bosch", "combinado"],
            ["Ventoinhas"] = ["ventoinha", "rowenta", "ventilacao"],
            ["Máquinas de café"] = ["nespresso", "cafe", "café"],
            ["Microondas compactos"] = ["microondas", "teka", "20"],
            ["Preparação de alimentos"] = ["kenwood", "robot de cozinha", "chef", "preparacao"],
            ["Aspiradores"] = ["aspirador", "becken", "limpeza"],
            ["Beleza e saúde"] = ["perfume", "lattafa", "beleza", "saude"],
            ["Saúde, beleza e perfumaria"] = ["perfume", "lattafa", "beleza"],
            ["Animais de estimação"] = ["royal", "racao", "cao"],
            ["Bebé"] = ["fraldas", "bebe", "rascals"],
            ["Bebé, puericultura e brinquedos"] = ["fraldas", "bebe", "lego"],
            ["Bricolage"] = ["bosch", "berbequim", "bricolage", "karcher", "pressao"],
            ["Bricolagem e construção"] = ["bosch", "berbequim", "bricolagem", "karcher", "pressao"],
            ["Casa e decoração"] = ["cadeira", "casa", "mitsai"],
            ["Sofás"] = ["sofa", "homcom", "cama"],
            ["Bricolage e jardim"] = ["bosch", "berbequim", "weber", "jardim"],
            ["Jardim"] = ["weber", "barbecue", "jardim"],
            ["Desporto, outdoor e viagem"] = ["bicicleta", "otte", "desporto", "trotinete", "fitness", "passadeira"],
            ["Desporto"] = ["bicicleta", "otte", "desporto", "trotinete", "fitness", "passadeira"],
            ["Fitness"] = ["fitfiu", "fitness", "passadeira"],
            ["Mobilidade"] = ["bicicleta", "trotinete", "mobilidade", "xiaomi"],
            ["Moda e acessórios"] = ["adidas", "sapatilhas", "moda"],
            ["Auto e moto"] = ["castrol", "oleo", "auto"],
            ["Escritório e papelaria"] = ["hp", "tinteiros", "papelaria"],
            ["Cultura, lazer e livros"] = ["livro", "catan", "atomic", "vinho", "papa"],
            ["Livros, música e filmes"] = ["livro", "atomic", "habits"],
            ["Gastronomia e vinhos"] = ["vinho", "papa", "figos", "gin"],
            ["Recondicionados e outlet"] = ["playstation", "ps5", "consola"],
            ["Consolas PlayStation"] = ["playstation", "ps5"]
        };

        Assert.Equal(expectations.Keys.OrderBy(category => category), service.Categories.OrderBy(category => category));

        foreach (var category in service.Categories)
        {
            var query = $"{category}: melhor preço, vendedores verificados, baixo risco.";
            var products = service.GetProducts(query);
            Assert.NotEmpty(products);

            var productsWithConfirmedOffers = products
                .Where(product => service.BuildSellerOffersForProduct(product, query).Any(ComparisonDataService.IsConfirmedStoreOffer))
                .ToList();

            Assert.True(
                productsWithConfirmedOffers.Count > 0,
                $"{category} should have at least one product with a confirmed direct store offer. Products: {string.Join(", ", products.Select(product => product.Slug))}");
            Assert.All(productsWithConfirmedOffers, product =>
            {
                var productText = string.Join(
                    ' ',
                    product.Name,
                    product.Brand,
                    product.Badge,
                    product.AiSummary,
                    string.Join(' ', product.Specs));
                Assert.Contains(expectations[category], term => productText.Contains(term, StringComparison.OrdinalIgnoreCase));
            });
        }
    }

    [Theory]
    [InlineData("KuantoKusta", "Electrodomésticos", "bosch")]
    [InlineData("KuantoKusta", "Saúde e Beleza", "perfume")]
    [InlineData("KuantoKusta", "Informática", "portatil")]
    [InlineData("KuantoKusta", "Smartphones e Acessórios", "iphone")]
    [InlineData("KuantoKusta", "Imagem e Som", "tv")]
    [InlineData("KuantoKusta", "Gaming", "playstation")]
    [InlineData("KuantoKusta", "Animais de Estimação", "advance")]
    [InlineData("KuantoKusta", "Puericultura e Brinquedos", "fraldas")]
    [InlineData("KuantoKusta", "Bricolagem e Construção", "bosch")]
    [InlineData("KuantoKusta", "Casa e Decoração", "cadeira")]
    [InlineData("KuantoKusta", "Desporto", "bicicleta")]
    [InlineData("KuantoKusta", "Moda e Acessórios", "adidas")]
    [InlineData("KuantoKusta", "Auto e Moto", "castrol")]
    [InlineData("KuantoKusta", "Escritório e Papelaria", "tinteiros")]
    [InlineData("KuantoKusta", "Cultura e Lazer", "catan")]
    [InlineData("KuantoKusta", "Gastronomia e Vinhos", "papa")]
    [InlineData("Worten", "Recondicionados e Outlet", "playstation")]
    [InlineData("Worten", "Eletrodomésticos", "bosch")]
    [InlineData("Worten", "Grandes eletrodomésticos", "bosch")]
    [InlineData("Worten", "Pequenos eletrodomésticos", "microondas")]
    [InlineData("Worten", "Máquinas de lavar", "becken")]
    [InlineData("Worten", "Frigoríficos", "bosch")]
    [InlineData("Worten", "Ventoinhas", "rowenta")]
    [InlineData("Worten", "Preparação de alimentos", "kenwood")]
    [InlineData("Worten", "Aspiradores", "aspirador")]
    [InlineData("Worten", "Telemóveis e Smartwatches", "iphone")]
    [InlineData("Worten", "Informática", "asus")]
    [InlineData("Worten", "Computadores e Tablets", "asus")]
    [InlineData("Worten", "TV e Som", "tv")]
    [InlineData("Worten", "Gaming", "playstation")]
    [InlineData("Worten", "Jogos e Brinquedos", "lego")]
    [InlineData("Worten", "Fotografia, Drones e Vídeo", "canon")]
    [InlineData("Worten", "Beleza e Saúde", "perfume")]
    [InlineData("Worten", "Cuidado Pessoal e Saúde", "perfume")]
    [InlineData("Worten", "Perfumaria e Cosmética", "perfume")]
    [InlineData("Worten", "Bebé", "fraldas")]
    [InlineData("Worten", "Casa e Decoração", "cadeira")]
    [InlineData("Worten", "Sofás", "sofa")]
    [InlineData("Worten", "Jardim", "weber")]
    [InlineData("Worten", "Bricolage", "bosch")]
    [InlineData("Worten", "Bricolage e Jardim", "weber")]
    [InlineData("Worten", "Desporto, Outdoor e Viagem", "bicicleta")]
    [InlineData("Worten", "Fitness", "fitfiu")]
    [InlineData("Worten", "Mobilidade", "trotinete")]
    [InlineData("Worten", "Mobilidade, Auto e Moto", "castrol")]
    [InlineData("Worten", "Livros, Música e Filmes", "atomic")]
    [InlineData("Worten", "Escritório e Papelaria", "tinteiros")]
    public void OfficialMarketplaceCategories_ReturnValidatedStoreOptions(
        string source,
        string category,
        string expectedTerm)
    {
        var service = new ComparisonDataService(new AppText());
        var query = $"{category}: melhor preço, vendedores verificados, baixo risco.";

        var products = service.GetProducts(query);
        var productsWithConfirmedOffers = products
            .Where(product => service.BuildSellerOffersForProduct(product, query).Any(ComparisonDataService.IsConfirmedStoreOffer))
            .ToList();

        Assert.True(
            productsWithConfirmedOffers.Count > 0,
            $"{source}/{category} should return at least one product with a direct confirmed store offer.");
        Assert.Contains(productsWithConfirmedOffers, product =>
            string.Join(' ', product.Name, product.Brand, product.Badge, product.AiSummary, string.Join(' ', product.Specs))
                .Contains(expectedTerm, StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("Electrodomésticos", "Microondas Teka MW FS20 WH 20L", "Teka MW FS20 WH Microondas 20L", "teka-mw-fs20-wh-microondas", 4939, "PlayStation")]
    [InlineData("Electrodomésticos", "Delta Q Mini Qool Cinzento", "Delta Q Mini Qool Cinzento", "delta-q-mini-qool-cinzento", 3599, "Delta Q Qalidus")]
    [InlineData("Máquinas de Lavar Roupa", "Hisense WF1G7021BW 7Kg 1200RPM Classe B", "Hisense WF1G7021BW Maquina de Lavar Roupa", "hisense-wf1g7021bw-maquina-lavar", 21490, "Lavar Loica")]
    [InlineData("Máquinas de Secar Roupa", "Beko BM3T48249W 8Kg Classe C", "Beko BM3T48249W Maquina de Secar Roupa 8Kg", "beko-bm3t48249w-maquina-secar-roupa", 36690, "Adidas")]
    [InlineData("Informática", "Apple iPad 2025 11 A16 128GB Wi-Fi Prateado", "Apple iPad 11 A16 128GB Wi-Fi", "apple-ipad-11-a16-128gb", 33990, "MacBook")]
    [InlineData("Escritório e Papelaria", "Tinteiro HP 308 Preto Tricolor Pack 2x", "Tinteiro HP 308 Preto/Tricolor Pack 2x", "hp-308-preto-tricolor-pack", 3221, "HP DeskJet")]
    [InlineData("Smartphones e Acessórios", "Samsung Galaxy A16 4G 6.7 Dual SIM 4GB 128GB Black", "Samsung Galaxy A16", "samsung-galaxy-a16", 10889, "iPhone")]
    [InlineData("Animais de Estimação", "Advance Cat Adult Frango e Arroz 12kg", "Advance Cat Adult Frango e Arroz 12kg", "advance-cat-adult-frango-arroz-12kg", 4388, "Royal Canin")]
    [InlineData("Bricolagem e Construção", "Karcher K3 Lavadora de Alta Pressao", "Karcher K3 Lavadora de Alta Pressao", "karcher-k3-lavadora-alta-pressao", 9199, "Bosch Professional")]
    [InlineData("Desporto", "Cecotec Bicicleta Estatica DrumFit Indoor 10000 Teseo", "Cecotec DrumFit Indoor 10000 Teseo", "cecotec-drumfit-indoor-10000-teseo", 12299, "PlayStation")]
    public void KuantoKustaPublicProducts_ReturnMatchingProductAndConfirmedOffer(
        string category,
        string query,
        string expectedProduct,
        string expectedSlug,
        long expectedPriceCents,
        string wrongTopTerm)
    {
        var service = new ComparisonDataService(new AppText());

        var products = service.GetProducts($"{category}: {query}");

        Assert.NotEmpty(products);
        var topProduct = products[0];
        Assert.Equal(expectedSlug, topProduct.Slug);
        Assert.Equal(expectedProduct, topProduct.Name);

        var topText = string.Join(' ', topProduct.Name, topProduct.Brand, topProduct.Badge, topProduct.AiSummary, string.Join(' ', topProduct.Specs));
        Assert.DoesNotContain(wrongTopTerm, topText, StringComparison.OrdinalIgnoreCase);

        var kuantoKusta = service.BuildSellerOffersForProduct(topProduct, query)
            .SingleOrDefault(offer => offer.Seller == "KuantoKusta");

        Assert.NotNull(kuantoKusta);
        Assert.True(ComparisonDataService.IsConfirmedStoreOffer(kuantoKusta!));
        Assert.Equal(expectedPriceCents, kuantoKusta.PriceCents);
        Assert.StartsWith("https://www.kuantokusta.pt/p/", kuantoKusta.Url);
        Assert.DoesNotContain("/search", kuantoKusta.Url);
    }

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

        Assert.Contains("Frigorifico", products[0].Name, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(products, product => string.Join(' ', product.Name, string.Join(' ', product.Specs)).Contains("Classe A", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(products, product => product.Brand.Contains("Bosch", StringComparison.OrdinalIgnoreCase));
        Assert.All(products, product =>
            Assert.Contains(
                service.BuildSellerOffersForProduct(product, "comprar frigorifico classe A"),
                ComparisonDataService.IsConfirmedStoreOffer));
        Assert.DoesNotContain(products, product => product.Name == "Miele W1 Lavadora");
        Assert.DoesNotContain(products, product => product.Name == "Samsung Bespoke Lava-loiça");
    }

    [Theory]
    [InlineData("microondas")]
    [InlineData("micro-ondas compacto")]
    public void GetProducts_ReturnsMicrowaveCatalogWithConfirmedStoreOffers(string query)
    {
        var service = new ComparisonDataService(new AppText());

        var products = service.GetProducts(query);

        Assert.NotEmpty(products);
        Assert.All(products, product =>
        {
            var productText = string.Join(' ', product.Name, product.Brand, product.Badge, product.AiSummary, string.Join(' ', product.Specs));
            Assert.Contains("Microondas", productText, StringComparison.OrdinalIgnoreCase);
        });
        Assert.Contains(products, product => product.Slug == "teka-mw-fs20-wh-microondas");
        Assert.DoesNotContain(products, product => product.Name == "Bosch Serie 6 Frigorífico");
        Assert.All(products, product =>
            Assert.Contains(
                service.BuildSellerOffersForProduct(product, query),
                ComparisonDataService.IsConfirmedStoreOffer));
    }

    [Fact]
    public void GetSellerOffers_ReturnsDirectStorePagesForMicrowaves()
    {
        var service = new ComparisonDataService(new AppText());

        var cheapestOffers = service.GetSellerOffers("teka-mw-fs20-wh-microondas");
        var grillOffers = service.GetSellerOffers("teka-mw-fs20-g-wh-microondas");
        var blackGrillOffers = service.GetSellerOffers("teka-mw-fs20-g-bk-microondas");

        var castro = Assert.Single(cheapestOffers.Where(offer => offer.Seller == "Castro Electronica"));
        Assert.True(castro.IsLivePrice);
        Assert.Equal(4939, castro.PriceCents);
        Assert.Equal("https://www.castroelectronica.pt/pt/product/microondas-mw-fs20-wh-20l-700w-pretobranco--teka", castro.Url);

        var darty = Assert.Single(grillOffers.Where(offer => offer.Seller == "Darty"));
        Assert.True(darty.IsLivePrice);
        Assert.Equal(6499, darty.PriceCents);
        Assert.Equal("https://darty.pt/products/teka-microond-mw-fs20-g-wh-grill-20", darty.Url);

        var worten = Assert.Single(blackGrillOffers.Where(offer => offer.Seller == "Worten"));
        Assert.True(worten.IsLivePrice);
        Assert.Equal(8599, worten.PriceCents);
        Assert.Equal("https://www.worten.pt/produtos/microondas-teka-mwfs20gbk-20-l-grill-preto-8471475", worten.Url);
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
    public void GetProducts_ReturnsDishwasherNotWashingMachine_WhenQueryMentionsLavarLoica()
    {
        var service = new ComparisonDataService(new AppText());

        var products = service.GetProducts("Maquina de Lavar Loica Indesit IN2FE13DT9S 13 conjuntos classe E");

        Assert.NotEmpty(products);
        Assert.Equal("Indesit IN2FE13DT9S Maquina de Lavar Loica", products[0].Name);
        Assert.DoesNotContain(products, product => product.Name.Contains("Lavar Roupa", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(products, product => product.Name.Contains("Lavadora", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetSellerOffers_ReturnsConfirmedDishwasherProductPage_WhenQueryMentionsLavarLoica()
    {
        var service = new ComparisonDataService(new AppText());

        var offers = service.GetSellerOffers("indesit-in2fe13dt9s-lava-loica");
        var castro = offers.Single(offer => offer.Seller == "Castro Electronica");

        Assert.True(castro.IsLivePrice);
        Assert.Equal(23619, castro.PriceCents);
        Assert.Contains("maquina-de-lavar-loica-in2fe13dt9s", castro.Url, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/search", castro.Url);
        Assert.DoesNotContain("/pesquisa", castro.Url);
    }

    [Fact]
    public void GetProducts_ReturnsWashingMachineNotDishwasher_WhenQueryMentionsLavarRoupa()
    {
        var service = new ComparisonDataService(new AppText());

        var products = service.GetProducts("maquina de lavar roupa 8kg");

        Assert.NotEmpty(products);
        Assert.Equal("becken-bwm8812n-maquina-lavar", products[0].Slug);
        Assert.DoesNotContain(products, product => product.Name.Contains("Adidas", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(products, product => product.Name.Contains("Lavar Loica", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(products, product => product.Name.Contains("Lava-loi", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetProducts_ReturnsDryerNotFashion_WhenQueryMentionsSecarRoupa()
    {
        var service = new ComparisonDataService(new AppText());

        var products = service.GetProducts("Maquina de Secar Roupa Beko BM3T48249W 8Kg Classe C");

        Assert.NotEmpty(products);
        Assert.Equal("beko-bm3t48249w-maquina-secar-roupa", products[0].Slug);
        Assert.DoesNotContain(products, product => product.Name.Contains("Adidas", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(products, product => product.Name.Contains("Sapatilhas", StringComparison.OrdinalIgnoreCase));
        Assert.All(products, product => Assert.Contains("secar", string.Join(' ', product.Name, product.Specs, product.AiSummary), StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetSellerOffers_ReturnsConfirmedDryerProductPages_WhenQueryMentionsSecarRoupa()
    {
        var service = new ComparisonDataService(new AppText());

        var offers = service.GetSellerOffers("beko-bm3t48249w-maquina-secar-roupa");

        var kuantoKusta = offers.Single(offer => offer.Seller == "KuantoKusta");
        Assert.True(ComparisonDataService.IsConfirmedStoreOffer(kuantoKusta));
        Assert.Equal(36690, kuantoKusta.PriceCents);
        Assert.Equal("https://www.kuantokusta.pt/p/11598597/beko-bm3t48249w-8kg-classe-c", kuantoKusta.Url);
        Assert.DoesNotContain("/search", kuantoKusta.Url);

        Assert.Contains(offers, offer => offer.Seller == "Worten" && offer.Url.Contains("beko-bm3t48249w", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(offers, offer => offer.Seller == "Darty" && offer.PriceCents == 46199);
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

        Assert.Contains("40,80", g305.Price);
    }

    [Fact]
    public void GetSellerOffers_ShowsLivePricesAndDirectProductPages_ForKnownG305Stores()
    {
        var service = new ComparisonDataService(new AppText());

        var offers = service.GetSellerOffers("logitech-g305-lightspeed");
        var aquario = offers.Single(offer => offer.Seller == "Aquario");
        var globaldata = offers.Single(offer => offer.Seller == "Globaldata");
        var kuantoKusta = offers.Single(offer => offer.Seller == "KuantoKusta");
        var pcdiga = offers.Single(offer => offer.Seller == "PCDIGA");
        var radioPopular = offers.Single(offer => offer.Seller == "Radio Popular");

        Assert.True(aquario.Preferred);
        Assert.True(aquario.IsLivePrice);
        Assert.Equal(4080, aquario.PriceCents);
        Assert.Equal("https://www.aquario.pt/en/product/logitech-logitech-g305-preto-910-005283", aquario.Url);

        Assert.True(globaldata.IsLivePrice);
        Assert.Equal(4590, globaldata.PriceCents);
        Assert.Equal("https://www.globaldata.pt/rato-logitech-g-series-g305-lightspeed-wireless-gaming-preto/910-005283.html", globaldata.Url);

        Assert.True(kuantoKusta.IsLivePrice);
        Assert.Equal(4298, kuantoKusta.PriceCents);
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
        var worten = offers.Single(offer => offer.Seller == "Worten");

        Assert.True(worten.IsLivePrice);
        Assert.Equal(93999, worten.PriceCents);
        Assert.Equal("https://www.worten.pt/produtos/iphone-17-apple-6-3-256-gb-preto-8600278", worten.Url);
        Assert.DoesNotContain("/pesquisa/", worten.Url);
    }

    [Fact]
    public void GetSellerOffers_ReturnsConfirmedGalaxyWatch7StorePriceAndProductPage()
    {
        var service = new ComparisonDataService(new AppText());

        var offers = service.GetSellerOffers("samsung-galaxy-watch7");
        var kuantoKusta = offers.Single(offer => offer.Seller == "KuantoKusta");

        Assert.True(kuantoKusta.IsLivePrice);
        Assert.Equal(15700, kuantoKusta.PriceCents);
        Assert.Equal("https://www.kuantokusta.pt/p/11345402/samsung-galaxy-watch7-40mm-bt-green", kuantoKusta.Url);
        Assert.DoesNotContain("/search", kuantoKusta.Url);
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
    [InlineData("Smartwatch XIAOMI Redmi Watch 5 Active Bluetooth Autonomia ate 18 dias Preto", "Xiaomi Redmi Watch 5 Active")]
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
        var k380 = Assert.Single(products.Where(product => product.Name == "Logitech K380"));
        Assert.DoesNotContain(products, product => product.Name == "Logitech Signature M650");
        Assert.Null(k380.OfficialSource);
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
        Assert.Contains(products, product => product.Name == "Mitsai Roma II Cadeira de Escritorio");
        Assert.DoesNotContain(products, product => product.Name == "MacBook Air M3");
    }

    [Fact]
    public void GetProducts_ReturnsGamingChairCatalog_WhenQueryAsksForGamingChair()
    {
        var service = new ComparisonDataService(new AppText());

        var products = service.GetProducts("cadeira gaming");

        Assert.NotEmpty(products);
        Assert.Equal("RACINGREAT Costas Altas Cadeira Gaming", products[0].Name);
        Assert.DoesNotContain(products, product => product.Name == "PlayStation 5 Slim");
        Assert.All(products, product =>
        {
            var productText = string.Join(' ', product.Name, product.Brand, product.Badge, product.AiSummary, string.Join(' ', product.Specs));
            Assert.Contains("Cadeira", productText, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void GetSellerOffers_ReturnsConfirmedGamingChairStorePriceAndProductPage()
    {
        var service = new ComparisonDataService(new AppText());

        var offers = service.GetSellerOffers("racingreat-costas-altas-cadeira-gaming");
        var worten = offers.Single(offer => offer.Seller == "Worten");

        Assert.True(worten.IsLivePrice);
        Assert.Equal(6500, worten.PriceCents);
        Assert.Equal("https://www.worten.pt/produtos/cadeira-de-escritorio-ergonomica-racingreat-costas-altas-inclinavel-bracos-regulaveis-preto-mrkean-8711544779636", worten.Url);
        Assert.DoesNotContain("/search", worten.Url);
        Assert.DoesNotContain("/pesquisa", worten.Url);
    }

    [Theory]
    [InlineData("disco externo 1tb usb 3.2", "Western Digital My Passport 1TB", "MacBook Air M3")]
    [InlineData("monitor 27 polegadas 144hz", "LG UltraGear 27GP850-B", "iPhone 17")]
    [InlineData("auscultadores bluetooth noise cancelling", "Sony WH-1000XM5", "Samsung Galaxy S24")]
    [InlineData("televisor 55 polegadas oled", "LG OLED C4 55\"", "Dell XPS 14")]
    [InlineData("maquina de cafe automatica", "De'Longhi Magnifica Start", "Bosch Serie 6 Frigorífico")]
    [InlineData("delta q mini qool", "Delta Q Mini Qool Cinzento", "Delta Q Qalidus Capsulas")]
    [InlineData("berbequim sem fios 18v", "Bosch Professional GSB 18V-55", "MacBook Air M3")]
    [InlineData("karcher k3 lavadora alta pressao", "Karcher K3 Lavadora de Alta Pressao", "Becken Boostwash BWM8812N Maquina de Lavar Roupa")]
    [InlineData("pneu 205 55 r16", "Michelin Primacy 4+ 205/55 R16", "Samsung Galaxy S24")]
    [InlineData("impressora multifuncoes wifi", "Epson EcoTank L3250", "MacBook Air M3")]
    [InlineData("tinteiro hp 308 preto tricolor pack 2x", "Tinteiro HP 308 Preto/Tricolor Pack 2x", "HP DeskJet 2921 All-in-One")]
    [InlineData("racao cao adulto 15kg", "Royal Canin Medium Adult 15kg", "Bosch Serie 6 Frigorífico")]
    [InlineData("advance cat adult frango arroz 12kg", "Advance Cat Adult Frango e Arroz 12kg", "Bosch Serie 6 Frigorífico")]
    [InlineData("fraldas bebe tamanho 4", "Pampers Premium Protection T4", "iPhone 17")]
    [InlineData("mesa escritorio ate 100 euros", "IKEA LAGKAPTEN / ADILS", "Sony WH-1000XM5")]
    [InlineData("aspirador robot com mapeamento", "Roborock Q8 Max", "Bosch Serie 6 Frigorífico")]
    [InlineData("maquina de lavar roupa hisense 7kg", "Hisense WF1G7021BW Maquina de Lavar Roupa", "Indesit IN2FE13DT9S Maquina de Lavar Loica")]
    [InlineData("maquina de secar roupa beko bm3t48249w 8kg classe c", "Beko BM3T48249W Maquina de Secar Roupa 8Kg", "Adidas Adizero Evo SL")]
    [InlineData("bicicleta estatica cecotec drumfit indoor 10000 teseo", "Cecotec DrumFit Indoor 10000 Teseo", "PlayStation 5 Slim")]
    [InlineData("consola ps5", "PlayStation 5 Slim", "Nintendo Switch OLED")]
    [InlineData("smartwatch android", "Samsung Galaxy Watch8", "MacBook Air M3")]
    [InlineData("camera vigilancia interior wifi", "TP-Link Tapo C200", "iPhone 17")]
    [InlineData("trotinete eletrica cidade", "Xiaomi 4 Lite 2nd Gen Trotinete", "Bosch Serie 6 Frigorífico")]
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

        Assert.Equal("Worten", offer.Seller);
        Assert.Equal(244900, offer.PriceCents);
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

