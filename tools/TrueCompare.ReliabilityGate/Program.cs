namespace TrueCompare.ReliabilityGate;

public static class ReliabilityGateCli
{
    public static async Task<int> Main(string[] args)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var cancellationToken = CancellationToken.None;

        var options = ParseOptions(args);
        Directory.CreateDirectory(options.OutputDirectory);

        var runWarnings = new List<string>();
        var sourceProjectDirectory = FindSourceProjectDirectory();
        var browserRunner = new BrowserAutomationRunner(sourceProjectDirectory);

        ReliabilityFixture? fixture = null;
        MarketCatalog catalog;

        if (options.Live && options.LiveKuantoKusta)
        {
            catalog = await browserRunner.TryCollectKuantoKustaAsync(options, options.OutputDirectory, cancellationToken)
                ?? new MarketCatalog(
                    Array.Empty<MarketCategory>(),
                    EvidenceStatus.Blocked,
                    "Coleta live do KuantoKusta não produziu catálogo auditável.",
                    DateTimeOffset.UtcNow);
        }
        else
        {
            fixture = await ReliabilityJson.ReadAsync<ReliabilityFixture>(ResolvePath(options.FixturePath), cancellationToken)
                ?? throw new InvalidOperationException($"Fixture inválida: {options.FixturePath}");
            catalog = fixture.Catalog;
            runWarnings.Add("Execução sem coleta live do KuantoKusta: foram usadas fixtures gravadas.");
        }

        if (!string.IsNullOrWhiteSpace(options.CategoryFilter))
        {
            catalog = catalog with
            {
                Categories = catalog.Categories
                    .Where(category => ReliabilityNormalizer.NormalizeName(category.Name).Contains(ReliabilityNormalizer.NormalizeName(options.CategoryFilter), StringComparison.OrdinalIgnoreCase))
                    .ToList()
            };
        }

        if (catalog.Status != EvidenceStatus.Validated)
        {
            runWarnings.Add($"KuantoKusta bloqueado/inconclusivo: {catalog.BlockReason ?? catalog.Status.ToString()}");
        }

        var cases = ReliabilityPromptGenerator.Generate(catalog, options.PromptsPerSubcategory, options.MaxSubcategories);
        var appProvider = new ApplicationRecommendationProvider();
        var scorer = new ReliabilityScorer();
        var results = new List<CaseComparisonResult>();

        foreach (var testCase in cases)
        {
            var chatGpt = await ResolveChatGptReferenceAsync(options, fixture, browserRunner, testCase, cancellationToken);
            var app = appProvider.Ask(testCase.UserPrompt);
            results.Add(scorer.Score(testCase, chatGpt, app));
        }

        var report = scorer.BuildReport(startedAt, DateTimeOffset.UtcNow, catalog, results, runWarnings);
        await new ReliabilityReportWriter().WriteAllAsync(report, options.OutputDirectory, cancellationToken);

        Console.WriteLine(report.VerdictText);
        Console.WriteLine($"Score agregado: {report.AggregateScore:0.##}%");
        Console.WriteLine($"Relatório HTML: {Path.GetFullPath(Path.Combine(options.OutputDirectory, "reliability-report.html"))}");
        Console.WriteLine($"Relatório JSON: {Path.GetFullPath(Path.Combine(options.OutputDirectory, "reliability-report.json"))}");

