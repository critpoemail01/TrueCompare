namespace TrueCompare.ReliabilityGate;

public sealed class ReliabilityScorer
{
    public CaseComparisonResult Score(ReliabilityCase testCase, ChatGptReference chatGpt, AppRecommendation app)
    {
        var warnings = new List<string>();
        var criticalFailures = new List<string>();

        if (chatGpt.Status is EvidenceStatus.Blocked or EvidenceStatus.Inconclusive)
        {
            warnings.Add($"Referência ChatGPT inconclusiva: {chatGpt.BlockReason ?? "sem resposta validável"}");
        }

        if (app.Status is EvidenceStatus.Blocked or EvidenceStatus.Inconclusive)
        {
            criticalFailures.Add($"Aplicação sem resposta validável: {app.BlockReason ?? "sem produtos recomendados"}");
        }

        if (app.Products.Count == 0)
        {
            criticalFailures.Add("A aplicação não devolveu produtos recomendados para uma subcategoria com dados de mercado.");
        }

        var productMatches = app.Products
            .Take(4)
            .Select(product => MatchProduct(product, testCase.Products, chatGpt.RecommendedProductNames))
            .ToList();

        foreach (var match in productMatches)
        {
            if (!match.ExistsInMarket)
            {
                criticalFailures.Add($"Produto inventado ou sem evidência no KuantoKusta: {match.AppProduct}");
            }

            if (!match.CategoryCorrect)
            {
                criticalFailures.Add($"Produto fora da subcategoria {testCase.Subcategory}: {match.AppProduct}");
            }

            if (match.ExistsInMarket && !match.PriceCompatible)
            {
                criticalFailures.Add($"Preço gravemente desalinhado para {match.AppProduct}: app {ReliabilityNormalizer.FormatCurrency(match.AppPriceCents ?? 0)} vs mercado {ReliabilityNormalizer.FormatCurrency(match.MarketPriceCents ?? 0)}");
            }
        }

        var existenceScore = productMatches.Count == 0 ? 0 : productMatches.Count(match => match.ExistsInMarket) * 100 / productMatches.Count;
        var categoryScore = productMatches.Count == 0 ? 0 : productMatches.Count(match => match.CategoryCorrect) * 100 / productMatches.Count;
        var priceScore = productMatches.Count == 0 ? 0 : productMatches.Sum(match => match.PriceScore) / productMatches.Count;
        var chatGptScore = chatGpt.Status == EvidenceStatus.Validated
            ? ScoreAgainstChatGpt(productMatches, chatGpt.RecommendedProductNames)
            : 0;
        var evidenceScore = app.Products.Count > 0 && app.Products.All(product => !string.IsNullOrWhiteSpace(product.Justification)) ? 100 : 50;

        var score = (int)Math.Round(
            existenceScore * 0.25
            + categoryScore * 0.20
            + priceScore * 0.20
            + chatGptScore * 0.20
            + evidenceScore * 0.15,
            MidpointRounding.AwayFromZero);

        var status = criticalFailures.Count > 0
            ? EvidenceStatus.Validated
            : chatGpt.Status == EvidenceStatus.Validated && app.Status == EvidenceStatus.Validated
                ? EvidenceStatus.Validated
                : EvidenceStatus.Inconclusive;

        return new CaseComparisonResult(
            testCase.Id,
            testCase.Category,
            testCase.Subcategory,
            testCase.UserPrompt,
            status,
            score,
            criticalFailures.Count > 0,
            criticalFailures,
            warnings,
            productMatches,
            chatGpt,
            app,
            testCase.Products);
    }

