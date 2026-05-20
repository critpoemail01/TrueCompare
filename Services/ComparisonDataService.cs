using System.Globalization;
using System.Text;
using TrueCompare.Models;

namespace TrueCompare.Services;

public sealed class ComparisonDataService(AppText text)
{
    private const string LaptopCatalog = "laptops";
    private const string SmartphoneCatalog = "smartphones";
    private const string ApplianceCatalog = "appliances";
    private const string MouseCatalog = "mice";

    private static readonly string[] SmartphoneCatalogTerms =
    [
        "smartphone",
        "smartphones",
        "telemovel",
        "phone",
        "phones",
        "mobile",
        "mobiles",
        "iphone",
        "android",
        "pixel",
        "xiaomi",
        "galaxy",
        "celular"
    ];

    private static readonly string[] ApplianceCatalogTerms =
    [
        "eletrodomestico",
        "eletrodomesticos",
        "electrodomestico",
        "electrodomesticos",
        "appliance",
        "appliances",
        "frigorifico",
        "fridge",
        "geladeira",
        "combinado",
        "maquina de lavar",
        "maquina lavar",
        "lavadora",
        "lavagem",
        "lava loica",
        "lava-loica",
        "dishwasher",
        "washer",
        "eficiencia a",
        "classe a",
        "a+++",
        "cozinha"
    ];

    private static readonly string[] LaptopCatalogTerms =
    [
        "laptop",
        "laptops",
        "portatil",
        "portateis",
        "notebook",
        "computador",
        "macbook",
        "thinkpad",
        "zenbook",
        "xps",
        "ultrabook",
        "pc profissional"
    ];

    private static readonly string[] MouseCatalogTerms =
    [
        "rato",
        "ratos",
        "mouse",
        "mice",
        "wireless mouse",
        "bluetooth mouse",
        "gaming mouse",
        "rato gaming",
        "dpi"
    ];

    public IReadOnlyList<string> Categories => text.IsEnglish
        ? new List<string>
        {
            "Premium smartphones",
            "Professional laptops",
            "A+++ efficient appliances",
            "Mice and peripherals under €50",
            "Industrial tools",
            "Sports equipment",
            "Car accessories",
            "Furniture & decor",
            "Audio & DJ equipment"
        }
        : new List<string>
        {
            "Smartphones premium",
            "Portáteis profissionais",
            "Eletrodomésticos eficiência A+++",
            "Ratos e periféricos até 50€",
            "Ferramentas industriais",
            "Equipamento desportivo",
            "Acessórios automóvel",
            "Móveis & decoração",
            "Áudio & DJ equipment"
        };

    public IReadOnlyList<CriteriaWeight> DefaultWeights => GetCriteriaWeights(null);

    public IReadOnlyList<ProductResult> Products => GetProducts(null);

    public IReadOnlyList<SellerOffer> SellerOffers => GetSellerOffers(null);

    public IReadOnlyList<FeatureItem> Features => text.IsEnglish
        ? new List<FeatureItem>
        {
            new("01", "Smart prompt", "Describe what you need in natural language, without complex filters.", "#7BE8E0"),
            new("02", "Custom weights", "Choose what matters: price, battery, warranty and support.", "#5EE9A8"),
            new("03", "Independent comparison", "Zero sponsorships, zero affiliates. Products are ranked by merit.", "#B49CFF"),
            new("04", "Anti-fraud verification", "Detect suspicious listings, validate sellers and serial numbers.", "#E97B7B"),
            new("05", "Secure checkout", "Order through the app with authenticity and return guarantees.", "#E9D67B"),
            new("06", "Video tutorials", "Learn each feature in less than 2 minutes in the tutorial center.", "#F0A36A")
        }
        : new List<FeatureItem>
        {
            new("01", "Prompt inteligente", "Descreve em linguagem natural o que procuras — sem filtros complicados.", "#7BE8E0"),
            new("02", "Pesos personalizados", "Define o que valorizas (preço, autonomia, garantia) e a IA ranqueia por ti.", "#5EE9A8"),
            new("03", "Comparação isenta", "Zero patrocínios, zero afiliados. Os produtos são ranqueados pelo mérito.", "#B49CFF"),
            new("04", "Verificação anti-fraude", "Deteção de listagens suspeitas, validação de vendedores e número de série.", "#E97B7B"),
            new("05", "Checkout seguro", "Encomenda directa pela app com garantia de autenticidade e devolução.", "#E9D67B"),
            new("06", "Tutoriais em vídeo", "Aprende cada funcionalidade em menos de 2 minutos no centro de tutoriais.", "#F0A36A")
        };

    public IReadOnlyList<TutorialItem> Tutorials => text.IsEnglish
        ? new List<TutorialItem>
        {
            new("Beginners", "Create the first prompt", "1:42"),
            new("Beginners", "Adjust criteria weights", "2:15"),
            new("Beginners", "Read the AI score", "1:58"),
            new("Advanced", "Add a product manually", "1:30"),
            new("Security", "Detect fake sellers", "3:05"),
            new("Orders", "Order with secure payment", "2:40")
        }
        : new List<TutorialItem>
        {
            new("Iniciantes", "Como criar o primeiro prompt", "1:42"),
            new("Iniciantes", "Ajustar pesos dos critérios", "2:15"),
            new("Iniciantes", "Ler o score da IA", "1:58"),
            new("Avançado", "Adicionar produto manual", "1:30"),
            new("Segurança", "Detectar vendedores falsos", "3:05"),
            new("Encomendas", "Encomendar com pagamento seguro", "2:40")
        };

