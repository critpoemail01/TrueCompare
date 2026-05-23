using TrueCompare.ReliabilityGate;

namespace TrueCompare.Tests.ReliabilityGate;

public sealed class ReliabilityGateTests
{
    [Theory]
    [InlineData("1 279,00 €", 127900)]
    [InlineData("1.279,00 €", 127900)]
    [InlineData("1.249 €", 124900)]
    [InlineData("499,99 EUR", 49999)]
    [InlineData("desde 366,90€", 36690)]
    public void Price_normalizer_parses_portuguese_prices(string value, long expectedCents)
    {
        Assert.Equal(expectedCents, ReliabilityNormalizer.ParsePriceCents(value));
    }

    [Fact]
    public void Name_similarity_matches_equivalent_models()
    {
        var score = ReliabilityNormalizer.SimilarityScore(
            "Samsung RB34C600ESA A Frigorifico Combinado",
            "Frigorifico combinado Samsung RB34C600ESA");

        Assert.True(score >= 55);
    }

    [Fact]
    public async Task Fixture_generates_stable_cases_for_ci()
    {
        var fixture = await LoadFixtureAsync();
        var cases = ReliabilityPromptGenerator.Generate(fixture.Catalog, promptsPerSubcategory: 1, maxSubcategories: 3);

        Assert.Equal(3, cases.Count);
        Assert.Contains(cases, testCase => testCase.Id == "eletrodomesticos-frigorificos-001");
        Assert.Contains(cases, testCase => testCase.Id == "eletrodomesticos-secar-roupa-002");
        Assert.Contains(cases, testCase => testCase.Id == "informatica-monitores-003");
    }

    [Fact]
    public async Task Scorer_accepts_market_backed_equivalent_recommendation()
    {
        var fixture = await LoadFixtureAsync();
        var testCase = ReliabilityPromptGenerator.Generate(fixture.Catalog, promptsPerSubcategory: 1, maxSubcategories: 1).Single();
        var chatGpt = new ChatGptReference(
            EvidenceStatus.Validated,
            "prompt",
            "fixture",
            "Fixture ChatGPT Pro",
            "Alta fixture",
            DateTimeOffset.UtcNow,
            null,
            null,
            fixture.ChatGptReferences[testCase.Id].RecommendedProductNames);
        var app = new AppRecommendation(
            EvidenceStatus.Validated,
            testCase.UserPrompt,
            "A Samsung é a melhor opção custo/benefício.",
            DateTimeOffset.UtcNow,
            new[]
            {
                new AppRecommendedProduct(
                    "Samsung RB34C600ESA A Frigorifico Combinado",
                    "Samsung",
                    "499,99 EUR",
                    49999,
                    "Preço/qualidade",
                    "Modelo existente no KuantoKusta, preço compatível e classe A.")
            });

        var result = new ReliabilityScorer().Score(testCase, chatGpt, app);

        Assert.False(result.CriticalFailure);
        Assert.True(result.Score >= 85);
    }

    [Fact]
    public async Task Scorer_marks_invented_or_wrong_category_product_as_critical()
    {
        var fixture = await LoadFixtureAsync();
        var testCase = ReliabilityPromptGenerator.Generate(fixture.Catalog, promptsPerSubcategory: 1, maxSubcategories: 1).Single();
        var chatGpt = new ChatGptReference(
            EvidenceStatus.Validated,
            "prompt",
            "fixture",
            "Fixture ChatGPT Pro",
            "Alta fixture",
            DateTimeOffset.UtcNow,
            null,
            null,
            fixture.ChatGptReferences[testCase.Id].RecommendedProductNames);
        var app = new AppRecommendation(
            EvidenceStatus.Validated,
            testCase.UserPrompt,
            "Recomendo uma consola.",
            DateTimeOffset.UtcNow,
            new[]
            {
                new AppRecommendedProduct(
                    "PlayStation 5 Slim",
                    "Sony",
                    "649,99 €",
                    64999,
                    "Errado",
                    "Sem evidência para frigoríficos.")
            });

        var result = new ReliabilityScorer().Score(testCase, chatGpt, app);

        Assert.True(result.CriticalFailure);
        Assert.Contains(result.CriticalFailures, failure => failure.Contains("Produto inventado", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Report_writer_emits_required_final_verdict_section()
    {
        var fixture = await LoadFixtureAsync();
        var report = new ReliabilityReport(
            ReliabilityVerdict.Inconclusivo,
            "INCONCLUSIVO: validação incompleta por bloqueios externos, páginas não acessíveis ou dados insuficientes",
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow,
            fixture.Catalog.Categories.Count,
            fixture.Catalog.Categories.SelectMany(category => category.Subcategories).Count(),
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            new Dictionary<string, double>(),
            Array.Empty<CaseComparisonResult>(),
            Array.Empty<MarketSubcategory>(),
            new[] { "Fixture de teste." });
        var output = Path.Combine(Path.GetTempPath(), "truecompare-reliability-tests", Guid.NewGuid().ToString("N"));

        await new ReliabilityReportWriter().WriteAllAsync(report, output, CancellationToken.None);

        var html = await File.ReadAllTextAsync(Path.Combine(output, "reliability-report.html"));
        Assert.Contains("Veredito final de fiabilidade", html);
        Assert.Contains("INCONCLUSIVO", html);
    }

    [Fact]
    public void Application_provider_returns_products_for_known_market_prompt()
    {
        var app = new ApplicationRecommendationProvider().Ask("Qual é a melhor opção custo/benefício em Frigoríficos?");

        Assert.Equal(EvidenceStatus.Validated, app.Status);
        Assert.NotEmpty(app.Products);
        Assert.Contains(app.Products, product => product.Name.Contains("Frigorifico", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Application_provider_returns_monitors_for_monitor_prompt()
    {
        var app = new ApplicationRecommendationProvider().Ask("Qual é a melhor opção custo/benefício em Monitores?");

        Assert.Equal(EvidenceStatus.Validated, app.Status);
        Assert.NotEmpty(app.Products);
        Assert.Contains(app.Products, product => product.Name.Contains("LG UltraGear", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<ReliabilityFixture> LoadFixtureAsync()
    {
        var path = Path.Combine(FindRepositoryRoot(), "tools", "TrueCompare.ReliabilityGate", "Fixtures", "kuantokusta-sample-fixture.json");
        return await ReliabilityJson.ReadAsync<ReliabilityFixture>(path, CancellationToken.None)
            ?? throw new InvalidOperationException("Fixture do reliability gate inválida.");
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "TrueCompare.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Não foi encontrada a raiz do repositório TrueCompare.");
    }
}