        return report.Verdict == ReliabilityVerdict.Falhou ? 2 : 0;
    }

    private static async Task<ChatGptReference> ResolveChatGptReferenceAsync(
        ReliabilityRunOptions options,
        ReliabilityFixture? fixture,
        BrowserAutomationRunner browserRunner,
        ReliabilityCase testCase,
        CancellationToken cancellationToken)
    {
        var prompt = ReliabilityPromptGenerator.BuildChatGptPrompt(testCase);
        var liveChatGptEnabled = options.Live && options.LiveChatGptBrowser;
        if (liveChatGptEnabled)
        {
            return await browserRunner.AskChatGptBrowserAsync(options, testCase, options.OutputDirectory, cancellationToken);
        }

        if (fixture?.ChatGptReferences.TryGetValue(testCase.Id, out var reference) == true)
        {
            return new ChatGptReference(
                EvidenceStatus.Validated,
                prompt,
                reference.ResponseText,
                reference.ModelUsed,
                reference.ReasoningMode,
                DateTimeOffset.UtcNow,
                null,
                null,
                reference.RecommendedProductNames);
        }

        return new ChatGptReference(
            EvidenceStatus.Inconclusive,
            prompt,
            string.Empty,
            null,
            null,
            DateTimeOffset.UtcNow,
            null,
            "RUN_LIVE_CHATGPT_BROWSER_VALIDATION não está ativo e não existe fixture ChatGPT para este caso.",
            Array.Empty<string>());
    }

    private static ReliabilityRunOptions ParseOptions(string[] args)
    {
        var map = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            if (!arg.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            var key = arg[2..];
            if (index + 1 < args.Length && !args[index + 1].StartsWith("--", StringComparison.Ordinal))
            {
                map[key] = args[++index];
            }
            else
            {
                map[key] = "true";
            }
        }

        var live = Bool(map, "live") || EnvBool("RUN_LIVE_RELIABILITY_GATE");
        var liveKuantoKusta = live && (Bool(map, "live-kuantokusta") || EnvBool("RUN_LIVE_KUANTOKUSTA"));
        var liveChatGpt = live && (Bool(map, "live-chatgpt") || EnvBool("RUN_LIVE_CHATGPT_BROWSER_VALIDATION") || EnvBool("CHATGPT_BROWSER_MODE"));

        return new ReliabilityRunOptions(
            live,
            liveKuantoKusta,
            liveChatGpt,
            Value(map, "category"),
            Int(map, "max-subcategories", live ? 0 : 3),
            Int(map, "max-products", 20),
            Int(map, "prompts-per-subcategory", live ? 5 : 1),
            Value(map, "output") ?? Path.Combine("artifacts", "reliability-gate", DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss")),
            Value(map, "fixture") ?? Path.Combine("tools", "TrueCompare.ReliabilityGate", "Fixtures", "kuantokusta-sample-fixture.json"),
            Value(map, "chatgpt-model") ?? "GPT-5.5",
            Value(map, "chatgpt-reasoning") ?? "Alta",
            Value(map, "chrome-cdp") ?? Environment.GetEnvironmentVariable("CHATGPT_BROWSER_CDP"),
            Value(map, "chrome-user-data-dir") ?? Environment.GetEnvironmentVariable("CHATGPT_CHROME_USER_DATA_DIR"),
            Value(map, "app-base-url") ?? "http://localhost:5190");
    }

    private static string FindSourceProjectDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "tools", "TrueCompare.ReliabilityGate", "Browser");
            if (Directory.Exists(candidate))
            {
                return Path.Combine(current.FullName, "tools", "TrueCompare.ReliabilityGate");
            }

            current = current.Parent;
        }

        return Path.Combine(Directory.GetCurrentDirectory(), "tools", "TrueCompare.ReliabilityGate");
    }

    private static string ResolvePath(string path)
    {
        if (Path.IsPathRooted(path))
        {
            return path;
        }

        var cwdPath = Path.GetFullPath(path);
        if (File.Exists(cwdPath))
        {
            return cwdPath;
        }

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
    }

    private static string? Value(IReadOnlyDictionary<string, string?> map, string key)
    {
        return map.TryGetValue(key, out var value) ? value : null;
    }

    private static bool Bool(IReadOnlyDictionary<string, string?> map, string key)
    {
        return map.TryGetValue(key, out var value) && bool.TryParse(value, out var parsed) && parsed;
    }

    private static bool EnvBool(string key)
    {
        return bool.TryParse(Environment.GetEnvironmentVariable(key), out var parsed) && parsed;
    }

    private static int Int(IReadOnlyDictionary<string, string?> map, string key, int fallback)
    {
        return map.TryGetValue(key, out var value) && int.TryParse(value, out var parsed) ? parsed : fallback;
    }
}