    private static IReadOnlyList<ProductResult> LaptopProducts { get; } = new List<ProductResult>
    {
        new(
            "macbook-air-m3",
            1,
            94,
            "MacBook Air M3",
            "Apple",
            "1.299 €",
            "#5EE9A8",
            "Recomendado",
            new[] { "14\"", "1.24 kg", "18h autonomia", "M3", "16GB", "Garantia 2 anos" },
            new[] { "Autonomia 38% acima da média", "Peso reduzido", "Garantia oficial em Portugal" },
            new[] { "Número de série validado", "Revendedor autorizado", "Stock real confirmado", "Garantia oficial registável em PT", "Sem histórico de contrafação" },
            new[]
            {
                new FraudAlert("marketplace-xyz.com", "Preço 60% abaixo do mercado"),
                new FraudAlert("deals-eu.shop", "Domínio criado há 14 dias"),
                new FraudAlert("tech-outlet.io", "Sem registo fiscal verificável")
            },
            "Este produto cumpre 94% dos critérios definidos. Pontos fortes: autonomia, peso reduzido e garantia oficial."
        ),
        new(
            "thinkpad-x1-carbon",
            2,
            89,
            "ThinkPad X1 Carbon",
            "Lenovo",
            "1.450 €",
            "#7BE8E0",
            "Melhor garantia",
            new[] { "14\"", "1.12 kg", "15h autonomia", "i7", "16GB", "Garantia 3 anos" },
            new[] { "Mais leve da lista", "Garantia superior", "Bom equilíbrio profissional" },
            new[] { "Revendedor autorizado", "Garantia validada", "Stock cruzado em 3 fontes" },
            Array.Empty<FraudAlert>(),
            "Excelente opção para suporte e mobilidade. Fica atrás do primeiro lugar pelo preço mais alto."
        ),
        new(
            "dell-xps-14",
            3,
            82,
            "Dell XPS 14",
            "Dell",
            "1.380 €",
            "#7BE8E0",
            "Ecrã premium",
            new[] { "14\"", "1.59 kg", "12h autonomia", "i7", "16GB", "Garantia 2 anos" },
            new[] { "Boa performance", "Ecrã forte", "Construção sólida" },
            new[] { "Revendedor autorizado", "Garantia validada", "Sem alertas críticos" },
            Array.Empty<FraudAlert>(),
            "Boa escolha se o ecrã for prioritário. Penalizado pelo peso e autonomia abaixo dos líderes."
        ),
        new(
            "asus-zenbook-14",
            4,
            78,
            "ASUS ZenBook 14",
            "ASUS",
            "1.150 €",
            "#7BE8E0",
            "Melhor preço",
            new[] { "14\"", "1.20 kg", "11h autonomia", "i5", "16GB", "Garantia 2 anos" },
            new[] { "Preço competitivo", "Leve", "Boa relação valor" },
            new[] { "Vendedor autorizado", "Garantia validada", "Preço dentro do mercado" },
            Array.Empty<FraudAlert>(),
            "A melhor opção para orçamento controlado. Menos indicado para workloads pesados."
        )
    };

    private static IReadOnlyList<ProductResult> SmartphoneProducts { get; } = new List<ProductResult>
    {
        new(
            "iphone-15-pro",
            1,
            93,
            "iPhone 15 Pro",
            "Apple",
            "999 €",
            "#5EE9A8",
            "Recomendado",
            new[] { "6.1\"", "187 g", "23h vídeo", "A17 Pro", "128GB", "Garantia 3 anos" },
            new[] { "Melhor ecossistema", "Câmara consistente", "Suporte longo" },
            new[] { "IMEI validável", "Revendedor autorizado", "Garantia UE confirmada", "Stock real confirmado", "Sem alertas críticos" },
            new[]
            {
                new FraudAlert("phones-discount.shop", "Preço 45% abaixo do mercado"),
                new FraudAlert("market-eu-mobile.net", "Sem informação fiscal verificável")
            },
            "Melhor escolha premium para quem valoriza câmara, desempenho e suporte prolongado."
        ),
        new(
            "samsung-galaxy-s24",
            2,
            90,
            "Samsung Galaxy S24",
            "Samsung",
            "759 €",
            "#7BE8E0",
            "Melhor Android",
            new[] { "6.2\"", "168 g", "4000 mAh", "Exynos 2400", "256GB", "Garantia 3 anos" },
            new[] { "Ecrã excelente", "Atualizações longas", "Boa autonomia" },
            new[] { "IMEI validável", "Revendedor autorizado", "Garantia UE confirmada" },
            Array.Empty<FraudAlert>(),
            "A opção Android mais equilibrada para desempenho, ecrã e longevidade."
        ),
        new(
            "google-pixel-8",
            3,
            86,
            "Google Pixel 8",
            "Google",
            "619 €",
            "#B49CFF",
            "Melhor câmara IA",
            new[] { "6.2\"", "187 g", "4575 mAh", "Tensor G3", "128GB", "Garantia 3 anos" },
            new[] { "Fotografia forte", "Android limpo", "Excelente valor" },
            new[] { "Revendedor autorizado", "Garantia validada", "Sem alertas críticos" },
            Array.Empty<FraudAlert>(),
            "Ideal para fotografia computacional e experiência Android limpa."
        ),
        new(
            "xiaomi-14",
            4,
            82,
            "Xiaomi 14",
            "Xiaomi",
            "699 €",
            "#E9D67B",
            "Melhor preço/performance",
            new[] { "6.36\"", "193 g", "4610 mAh", "Snapdragon 8 Gen 3", "256GB", "Garantia 3 anos" },
            new[] { "Performance elevada", "Carregamento rápido", "Preço agressivo" },
            new[] { "Revendedor autorizado", "Garantia UE confirmada", "Preço dentro do mercado" },
            Array.Empty<FraudAlert>(),
            "Muito forte em performance e carregamento, com preço competitivo."
        )
    };

    private static IReadOnlyList<ProductResult> ApplianceProducts { get; } = new List<ProductResult>
    {
        new(
            "bosch-serie-6-frigorifico",
            1,
            91,
            "Bosch Serie 6 Frigorífico",
            "Bosch",
            "899 €",
            "#5EE9A8",
            "Mais eficiente",
            new[] { "Classe A+++", "366 L", "No Frost", "35 dB", "203 cm", "Garantia 3 anos" },
            new[] { "Consumo muito baixo", "Silencioso", "Boa capacidade familiar" },
            new[] { "Número de série validado", "Revendedor autorizado", "Etiqueta energética confirmada", "Stock real confirmado", "Garantia UE confirmada" },
            new[]
            {
                new FraudAlert("home-clearance.shop", "Preço 50% abaixo do mercado"),
                new FraudAlert("outlet-domestico.net", "Sem registo fiscal verificável")
            },
            "Melhor escolha para frigorífico eficiente, silencioso e com garantia forte."
        ),
        new(
            "lg-instaview-combinado",
            2,
            88,
            "LG InstaView Combinado",
            "LG",
            "1.049 €",
            "#7BE8E0",
            "Melhor tecnologia",
            new[] { "Classe A++", "635 L", "DoorCooling+", "36 dB", "Wi-Fi", "Garantia 3 anos" },
            new[] { "Capacidade superior", "Arrefecimento rápido", "Funcionalidades smart" },
            new[] { "Revendedor autorizado", "Garantia validada", "Etiqueta energética confirmada" },
            Array.Empty<FraudAlert>(),
            "Excelente para famílias que precisam de grande capacidade e controlo inteligente."
        ),
        new(
            "miele-w1-lavadora",
            3,
            85,
            "Miele W1 Lavadora",
            "Miele",
            "949 €",
            "#B49CFF",
            "Melhor durabilidade",
            new[] { "Classe A+++", "9 kg", "1400 rpm", "47 dB", "TwinDos", "Garantia 3 anos" },
            new[] { "Construção robusta", "Dosagem automática", "Lavagem silenciosa" },
            new[] { "Revendedor autorizado", "Garantia validada", "Sem alertas críticos" },
            Array.Empty<FraudAlert>(),
            "Boa escolha quando durabilidade e qualidade de lavagem pesam mais que preço inicial."
        ),
        new(
            "samsung-bespoke-lava-loica",
            4,
            81,
            "Samsung Bespoke Lava-loiça",
            "Samsung",
            "579 €",
            "#E9D67B",
            "Melhor preço",
            new[] { "Classe A++", "14 serviços", "44 dB", "Auto Door", "Wi-Fi", "Garantia 3 anos" },
            new[] { "Preço competitivo", "Baixo ruído", "Programas inteligentes" },
            new[] { "Vendedor autorizado", "Garantia UE confirmada", "Preço dentro do mercado" },
            Array.Empty<FraudAlert>(),
            "Opção equilibrada para cozinha moderna com bom preço e baixo ruído."
        )
    };

