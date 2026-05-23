namespace TrueCompare.ReliabilityGate;

public static class ReliabilityPromptGenerator
{
    public static IReadOnlyList<ReliabilityCase> Generate(
        MarketCatalog catalog,
        int promptsPerSubcategory,
        int maxSubcategories)
    {
        var cases = new List<ReliabilityCase>();
        var subcategories = catalog.Categories
            .SelectMany(category => category.Subcategories)
            .Where(subcategory => subcategory.Status == EvidenceStatus.Validated && subcategory.Products.Count > 0)
            .Take(maxSubcategories <= 0 ? int.MaxValue : maxSubcategories)
            .ToList();

        foreach (var subcategory in subcategories)
        {
            foreach (var prompt in BuildPrompts(subcategory.Name).Take(Math.Max(1, promptsPerSubcategory)))
            {
                var id = $"{Slug(subcategory.Category)}-{Slug(subcategory.Name)}-{cases.Count + 1:000}";
                cases.Add(new ReliabilityCase(
                    id,
                    subcategory.Category,
                    subcategory.Name,
                    subcategory.Url,
                    prompt,
                    subcategory.Products));
            }
        }

        return cases;
    }

    public static string BuildChatGptPrompt(ReliabilityCase testCase)
    {
        var products = string.Join(
            Environment.NewLine,
            testCase.Products.Select((product, index) =>
                $"{index + 1}. Nome: {product.Name}; Marca: {product.Brand}; Modelo: {product.Model}; Preco minimo: {ReliabilityNormalizer.FormatCurrency(product.MinPriceCents)}; Preco maximo: {(product.MaxPriceCents.HasValue ? ReliabilityNormalizer.FormatCurrency(product.MaxPriceCents.Value) : "n/d")}; Lojas: {string.Join(", ", product.Stores)}; Badges: {string.Join(", ", product.Badges)}; Rating: {product.Rating ?? "n/d"}; Disponibilidade: {product.Availability ?? "n/d"}; URL: {product.Url}"));

        return $$"""
        Estou a validar uma aplicação de recomendação de produtos.

        Subcategoria: {{testCase.Subcategory}}
        Pergunta do utilizador: {{testCase.UserPrompt}}

        O teu universo de escolha é apenas a lista de produtos fornecida. Não podes recomendar produtos fora desta lista. Se os dados forem insuficientes, responde que não é possível validar.

        Produtos reais recolhidos do KuantoKusta:
        {{products}}

        Responde em JSON válido, sem markdown, com esta estrutura:
        {
          "status": "validado|inconclusivo",
          "mais_barata_aceitavel": "nome exato ou null",
          "melhor_custo_beneficio": "nome exato ou null",
          "intermedia": "nome exato ou null",
          "topo": "nome exato ou null",
          "evitar": "nome exato ou null",
          "criterios": ["criterio 1", "criterio 2"],
          "justificacao_curta": "texto curto",
          "dados_insuficientes": "motivo ou null"
        }
        """;
    }

    private static IEnumerable<string> BuildPrompts(string subcategory)
    {
        yield return $"Qual é a melhor opção custo/benefício em {subcategory}?";
        yield return $"Quero comprar {subcategory}, dá-me uma opção barata, intermédia e topo.";
        yield return $"Qual {subcategory} devo comprar para uma família?";
        yield return $"Qual {subcategory} é melhor para uso intensivo?";
        yield return $"Quero o produto mais barato que ainda valha a pena em {subcategory}.";
    }

    private static string Slug(string value)
    {
        var tokenSlug = string.Join('-', ReliabilityNormalizer.Tokenize(value)).ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(tokenSlug))
        {
            return tokenSlug;
        }

        var normalized = ReliabilityNormalizer.NormalizeName(value);
        return string.IsNullOrWhiteSpace(normalized)
            ? "geral"
            : normalized.Replace(' ', '-');
    }
}
