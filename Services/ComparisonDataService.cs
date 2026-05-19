using TrueCompare.Models;

namespace TrueCompare.Services;

public sealed class ComparisonDataService
{
    private const string LaptopCatalog = "laptops";
    private const string SmartphoneCatalog = "smartphones";

    public IReadOnlyList<string> Categories { get; } = new List<string>
    {
        "Smartphones premium",
        "Portáteis profissionais",
        "Eletrodomésticos eficiência A+++",
        "Ferramentas industriais",
        "Equipamento desportivo",
        "Acessórios automóvel",
        "Móveis & decoração",
        "Áudio & DJ equipment"
    };

    public IReadOnlyList<CriteriaWeight> DefaultWeights { get; } = new List<CriteriaWeight>
    {
        new("Preço", 80, "#7BE8E0"),
        new("Autonomia", 95, "#5EE9A8"),
        new("Peso e portabilidade", 70, "#7BE8E0"),
        new("Performance (CPU)", 60, "#7BE8E0"),
        new("Qualidade do ecrã", 50, "#B49CFF"),
        new("Garantia e suporte", 75, "#5EE9A8"),
        new("Sustentabilidade", 40, "#E9D67B"),
        new("Origem / fabrico", 30, "#F0A36A")
    };

    public IReadOnlyList<ProductResult> Products => LaptopProducts;

    public IReadOnlyList<SellerOffer> SellerOffers => LaptopOffers;

    public IReadOnlyList<FeatureItem> Features { get; } = new List<FeatureItem>
    {
        new("01", "Prompt inteligente", "Linguagem natural. Zero filtros.", "#7BE8E0"),
        new("02", "Pesos personalizados", "Preço, autonomia, garantia.", "#5EE9A8"),
        new("03", "Comparação isenta", "Zero patrocínios.", "#B49CFF"),
        new("04", "Anti-fraude", "Listagens, vendedores, série.", "#E97B7B"),
        new("05", "Checkout seguro", "Cartão, garantia, devolução.", "#E9D67B"),
        new("06", "Tutoriais em vídeo", "Fluxos em 2 minutos.", "#F0A36A")
    };

    public IReadOnlyList<TutorialItem> Tutorials { get; } = new List<TutorialItem>
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

    public IReadOnlyList<ProductResult> GetProducts(string? query)
    {
        return ResolveCatalog(query) == SmartphoneCatalog ? SmartphoneProducts : LaptopProducts;
    }

    public IReadOnlyList<SellerOffer> GetSellerOffers(string? queryOrSlug)
    {
        return ResolveCatalog(queryOrSlug) == SmartphoneCatalog ? SmartphoneOffers : LaptopOffers;
    }

    public ProductResult? FindProduct(string slug)
    {
        return LaptopProducts.Concat(SmartphoneProducts)
            .FirstOrDefault(product => product.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
    }

    public SellerOffer GetBestOffer(string? queryOrSlug = null)
    {
        return GetSellerOffers(queryOrSlug).OrderBy(offer => offer.PriceCents).First();
    }

    private static string ResolveCatalog(string? queryOrSlug)
    {
        if (string.IsNullOrWhiteSpace(queryOrSlug))
        {
            return LaptopCatalog;
        }

        var value = queryOrSlug.Trim().ToLowerInvariant();
        if (SmartphoneProducts.Any(product => product.Slug.Equals(value, StringComparison.OrdinalIgnoreCase))
            || value.Contains("smartphone")
            || value.Contains("telemóvel")
            || value.Contains("telemovel")
            || value.Contains("iphone")
            || value.Contains("samsung")
            || value.Contains("android")
            || value.Contains("pixel")
            || value.Contains("xiaomi")
            || value.Contains("celular"))
        {
            return SmartphoneCatalog;
        }

        return LaptopCatalog;
    }
}