    public ReliabilityReport BuildReport(
        DateTimeOffset startedAt,
        DateTimeOffset finishedAt,
        MarketCatalog catalog,
        IReadOnlyList<CaseComparisonResult> cases,
        IReadOnlyList<string> runWarnings)
    {
        var allSubcategories = catalog.Categories.SelectMany(category => category.Subcategories).ToList();
        var notValidable = allSubcategories
            .Where(subcategory => subcategory.Status != EvidenceStatus.Validated)
            .ToList();

        var categoryScores = cases
            .GroupBy(testCase => testCase.Category)
            .ToDictionary(group => group.Key, group => group.Average(testCase => testCase.Score), StringComparer.OrdinalIgnoreCase);

        var aggregateScore = cases.Count == 0 ? 0 : cases.Average(testCase => testCase.Score);
        var criticalCount = cases.Sum(testCase => testCase.CriticalFailures.Count);
        var blockedCases = cases.Count(testCase => testCase.ChatGpt.Status == EvidenceStatus.Blocked || testCase.App.Status == EvidenceStatus.Blocked);
        var inconclusiveCases = cases.Count(testCase => testCase.Status == EvidenceStatus.Inconclusive || testCase.ChatGpt.Status == EvidenceStatus.Inconclusive);
        var minCategoryScore = categoryScores.Count == 0 ? 0 : categoryScores.Min(item => item.Value);

        var verdict = criticalCount > 0
            ? ReliabilityVerdict.Falhou
            : blockedCases > 0 || inconclusiveCases > 0 || notValidable.Count > 0 || cases.Count == 0
                ? ReliabilityVerdict.Inconclusivo
                : aggregateScore >= 85 && minCategoryScore >= 75
                    ? ReliabilityVerdict.Passou
                    : ReliabilityVerdict.Falhou;

        var verdictText = verdict switch
        {
            ReliabilityVerdict.Passou => "PASSOU: aplicação validada no teste final",
            ReliabilityVerdict.Falhou => "FALHOU: aplicação não validada",
            _ => "INCONCLUSIVO: validação incompleta por bloqueios externos, páginas não acessíveis ou dados insuficientes"
        };

        return new ReliabilityReport(
            verdict,
            verdictText,
            startedAt,
            finishedAt,
            catalog.Categories.Count,
            allSubcategories.Count,
            allSubcategories.Count - notValidable.Count,
            notValidable.Count,
            cases.Count,
            blockedCases,
            inconclusiveCases,
            criticalCount,
            Math.Round(aggregateScore, 2),
            categoryScores.ToDictionary(item => item.Key, item => Math.Round(item.Value, 2), StringComparer.OrdinalIgnoreCase),
            cases,
            notValidable,
            runWarnings);
    }

    private static ProductMatchResult MatchProduct(
        AppRecommendedProduct appProduct,
        IReadOnlyList<MarketProduct> marketProducts,
        IReadOnlyList<string> chatGptProductNames)
    {
        var bestMarket = marketProducts
            .Select(product => new
            {
                Product = product,
                Score = ReliabilityNormalizer.SimilarityScore(appProduct.Name, product.Name)
            })
            .OrderByDescending(item => item.Score)
            .FirstOrDefault();

        var exists = bestMarket is not null && bestMarket.Score >= 55;
        long? marketPrice = exists ? bestMarket!.Product.MinPriceCents : null;
        var priceCompatible = exists && marketPrice.HasValue && ReliabilityNormalizer.PricesCompatible(appProduct.PriceCents, marketPrice.Value);
        var priceScore = !exists || marketPrice is null
            ? 0
            : priceCompatible
                ? 100
                : ReliabilityNormalizer.PricesCompatible(appProduct.PriceCents, marketPrice.Value, 0.35m)
                    ? 60
                    : 0;

        var bestChatGpt = chatGptProductNames
            .Select(name => new { Name = name, Score = ReliabilityNormalizer.SimilarityScore(appProduct.Name, name) })
            .OrderByDescending(item => item.Score)
            .FirstOrDefault();

        return new ProductMatchResult(
            appProduct.Name,
            exists ? bestMarket!.Product.Name : null,
            bestChatGpt is not null && bestChatGpt.Score >= 55 ? bestChatGpt.Name : null,
            bestMarket?.Score ?? 0,
            priceScore,
            appProduct.PriceCents > 0 ? appProduct.PriceCents : null,
            marketPrice,
            exists,
            exists,
            priceCompatible);
    }

    private static int ScoreAgainstChatGpt(IReadOnlyList<ProductMatchResult> productMatches, IReadOnlyList<string> chatGptProductNames)
    {
        if (chatGptProductNames.Count == 0 || productMatches.Count == 0)
        {
            return 0;
        }

        if (productMatches.Any(match => match.MatchedChatGptProduct is not null))
        {
            return 100;
        }

        var averageNameSimilarity = productMatches
            .SelectMany(match => chatGptProductNames.Select(name => ReliabilityNormalizer.SimilarityScore(match.AppProduct, name)))
            .DefaultIfEmpty(0)
            .Max();

        return averageNameSimilarity >= 45 ? 75 : 40;
    }
}