    private static IReadOnlyList<ProductResult> MouseProducts { get; } = new List<ProductResult>
    {
        new(
            "logitech-m650-signature",
            1,
            92,
            "Logitech M650 Signature",
            "Logitech",
            "34,99 €",
            "#5EE9A8",
            "Melhor geral",
            new[] { "Rato sem fios", "Bluetooth + USB", "24 meses bateria", "125 g", "Garantia 2 anos", "Até 50 €" },
            new[] { "Confortável para uso diário", "Cliques silenciosos", "Boa autonomia" },
            new[] { "Vendedor autorizado", "Garantia validada", "Stock real confirmado", "Preço dentro do mercado", "Sem alertas críticos" },
            Array.Empty<FraudAlert>(),
            "Melhor escolha abaixo de 50€ para trabalho diário, conforto e autonomia sem fios."
        ),
        new(
            "logitech-g305-lightspeed",
            2,
            90,
            "Logitech G305 Lightspeed",
            "Logitech",
            "44,99 €",
            "#7BE8E0",
            "Melhor gaming",
            new[] { "Rato gaming", "Sem fios", "12000 DPI", "99 g", "Garantia 2 anos", "Até 50 €" },
            new[] { "Baixa latência", "Sensor preciso", "Bom para jogos e trabalho" },
            new[] { "Vendedor autorizado", "Garantia validada", "Preço dentro do mercado" },
            Array.Empty<FraudAlert>(),
            "A melhor opção para quem quer um rato gaming sem fios até 50€."
        ),
        new(
            "microsoft-bluetooth-mouse",
            3,
            84,
            "Microsoft Bluetooth Mouse",
            "Microsoft",
            "24,99 €",
            "#B49CFF",
            "Mais barato",
            new[] { "Rato bluetooth", "Sem dongle", "12 meses bateria", "78 g", "Garantia 2 anos", "Até 50 €" },
            new[] { "Preço baixo", "Simples de transportar", "Bom para portátil" },
            new[] { "Vendedor autorizado", "Garantia validada", "Preço dentro do mercado" },
            Array.Empty<FraudAlert>(),
            "Opção económica e simples para quem quer um rato bluetooth leve."
        ),
        new(
            "razer-deathadder-essential",
            4,
            82,
            "Razer DeathAdder Essential",
            "Razer",
            "29,99 €",
            "#E9D67B",
            "Melhor ergonómico",
            new[] { "Rato com fio", "6400 DPI", "Ergonómico", "96 g", "Garantia 2 anos", "Até 50 €" },
            new[] { "Formato confortável", "Sensor fiável", "Bom preço" },
            new[] { "Vendedor autorizado", "Garantia validada", "Sem alertas críticos" },
            Array.Empty<FraudAlert>(),
            "Boa escolha se preferes rato com fio, formato ergonómico e preço baixo."
        )
    };

    private static IReadOnlyList<ProductResult> AllProducts { get; } = LaptopProducts
        .Concat(SmartphoneProducts)
        .Concat(ApplianceProducts)
        .Concat(MouseProducts)
        .ToList();

    private static IReadOnlyList<SellerOffer> LaptopOffers { get; } = new List<SellerOffer>
    {
        new("Apple Store PT", "1.299 €", 129900, "2 dias", "2 anos oficial", "Verificado", "https://www.apple.com/pt/shop/buy-mac/macbook-air", false),
        new("FNAC", "1.319 €", 131900, "3 dias", "2 anos oficial", "Verificado", "https://www.fnac.pt/SearchResult/ResultList.aspx?Search=MacBook+Air+M3", false),
        new("Worten", "1.329 €", 132900, "4 dias", "2 anos oficial", "Verificado", "https://www.worten.pt/search?query=MacBook%20Air%20M3", false),
        new("Amazon.es", "1.279 €", 127900, "5-7 dias", "2 anos oficial", "Autorizado", "https://www.amazon.es/s?k=MacBook+Air+M3", true),
        new("MediaMarkt", "1.349 €", 134900, "3 dias", "2 anos oficial", "Verificado", "https://www.mediamarkt.pt/pt/search.html?query=MacBook%20Air%20M3", false)
    };

    private static IReadOnlyList<SellerOffer> SmartphoneOffers { get; } = new List<SellerOffer>
    {
        new("Amazon.es", "749 €", 74900, "2-4 dias", "3 anos UE", "Autorizado", "https://www.amazon.es/s?k=smartphone+premium", true),
        new("Worten", "779 €", 77900, "2 dias", "3 anos PT", "Verificado", "https://www.worten.pt/search?query=smartphone%20premium", false),
        new("FNAC", "789 €", 78900, "3 dias", "3 anos PT", "Verificado", "https://www.fnac.pt/SearchResult/ResultList.aspx?Search=smartphone+premium", false),
        new("MediaMarkt", "799 €", 79900, "2-3 dias", "3 anos PT", "Verificado", "https://www.mediamarkt.pt/pt/search.html?query=smartphone%20premium", false),
        new("Samsung Store PT", "809 €", 80900, "2 dias", "3 anos oficial", "Verificado", "https://www.samsung.com/pt/smartphones/", false)
    };

    private static IReadOnlyList<SellerOffer> ApplianceOffers { get; } = new List<SellerOffer>
    {
        new("Worten", "879 €", 87900, "2 dias", "3 anos PT", "Verificado", "https://www.worten.pt/search?query=frigorifico%20bosch%20serie%206", true),
        new("Radio Popular", "899 €", 89900, "3 dias", "3 anos PT", "Verificado", "https://www.radiopopular.pt/pesquisa/frigorifico%20bosch%20serie%206", false),
        new("MediaMarkt", "919 €", 91900, "2-3 dias", "3 anos PT", "Verificado", "https://www.mediamarkt.pt/pt/search.html?query=frigorifico%20bosch%20serie%206", false),
        new("Amazon.es", "929 €", 92900, "4-7 dias", "3 anos UE", "Autorizado", "https://www.amazon.es/s?k=bosch+serie+6+frigorifico", false),
        new("KuantoKusta", "889 €", 88900, "Confirmar loja", "Validar vendedor", "A comparar", "https://www.kuantokusta.pt/search?q=bosch%20serie%206%20frigorifico", false)
    };

    private static IReadOnlyList<SellerOffer> MouseOffers { get; } = new List<SellerOffer>
    {
        new("Amazon.es", "32,99 €", 3299, "2-4 dias", "3 anos UE", "Autorizado", "https://www.amazon.es/s?k=rato+sem+fios+ate+50+euros", true),
        new("Worten", "34,99 €", 3499, "2 dias", "3 anos PT", "Verificado", "https://www.worten.pt/search?query=rato%20sem%20fios%20ate%2050%20euros", false),
        new("FNAC", "36,99 €", 3699, "3 dias", "3 anos PT", "Verificado", "https://www.fnac.pt/SearchResult/ResultList.aspx?Search=rato+sem+fios+ate+50+euros", false),
        new("MediaMarkt", "39,99 €", 3999, "2-3 dias", "3 anos PT", "Verificado", "https://www.mediamarkt.pt/pt/search.html?query=rato%20sem%20fios%20ate%2050%20euros", false),
        new("PCDIGA", "44,99 €", 4499, "1-2 dias", "3 anos PT", "Verificado", "https://www.pcdiga.com/pesquisa/rato%20sem%20fios%20ate%2050%20euros", false)
    };

    public IReadOnlyList<ProductResult> GetProducts(string? query)
    {
        return LocalizeProducts(ResolveProducts(query));
    }

    public IReadOnlyList<CriteriaWeight> GetCriteriaWeights(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return LaptopCriteria();
        }

        var normalizedQuery = NormalizeCatalogText(query);
        if (ContainsAnyTerm(normalizedQuery, "rato", "ratos", "mouse", "mice"))
        {
            return MouseCriteria();
        }

        if (ContainsAnyTerm(normalizedQuery, "cadeira", "cadeiras", "chair", "chairs"))
        {
            return ChairCriteria();
        }

        if (ContainsAnyTerm(normalizedQuery, "teclado", "teclados", "keyboard", "keyboards"))
        {
            return KeyboardCriteria();
        }

        if (ContainsAnyTerm(normalizedQuery, "monitor", "monitores", "display", "ecra", "ecran"))
        {
            return MonitorCriteria();
        }

        if (ContainsAnyTerm(normalizedQuery, "auscultadores", "auriculares", "headphones", "headset", "earbuds"))
        {
            return HeadphonesCriteria();
        }

        return TryResolveCatalog(query.Trim(), out var catalog) switch
        {
            true when catalog == SmartphoneCatalog => SmartphoneCriteria(),
            true when catalog == ApplianceCatalog => ApplianceCriteria(),
            true when catalog == MouseCatalog => MouseCriteria(),
            true when catalog == LaptopCatalog => LaptopCriteria(),
            _ => GenericProductCriteria()
        };
    }

    public IReadOnlyList<SellerOffer> GetSellerOffers(string? queryOrSlug)
    {
        var offers = ResolveCatalog(queryOrSlug) switch
        {
            SmartphoneCatalog => SmartphoneOffers,
            ApplianceCatalog => ApplianceOffers,
            MouseCatalog => MouseOffers,
            _ => LaptopOffers
        };

        return LocalizeOffers(offers);
    }

    private IReadOnlyList<ProductResult> ResolveProducts(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return LaptopProducts;
        }

        var rawValue = query.Trim();
        var hasCatalog = TryResolveCatalog(rawValue, out var catalog);
        if (!hasCatalog && IsUnsupportedKnownCategory(NormalizeCatalogText(rawValue)))
        {
            return Array.Empty<ProductResult>();
        }

        var candidates = hasCatalog ? GetCatalogProducts(catalog) : AllProducts;
        var ranked = RankProducts(candidates, rawValue, hasCatalog);

        if (ranked.Count > 0)
        {
            return ranked;
        }

        return hasCatalog ? candidates : Array.Empty<ProductResult>();
    }

    private static IReadOnlyList<ProductResult> GetCatalogProducts(string catalog)
    {
        return catalog switch
        {
            SmartphoneCatalog => SmartphoneProducts,
            ApplianceCatalog => ApplianceProducts,
            MouseCatalog => MouseProducts,
            _ => LaptopProducts
        };
    }

    private static IReadOnlyList<ProductResult> RankProducts(
        IReadOnlyList<ProductResult> products,
        string query,
        bool includeUnmatchedProducts)
    {
        var normalizedQuery = NormalizeCatalogText(query);
        var searchTerms = BuildSearchTerms(normalizedQuery);
        var ranked = products
            .Select(product => new
            {
                Product = product,
                Relevance = ScoreProduct(product, normalizedQuery, searchTerms)
            })
            .OrderByDescending(item => item.Relevance)
            .ThenByDescending(item => item.Product.Score)
            .ToList();

        if (ranked.Count == 0 || ranked[0].Relevance <= 0)
        {
            return Array.Empty<ProductResult>();
        }

        var matches = includeUnmatchedProducts
            ? ranked
            : ranked.Where(item => item.Relevance > 0);

        return matches
            .Take(4)
            .Select((item, index) => item.Product with { Rank = index + 1 })
            .ToList();
    }

    private static IReadOnlyList<string> BuildSearchTerms(string normalizedQuery)
    {
        var rawTerms = normalizedQuery
            .Replace('-', ' ')
            .Replace('/', ' ')
            .Replace(',', ' ')
            .Replace('.', ' ')
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "comprar",
            "compra",
            "quero",
            "queria",
            "procuro",
            "para",
            "com",
            "sem",
            "uma",
            "um",
            "uns",
            "umas",
            "melhor",
            "best",
            "buy",
            "the",
            "and",
            "for",
            "with",
            "mais",
            "menos",
            "bom",
            "boa",
            "bons",
            "boas",
            "produto",
            "produtos",
            "vendedor",
            "revendedor",
            "autorizado",
            "autorizada",
            "seller",
            "authorized",
            "loja",
            "store",
            "ate",
            "até",
            "euro",
            "euros",
            "eur",
            "orçamento",
            "orcamento",
            "máximo",
            "maximo",
            "max",
            "ergonomico",
            "ergonomica",
            "confortavel",
            "confortavel",
            "gaming"
        };

        return rawTerms
            .Where(term => term.Length > 2 && !stopWords.Contains(term))
            .SelectMany(ExpandSearchTerm)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IEnumerable<string> ExpandSearchTerm(string term)
    {
        yield return term;

        foreach (var expanded in term switch
        {
            "barato" or "barata" or "economico" or "economica" or "baixo" or "baixa" or "cheap" => new[] { "preco", "valor", "competitivo", "agressivo" },
            "camera" or "camara" or "foto" or "fotografia" => new[] { "camera", "camara", "fotografia" },
            "bateria" or "autonomia" => new[] { "autonomia", "bateria", "mah", "video" },
            "leve" or "portatil" or "portabilidade" => new[] { "leve", "peso", "kg", "portabilidade" },
            "rapido" or "rapida" or "performance" or "jogos" or "gaming" => new[] { "performance", "snapdragon", "i7", "m3", "rapido" },
            "frio" or "frigorifico" or "fridge" or "geladeira" => new[] { "frigorifico", "combinado", "frost", "arrefecimento" },
            "roupa" or "lavar" or "lavagem" or "lavadora" => new[] { "lavadora", "lavagem", "maquina", "twindos" },
            "loica" or "loicas" or "louca" or "dishwasher" => new[] { "loica", "lava", "dishwasher" },
            "silencioso" or "silenciosa" or "ruido" => new[] { "silencioso", "ruido", "db" },
            "rato" or "ratos" or "mouse" or "mice" => new[] { "rato", "mouse", "sem fios", "bluetooth", "dpi" },
            "ergonomico" or "ergonomica" => new[] { "ergonomico", "confortavel", "formato" },
            _ => Array.Empty<string>()
        })
        {
            yield return expanded;
        }
    }

    private static int ScoreProduct(ProductResult product, string normalizedQuery, IReadOnlyList<string> searchTerms)
    {
        var slug = NormalizeCatalogText(product.Slug);
        var name = NormalizeCatalogText(product.Name);
        var brand = NormalizeCatalogText(product.Brand);
        var specs = NormalizeCatalogText(string.Join(' ', product.Specs));
        var highlights = NormalizeCatalogText(string.Join(' ', product.Highlights));
        var badge = NormalizeCatalogText(product.Badge);
        var summary = NormalizeCatalogText(product.AiSummary);

        var score = 0;
        if (normalizedQuery.Contains(name, StringComparison.OrdinalIgnoreCase)
            || name.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase))
        {
            score += 100;
        }

        foreach (var term in searchTerms)
        {
            if (name.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                score += 18;
            }

            if (slug.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                score += 14;
            }

            if (brand.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                score += 12;
            }

            if (highlights.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                score += 8;
            }

            if (summary.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                score += 8;
            }

            if (specs.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                score += 6;
            }

            if (badge.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                score += 5;
            }
        }

        return score;
    }

    public ProductResult? FindProduct(string slug)
    {
        var product = AllProducts
            .FirstOrDefault(product => product.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));

        return product is null ? null : LocalizeProduct(product);
    }

    public SellerOffer GetBestOffer(string? queryOrSlug = null)
    {
        return GetSellerOffers(queryOrSlug).OrderBy(offer => offer.PriceCents).First();
    }

    private string ResolveCatalog(string? queryOrSlug)
    {
        return !string.IsNullOrWhiteSpace(queryOrSlug) && TryResolveCatalog(queryOrSlug.Trim(), out var catalog)
            ? catalog
            : LaptopCatalog;
    }

    private static bool TryResolveCatalog(string rawValue, out string catalog)
    {
        var value = NormalizeCatalogText(rawValue);

        if (MatchesKnownProduct(value, rawValue, SmartphoneProducts))
        {
            catalog = SmartphoneCatalog;
            return true;
        }

        if (MatchesKnownProduct(value, rawValue, ApplianceProducts))
        {
            catalog = ApplianceCatalog;
            return true;
        }

        if (MatchesKnownProduct(value, rawValue, MouseProducts))
        {
            catalog = MouseCatalog;
            return true;
        }

        if (MatchesKnownProduct(value, rawValue, LaptopProducts))
        {
            catalog = LaptopCatalog;
            return true;
        }

        var scores = new[]
        {
            new { Catalog = SmartphoneCatalog, Score = ScoreCatalog(value, SmartphoneCatalogTerms) },
            new { Catalog = ApplianceCatalog, Score = ScoreCatalog(value, ApplianceCatalogTerms) },
            new { Catalog = MouseCatalog, Score = ScoreCatalog(value, MouseCatalogTerms) },
            new { Catalog = LaptopCatalog, Score = ScoreCatalog(value, LaptopCatalogTerms) }
        }
        .OrderByDescending(item => item.Score)
        .ToList();

        if (scores[0].Score <= 0)
        {
            catalog = LaptopCatalog;
            return false;
        }

        catalog = scores[0].Catalog;
        return true;
    }

    private IReadOnlyList<CriteriaWeight> LaptopCriteria()
    {
        return text.IsEnglish
            ? BuildCriteria(("Price", 80, "#7BE8E0"), ("Battery life", 95, "#5EE9A8"), ("Weight and portability", 70, "#7BE8E0"), ("Performance (CPU)", 60, "#7BE8E0"), ("Display quality", 50, "#B49CFF"), ("Warranty and support", 75, "#5EE9A8"), ("Sustainability", 40, "#E9D67B"), ("Origin / manufacturing", 30, "#F0A36A"))
            : BuildCriteria(("Preço", 80, "#7BE8E0"), ("Autonomia", 95, "#5EE9A8"), ("Peso e portabilidade", 70, "#7BE8E0"), ("Performance (CPU)", 60, "#7BE8E0"), ("Qualidade do ecrã", 50, "#B49CFF"), ("Garantia e suporte", 75, "#5EE9A8"), ("Sustentabilidade", 40, "#E9D67B"), ("Origem / fabrico", 30, "#F0A36A"));
    }

    private IReadOnlyList<CriteriaWeight> SmartphoneCriteria()
    {
        return text.IsEnglish
            ? BuildCriteria(("Price", 80, "#7BE8E0"), ("Camera quality", 92, "#B49CFF"), ("Battery life", 85, "#5EE9A8"), ("Updates and support", 88, "#5EE9A8"), ("Display", 72, "#7BE8E0"), ("Performance", 66, "#7BE8E0"), ("Storage", 55, "#E9D67B"), ("Warranty / authorized seller", 82, "#F0A36A"))
            : BuildCriteria(("Preço", 80, "#7BE8E0"), ("Qualidade da câmara", 92, "#B49CFF"), ("Bateria", 85, "#5EE9A8"), ("Atualizações e suporte", 88, "#5EE9A8"), ("Ecrã", 72, "#7BE8E0"), ("Performance", 66, "#7BE8E0"), ("Armazenamento", 55, "#E9D67B"), ("Garantia / vendedor autorizado", 82, "#F0A36A"));
    }

    private IReadOnlyList<CriteriaWeight> ApplianceCriteria()
    {
        return text.IsEnglish
            ? BuildCriteria(("Price", 75, "#7BE8E0"), ("Energy efficiency", 95, "#5EE9A8"), ("Capacity", 85, "#7BE8E0"), ("Noise level", 70, "#B49CFF"), ("Annual consumption", 85, "#5EE9A8"), ("Dimensions / installation", 65, "#E9D67B"), ("Warranty / assistance", 82, "#F0A36A"), ("Delivery / removal", 60, "#7BE8E0"))
            : BuildCriteria(("Preço", 75, "#7BE8E0"), ("Eficiência energética", 95, "#5EE9A8"), ("Capacidade", 85, "#7BE8E0"), ("Nível de ruído", 70, "#B49CFF"), ("Consumo anual", 85, "#5EE9A8"), ("Dimensões / instalação", 65, "#E9D67B"), ("Garantia / assistência", 82, "#F0A36A"), ("Entrega / recolha", 60, "#7BE8E0"));
    }

    private IReadOnlyList<CriteriaWeight> MouseCriteria()
    {
        return text.IsEnglish
            ? BuildCriteria(("Price", 80, "#7BE8E0"), ("Ergonomics", 90, "#5EE9A8"), ("Sensor / DPI", 78, "#B49CFF"), ("Connectivity", 72, "#7BE8E0"), ("Battery / cable", 65, "#5EE9A8"), ("Weight and size", 60, "#E9D67B"), ("Click noise", 45, "#F0A36A"), ("Warranty / seller", 75, "#7BE8E0"))
            : BuildCriteria(("Preço", 80, "#7BE8E0"), ("Ergonomia", 90, "#5EE9A8"), ("Sensor / DPI", 78, "#B49CFF"), ("Conectividade", 72, "#7BE8E0"), ("Bateria / cabo", 65, "#5EE9A8"), ("Peso e tamanho", 60, "#E9D67B"), ("Ruído dos cliques", 45, "#F0A36A"), ("Garantia / vendedor", 75, "#7BE8E0"));
    }

    private IReadOnlyList<CriteriaWeight> ChairCriteria()
    {
        return text.IsEnglish
            ? BuildCriteria(("Price", 80, "#7BE8E0"), ("Ergonomics / lumbar support", 95, "#5EE9A8"), ("Adjustability", 85, "#7BE8E0"), ("Materials", 70, "#B49CFF"), ("Size / supported weight", 72, "#E9D67B"), ("Daily comfort", 90, "#5EE9A8"), ("Assembly / delivery", 55, "#F0A36A"), ("Warranty / returns", 75, "#7BE8E0"))
            : BuildCriteria(("Preço", 80, "#7BE8E0"), ("Ergonomia / apoio lombar", 95, "#5EE9A8"), ("Ajustes", 85, "#7BE8E0"), ("Materiais", 70, "#B49CFF"), ("Dimensões / peso suportado", 72, "#E9D67B"), ("Conforto diário", 90, "#5EE9A8"), ("Montagem / entrega", 55, "#F0A36A"), ("Garantia / devolução", 75, "#7BE8E0"));
    }

    private IReadOnlyList<CriteriaWeight> KeyboardCriteria()
    {
        return text.IsEnglish
            ? BuildCriteria(("Price", 78, "#7BE8E0"), ("Layout / compatibility", 90, "#5EE9A8"), ("Switch type", 82, "#B49CFF"), ("Ergonomics", 75, "#7BE8E0"), ("Connectivity", 70, "#5EE9A8"), ("Noise", 45, "#F0A36A"), ("Build quality", 80, "#E9D67B"), ("Warranty / seller", 72, "#7BE8E0"))
            : BuildCriteria(("Preço", 78, "#7BE8E0"), ("Layout / compatibilidade", 90, "#5EE9A8"), ("Tipo de switch", 82, "#B49CFF"), ("Ergonomia", 75, "#7BE8E0"), ("Conectividade", 70, "#5EE9A8"), ("Ruído", 45, "#F0A36A"), ("Qualidade de construção", 80, "#E9D67B"), ("Garantia / vendedor", 72, "#7BE8E0"));
    }

    private IReadOnlyList<CriteriaWeight> MonitorCriteria()
    {
        return text.IsEnglish
            ? BuildCriteria(("Price", 78, "#7BE8E0"), ("Size / resolution", 92, "#5EE9A8"), ("Panel type", 84, "#B49CFF"), ("Refresh rate", 72, "#7BE8E0"), ("Ergonomics", 60, "#E9D67B"), ("Connectivity", 75, "#5EE9A8"), ("Consumption", 45, "#F0A36A"), ("Warranty / pixels", 80, "#7BE8E0"))
            : BuildCriteria(("Preço", 78, "#7BE8E0"), ("Tamanho / resolução", 92, "#5EE9A8"), ("Tipo de painel", 84, "#B49CFF"), ("Taxa de atualização", 72, "#7BE8E0"), ("Ergonomia", 60, "#E9D67B"), ("Conectividade", 75, "#5EE9A8"), ("Consumo", 45, "#F0A36A"), ("Garantia / pixels", 80, "#7BE8E0"));
    }

    private IReadOnlyList<CriteriaWeight> HeadphonesCriteria()
    {
        return text.IsEnglish
            ? BuildCriteria(("Price", 78, "#7BE8E0"), ("Sound quality", 92, "#5EE9A8"), ("ANC / isolation", 82, "#B49CFF"), ("Comfort", 88, "#7BE8E0"), ("Battery", 78, "#5EE9A8"), ("Microphone", 65, "#E9D67B"), ("Codec / connectivity", 72, "#F0A36A"), ("Warranty / seller", 70, "#7BE8E0"))
            : BuildCriteria(("Preço", 78, "#7BE8E0"), ("Qualidade de som", 92, "#5EE9A8"), ("ANC / isolamento", 82, "#B49CFF"), ("Conforto", 88, "#7BE8E0"), ("Bateria", 78, "#5EE9A8"), ("Microfone", 65, "#E9D67B"), ("Codec / conectividade", 72, "#F0A36A"), ("Garantia / vendedor", 70, "#7BE8E0"));
    }

    private IReadOnlyList<CriteriaWeight> GenericProductCriteria()
    {
        return text.IsEnglish
            ? BuildCriteria(("Price", 82, "#7BE8E0"), ("Warranty", 78, "#5EE9A8"), ("Authenticity", 88, "#B49CFF"), ("Reviews", 65, "#7BE8E0"), ("Delivery", 55, "#E9D67B"), ("Compatibility", 70, "#5EE9A8"), ("Durability", 74, "#F0A36A"), ("Seller risk", 90, "#7BE8E0"))
            : BuildCriteria(("Preço", 82, "#7BE8E0"), ("Garantia", 78, "#5EE9A8"), ("Autenticidade", 88, "#B49CFF"), ("Avaliações", 65, "#7BE8E0"), ("Entrega", 55, "#E9D67B"), ("Compatibilidade", 70, "#5EE9A8"), ("Durabilidade", 74, "#F0A36A"), ("Risco do vendedor", 90, "#7BE8E0"));
    }

    private static IReadOnlyList<CriteriaWeight> BuildCriteria(params (string Name, int Value, string Accent)[] criteria)
    {
        return criteria.Select(item => new CriteriaWeight(item.Name, item.Value, item.Accent)).ToList();
    }

    private static bool ContainsAnyTerm(string normalizedQuery, params string[] terms)
    {
        return terms.Any(term => ContainsCatalogTerm(normalizedQuery, NormalizeCatalogText(term)));
    }

    private static bool IsUnsupportedKnownCategory(string normalizedQuery)
    {
        return ContainsAnyTerm(normalizedQuery, "cadeira", "cadeiras", "chair", "chairs")
            || ContainsAnyTerm(normalizedQuery, "teclado", "teclados", "keyboard", "keyboards")
            || ContainsAnyTerm(normalizedQuery, "monitor", "monitores")
            || ContainsAnyTerm(normalizedQuery, "auscultadores", "auriculares", "headphones", "headset", "earbuds");
    }

    private static bool MatchesKnownProduct(string normalizedQuery, string rawValue, IReadOnlyList<ProductResult> products)
    {
        return products.Any(product =>
        {
            var normalizedName = NormalizeCatalogText(product.Name);
            return product.Slug.Equals(rawValue, StringComparison.OrdinalIgnoreCase)
                || normalizedQuery.Contains(normalizedName, StringComparison.OrdinalIgnoreCase)
                || (normalizedQuery.Length > 4 && normalizedName.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase));
        });
    }

    private static int ScoreCatalog(string normalizedQuery, IReadOnlyList<string> terms)
    {
        return terms.Count(term => ContainsCatalogTerm(normalizedQuery, term));
    }

    private static bool ContainsCatalogTerm(string normalizedQuery, string normalizedTerm)
    {
        if (string.IsNullOrWhiteSpace(normalizedTerm))
        {
            return false;
        }

        if (normalizedTerm.Any(character => !char.IsLetterOrDigit(character)))
        {
            return normalizedQuery.Contains(normalizedTerm, StringComparison.OrdinalIgnoreCase);
        }

        return GetCatalogTokens(normalizedQuery).Contains(normalizedTerm, StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> GetCatalogTokens(string normalizedQuery)
    {
        return normalizedQuery
            .Split([' ', '-', '/', ',', '.', ';', ':', '(', ')', '[', ']', '{', '}'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string NormalizeCatalogText(string value)
    {
        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private IReadOnlyList<ProductResult> LocalizeProducts(IReadOnlyList<ProductResult> products)
    {
        return text.IsEnglish ? products.Select(LocalizeProduct).ToList() : products;
    }

    private ProductResult LocalizeProduct(ProductResult product)
    {
        if (!text.IsEnglish)
        {
            return product;
        }

        return product with
        {
            Badge = TranslateProductCopy(product.Badge),
            Specs = product.Specs.Select(TranslateProductCopy).ToList(),
            Highlights = product.Highlights.Select(TranslateProductCopy).ToList(),
            VerificationChecks = product.VerificationChecks.Select(TranslateProductCopy).ToList(),
            FraudAlerts = product.FraudAlerts.Select(alert => alert with { Reason = TranslateProductCopy(alert.Reason) }).ToList(),
            AiSummary = TranslateProductCopy(product.AiSummary)
        };
    }

    private IReadOnlyList<SellerOffer> LocalizeOffers(IReadOnlyList<SellerOffer> offers)
    {
        return text.IsEnglish
            ? offers.Select(offer => offer with
            {
                Delivery = TranslateProductCopy(offer.Delivery),
                Warranty = TranslateProductCopy(offer.Warranty),
                Status = TranslateProductCopy(offer.Status)
            }).ToList()
            : offers;
    }

    private static string TranslateProductCopy(string value)
    {
        return value switch
        {
            "Recomendado" => "Recommended",
            "Melhor garantia" => "Best warranty",
            "Ecrã premium" => "Premium display",
            "Melhor preço" => "Best price",
            "Melhor Android" => "Best Android",
            "Melhor câmara IA" => "Best AI camera",
            "Melhor preço/performance" => "Best price/performance",
            "Mais eficiente" => "Most efficient",
            "Melhor tecnologia" => "Best technology",
            "Melhor durabilidade" => "Best durability",
            "Melhor preco" => "Best price",
            "18h autonomia" => "18h battery",
            "15h autonomia" => "15h battery",
            "12h autonomia" => "12h battery",
            "11h autonomia" => "11h battery",
            "23h vídeo" => "23h video",
            "Garantia 2 anos" => "2-year warranty",
            "Garantia 3 anos" => "3-year warranty",
            "Autonomia 38% acima da média" => "Battery life 38% above average",
            "Peso reduzido" => "Low weight",
            "Garantia oficial em Portugal" => "Official warranty in Portugal",
            "Mais leve da lista" => "Lightest on the list",
            "Garantia superior" => "Stronger warranty",
            "Bom equilíbrio profissional" => "Good professional balance",
            "Boa performance" => "Good performance",
            "Ecrã forte" => "Strong display",
            "Construção sólida" => "Solid build",
            "Preço competitivo" => "Competitive price",
            "Leve" => "Light",
            "Boa relação valor" => "Good value",
            "Melhor ecossistema" => "Best ecosystem",
            "Câmara consistente" => "Consistent camera",
            "Suporte longo" => "Long support",
            "Ecrã excelente" => "Excellent display",
            "Atualizações longas" => "Long update window",
            "Boa autonomia" => "Good battery",
            "Fotografia forte" => "Strong photography",
            "Android limpo" => "Clean Android",
            "Excelente valor" => "Excellent value",
            "Performance elevada" => "High performance",
            "Carregamento rápido" => "Fast charging",
            "Preço agressivo" => "Aggressive price",
            "Número de série validado" => "Serial number validated",
            "Consumo muito baixo" => "Very low consumption",
            "Silencioso" => "Quiet",
            "Boa capacidade familiar" => "Good family capacity",
            "Capacidade superior" => "Higher capacity",
            "Arrefecimento rápido" => "Fast cooling",
            "Arrefecimento rapido" => "Fast cooling",
            "Funcionalidades smart" => "Smart features",
            "Construção robusta" => "Robust build",
            "Construcao robusta" => "Robust build",
            "Dosagem automática" => "Automatic dosing",
            "Dosagem automatica" => "Automatic dosing",
            "Lavagem silenciosa" => "Quiet washing",
            "Baixo ruído" => "Low noise",
            "Baixo ruido" => "Low noise",
            "Programas inteligentes" => "Smart programs",
            "Numero de serie validado" => "Serial number validated",
            "Revendedor autorizado" => "Authorized reseller",
            "Stock real confirmado" => "Real stock confirmed",
            "Garantia oficial registável em PT" => "Official warranty registerable in Portugal",
            "Sem histórico de contrafação" => "No counterfeit history",
            "Garantia validada" => "Warranty validated",
            "Etiqueta energética confirmada" => "Energy label confirmed",
            "Etiqueta energetica confirmada" => "Energy label confirmed",
            "Stock cruzado em 3 fontes" => "Stock cross-checked across 3 sources",
            "Sem alertas críticos" => "No critical alerts",
            "Sem alertas criticos" => "No critical alerts",
            "Vendedor autorizado" => "Authorized seller",
            "Preço dentro do mercado" => "Market-aligned price",
            "IMEI validável" => "IMEI can be validated",
            "Preco dentro do mercado" => "Market-aligned price",
            "Garantia UE confirmada" => "EU warranty confirmed",
            "Preço 60% abaixo do mercado" => "Price 60% below market",
            "Domínio criado há 14 dias" => "Domain created 14 days ago",
            "Sem registo fiscal verificável" => "No verifiable tax registration",
            "Preço 45% abaixo do mercado" => "Price 45% below market",
            "Sem informação fiscal verificável" => "No verifiable tax information",
            "Preço 50% abaixo do mercado" => "Price 50% below market",
            "Preco 50% abaixo do mercado" => "Price 50% below market",
            "Sem registo fiscal verificavel" => "No verifiable tax registration",
            "2 dias" => "2 days",
            "3 dias" => "3 days",
            "4 dias" => "4 days",
            "5-7 dias" => "5-7 days",
            "4-7 dias" => "4-7 days",
            "2-4 dias" => "2-4 days",
            "2-3 dias" => "2-3 days",
            "Confirmar loja" => "Confirm store",
            "2 anos oficial" => "2-year official",
            "3 anos UE" => "3-year EU",
            "3 anos PT" => "3-year PT",
            "3 anos oficial" => "3-year official",
            "Validar vendedor" => "Validate seller",
            "Verificado" => "Verified",
            "Autorizado" => "Authorized",
            "A comparar" => "Comparing",
            "Este produto cumpre 94% dos critérios definidos. Pontos fortes: autonomia, peso reduzido e garantia oficial." => "This product meets 94% of the selected criteria. Strengths: battery life, low weight and official warranty.",
            "Excelente opção para suporte e mobilidade. Fica atrás do primeiro lugar pelo preço mais alto." => "Excellent option for support and mobility. It sits behind first place because of the higher price.",
            "Boa escolha se o ecrã for prioritário. Penalizado pelo peso e autonomia abaixo dos líderes." => "Good choice if display quality is the priority. Penalized by weight and battery life below the leaders.",
            "A melhor opção para orçamento controlado. Menos indicado para workloads pesados." => "The best option for a controlled budget. Less suited to heavy workloads.",
            "Melhor escolha premium para quem valoriza câmara, desempenho e suporte prolongado." => "Best premium choice for camera, performance and long-term support.",
            "A opção Android mais equilibrada para desempenho, ecrã e longevidade." => "The most balanced Android option for performance, display and longevity.",
            "Ideal para fotografia computacional e experiência Android limpa." => "Ideal for computational photography and a clean Android experience.",
            "Muito forte em performance e carregamento, com preço competitivo." => "Very strong on performance and charging, with a competitive price.",
            "Melhor escolha para frigorífico eficiente, silencioso e com garantia forte." => "Best choice for an efficient, quiet fridge with a strong warranty.",
            "Melhor escolha para frigorifico eficiente, silencioso e com garantia forte." => "Best choice for an efficient, quiet fridge with a strong warranty.",
            "Excelente para famílias que precisam de grande capacidade e controlo inteligente." => "Excellent for families that need high capacity and smart control.",
            "Excelente para familias que precisam de grande capacidade e controlo inteligente." => "Excellent for families that need high capacity and smart control.",
            "Boa escolha quando durabilidade e qualidade de lavagem pesam mais que preço inicial." => "Good choice when durability and wash quality matter more than initial price.",
            "Boa escolha quando durabilidade e qualidade de lavagem pesam mais que preco inicial." => "Good choice when durability and wash quality matter more than initial price.",
            "Opção equilibrada para cozinha moderna com bom preço e baixo ruído." => "Balanced option for a modern kitchen with good price and low noise.",
            "Opcao equilibrada para cozinha moderna com bom preco e baixo ruido." => "Balanced option for a modern kitchen with good price and low noise.",
            _ => value
        };
    }
}
