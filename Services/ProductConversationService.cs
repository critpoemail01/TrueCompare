using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using TrueCompare.Models;

namespace TrueCompare.Services;

public sealed class ProductConversationService(ComparisonDataService data, AppText text)
{
    private static readonly string[] BroadProductCategoryTerms =
    [
        "smartphone",
        "smartphones",
        "telemovel",
        "telemoveis",
        "smartwatch",
        "smartwatches",
        "carregador",
        "carregadores",
        "cabo",
        "cabos",
        "portatil",
        "portateis",
        "computador",
        "computadores",
        "tablet",
        "tablets",
        "rato",
        "ratos",
        "periferico",
        "perifericos",
        "disco externo",
        "armazenamento",
        "monitor",
        "monitores",
        "impressora",
        "impressoras",
        "tv",
        "televisor",
        "televisores",
        "auscultadores",
        "gaming",
        "consola",
        "consolas",
        "brinquedo",
        "brinquedos",
        "drone",
        "drones",
        "eletrodomestico",
        "eletrodomesticos",
        "electrodomestico",
        "electrodomesticos",
        "maquina de lavar",
        "maquinas de lavar",
        "lavar roupa",
        "lavar loica",
        "lavar louca",
        "lavar loiça",
        "maquina de secar",
        "maquinas de secar",
        "frigorifico",
        "frigorificos",
        "fogao",
        "fogoes",
        "cooker",
        "stove",
        "microondas",
        "micro-ondas",
        "robot de cozinha",
        "preparacao de alimentos",
        "aspirador",
        "aspiradores",
        "perfume",
        "perfumaria",
        "racao",
        "animais",
        "fraldas",
        "bebe",
        "berbequim",
        "bricolage",
        "cadeira",
        "cadeiras",
        "sofa",
        "sofas",
        "jardim",
        "bicicleta",
        "trotinete",
        "fitness",
        "sapatilhas",
        "pneu",
        "pneus",
        "oleo",
        "tinteiro",
        "tinteiros",
        "livro",
        "livros",
        "vinho",
        "vinhos",
        "cafeteira",
        "cafeteiras"
    ];

    private static readonly HashSet<string> GenericProductIdentityTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "a",
        "as",
        "ate",
        "best",
        "boa",
        "bom",
        "barata",
        "barato",
        "cabos",
        "cadeira",
        "cadeiras",
        "cafe",
        "carregador",
        "carregadores",
        "classe",
        "com",
        "comprar",
        "conjuntos",
        "de",
        "da",
        "das",
        "do",
        "dos",
        "e",
        "em",
        "escritorio",
        "externo",
        "fitness",
        "frigorifico",
        "frigorificos",
        "fogao",
        "fogoes",
        "gaming",
        "gb",
        "kg",
        "l",
        "lavar",
        "loica",
        "loiça",
        "louca",
        "maquina",
        "maquinas",
        "melhor",
        "microondas",
        "monitor",
        "monitores",
        "para",
        "passadeira",
        "passadeiras",
        "perifericos",
        "portatil",
        "portateis",
        "preco",
        "premium",
        "produto",
        "rato",
        "ratos",
        "roupa",
        "secar",
        "sem",
        "smartphone",
        "smartphones",
        "tablet",
        "tablets",
        "tv",
        "usb",
        "vendedores",
        "verificados",
        "wireless"
    };

    public ProductConversationDecision Evaluate(string? userMessage)
    {
        var query = NormalizeUserMessage(userMessage);
        if (string.IsNullOrWhiteSpace(query))
        {
            return ProductConversationDecision.NeedsClarification(
                string.Empty,
                text["Home.ChatEmpty"]);
        }

        var products = data.GetProducts(query);
        if (products.Count == 0)
        {
            return ProductConversationDecision.NeedsClarification(
                query,
                text.Format("Home.ChatNeedsValidProduct", query));
        }

        var productsWithConfirmedOffers = products
            .Where(product => data
                .BuildSellerOffersForProduct(product, query)
                .Any(ComparisonDataService.IsConfirmedStoreOffer))
            .ToList();

        if (LooksLikeCoffeeMachineAdviceRequest(query, products))
        {
            var coffeeOptions = BuildCoffeeMachineAdviceOptions();
            if (coffeeOptions.Count > 0)
            {
                return ProductConversationDecision.ProductAdvice(
                    query,
                    BuildCoffeeMachineRecommendationMessage(coffeeOptions),
                    coffeeOptions
                        .Select(option => ToConversationOption(option.Product, true))
                        .ToList());
            }
        }

        if (LooksLikeFridgeAdviceRequest(query, products))
        {
            var fridgeOptions = BuildFridgeAdviceOptions();
            if (fridgeOptions.Count > 0)
            {
                return ProductConversationDecision.ProductAdvice(
                    query,
                    BuildFridgeRecommendationMessage(fridgeOptions),
                    fridgeOptions
                        .Select(option => ToConversationOption(option.Product, true))
                        .ToList());
            }
        }

        if (productsWithConfirmedOffers.Count == 0
            || LooksLikeProductAdviceQuestion(query)
            || LooksLikeBroadCategoryRequest(query, products))
        {
            var confirmedProductSlugs = productsWithConfirmedOffers
                .Select(product => product.Slug)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var candidateProducts = productsWithConfirmedOffers.Count > 0
                ? productsWithConfirmedOffers
                : products;
            var recommendationProducts = candidateProducts
                .OrderByDescending(product => confirmedProductSlugs.Contains(product.Slug))
                .ThenBy(product => product.Rank)
                .Take(4)
                .ToList();

            return ProductConversationDecision.ProductAdvice(
                query,
                BuildRecommendationMessage(query, recommendationProducts, productsWithConfirmedOffers.Count > 0),
                recommendationProducts
                    .Select(product => ToConversationOption(product, confirmedProductSlugs.Contains(product.Slug)))
                    .ToList());
        }

        var selectedProduct = productsWithConfirmedOffers[0];
        return ProductConversationDecision.Ready(
            query,
            selectedProduct.Name,
            text.Format("Home.ChatProductReady", selectedProduct.Name));
    }

    private static string NormalizeUserMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return string.Empty;
        }

        return Regex.Replace(message.Trim(), @"\s+", " ");
    }

    private static bool LooksLikeProductAdviceQuestion(string message)
    {
        var normalized = NormalizeForMatching(message);
        return message.Contains('?', StringComparison.Ordinal)
            || normalized.Contains("qual ", StringComparison.Ordinal)
            || normalized.Contains("quais ", StringComparison.Ordinal)
            || normalized.Contains("which ", StringComparison.Ordinal)
            || normalized.Contains("what ", StringComparison.Ordinal)
            || normalized.Contains("recomendas", StringComparison.Ordinal)
            || normalized.Contains("aconselhas", StringComparison.Ordinal)
            || normalized.Contains("recommend", StringComparison.Ordinal)
            || normalized.Contains("diferenca", StringComparison.Ordinal)
            || normalized.Contains("difference", StringComparison.Ordinal)
            || normalized.Contains("duvida", StringComparison.Ordinal)
            || normalized.Contains("nao sei", StringComparison.Ordinal)
            || normalized.Contains("n sei", StringComparison.Ordinal)
            || normalized.Contains("ajuda", StringComparison.Ordinal);
    }

    private static bool LooksLikeCoffeeMachineAdviceRequest(string query, IReadOnlyList<ProductResult> products)
    {
        var normalized = NormalizeForMatching(query);
        if (!ContainsAny(
            normalized,
            "maquina de cafe",
            "maquinas de cafe",
            "maquina cafe",
            "maquinas cafe",
            "coffee machine",
            "coffee maker",
            "cafeteira",
            "cafeteiras",
            "espresso"))
        {
            return false;
        }

        var asksForCoffeeOptions = LooksLikeProductAdviceQuestion(query)
            || ContainsAny(normalized, "opcoes", "opcao", "custo beneficio", "barato", "barata", "intermedio", "intermedia", "topo", "url", "comprar");
        if (ContainsAny(normalized, "vendedores verificados", "baixo risco") && !asksForCoffeeOptions)
        {
            return false;
        }

        return !MentionsSpecificProductName(query, products.Select(product => product.Name))
            || asksForCoffeeOptions;
    }

    private static bool LooksLikeFridgeAdviceRequest(string query, IReadOnlyList<ProductResult> products)
    {
        var normalized = NormalizeForMatching(query);
        if (!ContainsAny(
            normalized,
            "frigorifico",
            "frigorificos",
            "fridge",
            "geladeira",
            "combinado"))
        {
            return false;
        }

        var asksForFridgeOptions = LooksLikeProductAdviceQuestion(query)
            || ContainsAny(normalized, "opcoes", "opcao", "preco qualidade", "custo beneficio", "barato", "barata", "intermedio", "intermedia", "topo", "url", "urls", "comprar");

        return !MentionsSpecificProductName(query, products.Select(product => product.Name))
            || asksForFridgeOptions;
    }

    private bool LooksLikeBroadCategoryRequest(string query, IReadOnlyList<ProductResult> products)
    {
        if (products.Count == 0)
        {
            return false;
        }

        var normalized = NormalizeForMatching(query);
        if (data.Categories.Any(category => LooksLikeCategoryPrompt(normalized, NormalizeForMatching(category))))
        {
            return true;
        }

        if (MentionsSpecificProductIdentity(normalized, products))
        {
            return false;
        }

        return ContainsAny(normalized, BroadProductCategoryTerms)
            || ContainsAny(
                normalized,
                "baixo risco",
                "best value",
                "custo beneficio",
                "intermedio",
                "intermedia",
                "mais barato",
                "mais barata",
                "melhor preco",
                "preco qualidade",
                "topo",
                "vendedores verificados");
    }

    private static bool LooksLikeCategoryPrompt(string normalizedQuery, string normalizedCategory)
    {
        return string.Equals(normalizedQuery, normalizedCategory, StringComparison.Ordinal)
            || normalizedQuery.StartsWith(normalizedCategory + ":", StringComparison.Ordinal)
            || normalizedQuery.StartsWith(normalizedCategory + " ", StringComparison.Ordinal);
    }

    private static bool MentionsSpecificProductIdentity(string normalizedQuery, IEnumerable<ProductResult> products)
    {
        var queryTokens = ExtractTokens(normalizedQuery).ToHashSet(StringComparer.Ordinal);

        foreach (var product in products)
        {
            var normalizedName = NormalizeForMatching(product.Name);
            if (normalizedName.Length > 5 && normalizedQuery.Contains(normalizedName, StringComparison.Ordinal))
            {
                return true;
            }

            var identityTokens = ExtractProductIdentityTokens(product).ToList();
            if (identityTokens.Count == 0)
            {
                continue;
            }

            var matchingTokens = identityTokens
                .Where(queryTokens.Contains)
                .ToList();
            if (matchingTokens.Count >= 2)
            {
                return true;
            }

            if (matchingTokens.Any(ContainsDigit))
            {
                return true;
            }

            var brand = NormalizeForMatching(product.Brand);
            if (brand.Length >= 3 && queryTokens.Contains(brand) && matchingTokens.Count >= 1)
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> ExtractProductIdentityTokens(ProductResult product)
    {
        return ExtractTokens($"{product.Name} {product.Slug}")
            .Where(token => token.Length >= 2)
            .Where(token => !GenericProductIdentityTokens.Contains(token));
    }

    private static IEnumerable<string> ExtractTokens(string value)
    {
        foreach (Match match in Regex.Matches(value, @"[a-z0-9]+", RegexOptions.IgnoreCase))
        {
            yield return match.Value.ToLowerInvariant();
        }
    }

    private static bool ContainsDigit(string value)
    {
        return value.Any(char.IsDigit);
    }

    private static bool MentionsSpecificProductName(string query, IEnumerable<string> productNames)
    {
        var normalizedQuery = NormalizeForMatching(query);
        return productNames
            .Select(NormalizeForMatching)
            .Any(productName => productName.Length > 3 && normalizedQuery.Contains(productName, StringComparison.Ordinal));
    }

    private static string NormalizeForMatching(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder
            .ToString()
            .Normalize(NormalizationForm.FormC);
    }

    private string BuildRecommendationMessage(
        string query,
        IReadOnlyList<ProductResult> products,
        bool hasConfirmedStore)
    {
        var normalized = NormalizeForMatching(query);
        if (ContainsAny(normalized, "tablet industrial", "tablet robusto", "rugged tablet", "chao de fabrica", "fabrica", "logistica"))
        {
            return text.Pick(
                "Vou assumir que queres opções/recomendações de tablet industrial robusto. Vou separar por uso típico: Android para campo/logística, Windows para ERP/produção e modelos com leitor de código de barras/RFID.\n\nPara tablet industrial, eu escolheria conforme o sistema que precisas usar.\n\nRecomendação prática:\nMelhor equilíbrio Windows/chão de fábrica: Getac UX10 G3, porque combina Windows 11 Pro, ecrã brilhante, IP66/MIL-STD e toque com luvas.\nMelhor para logística/códigos de barras: Zebra ET45, se o teu software correr em Android ou web app e precisares de gestão empresarial, 5G, docks e acessórios.\nMelhor robustez profissional Windows: Panasonic TOUGHBOOK G2, mais caro, mas muito forte se precisares de módulos e operação em ambientes agressivos.\nMelhor custo Android rugged: Samsung Galaxy Tab Active4 Pro, quando Android chega e o orçamento pesa.\n\nAntes de avançar para comparar lojas, diz-me: sistema operativo obrigatório, ambiente de uso, necessidade de leitor de código/RFID, tamanho de ecrã e orçamento.",
                "I will assume you want recommendations for a rugged industrial tablet. I would separate them by typical use: Android for field/logistics, Windows for ERP/production, and models with barcode/RFID readers.\n\nFor an industrial tablet, I would choose based on the system you need to run.\n\nPractical recommendation:\nBest Windows/factory-floor balance: Getac UX10 G3, because it combines Windows 11 Pro, bright screen, IP66/MIL-STD and glove touch.\nBest for logistics/barcodes: Zebra ET45, if your software runs on Android or a web app and you need enterprise management, 5G, docks and accessories.\nBest professional Windows ruggedness: Panasonic TOUGHBOOK G2, more expensive, but stronger if you need modules and harsh-environment operation.\nBest Android rugged value: Samsung Galaxy Tab Active4 Pro, when Android is enough and budget matters.\n\nBefore moving to store comparison, tell me: required OS, usage environment, barcode/RFID needs, screen size and budget.");
        }

        var productNames = string.Join(", ", products.Take(3).Select(product => product.Name));
        var purchaseGuard = hasConfirmedStore
            ? text.Pick(
                "Quando escolhermos o modelo, avanço para comparação com lojas validadas.",
                "Once we choose the model, I can move to comparison with validated stores.")
            : text.Pick(
                "Ainda não avanço para compra porque não tenho loja direta com preço confirmado para estes modelos.",
                "I will not move to purchase yet because I do not have a direct store page with confirmed price for these models.");

        return text.Pick(
            $"Encontrei opções compatíveis: {productNames}.\n\nA escolha depende do uso real, orçamento e requisitos obrigatórios. Diz-me o que pesa mais: preço, robustez, sistema operativo, autonomia, acessórios ou prazo.\n\n{purchaseGuard}",
            $"I found compatible options: {productNames}.\n\nThe right choice depends on real use, budget and must-have requirements. Tell me what matters most: price, ruggedness, operating system, battery, accessories or delivery time.\n\n{purchaseGuard}");
    }

    private IReadOnlyList<CoffeeAdviceOption> BuildCoffeeMachineAdviceOptions()
    {
        var slots = new[]
        {
            new CoffeeAdviceSlot(
                "Mais barata - capsulas Nespresso",
                "Cheapest - Nespresso capsules",
                ["krups-nespresso-essenza-mini", "nespresso-vertuo-pop"],
                "Boa se queres gastar pouco, ter capsulas faceis de encontrar e uma maquina compacta.",
                "Good if you want a low entry price, easy-to-find capsules and a compact machine."),
            new CoffeeAdviceSlot(
                "Intermedia - espresso manual",
                "Mid-range - manual espresso",
                ["delonghi-stilosa-ec260", "nespresso-vertuo-pop"],
                "Boa entrada para espresso com porta-filtro, mais controlo e preco ainda moderado.",
                "Good entry point for portafilter espresso, with more control and still moderate price."),
            new CoffeeAdviceSlot(
                "Melhor custo/beneficio - automatica De'Longhi",
                "Best value - De'Longhi automatic",
                ["delonghi-magnifica-start", "philips-serie-2200-ep2224"],
                "Para mim e a escolha mais equilibrada: cafe em grao, moinho integrado e uso diario simples.",
                "For me this is the most balanced choice: beans, integrated grinder and simple daily use."),
            new CoffeeAdviceSlot(
                "Topo - automatica premium",
                "Premium - top automatic",
                ["delonghi-rivelia-exam440", "sage-bambino-plus"],
                "Opcao premium quando queres mais bebidas automaticas, melhor acabamento e margem para varios utilizadores.",
                "Premium choice when you want more automatic drinks, better finish and room for several users.")
        };

        var selected = new List<CoffeeAdviceOption>();
        foreach (var slot in slots)
        {
            var option = slot.ProductSlugs
                .Select(slug => BuildCoffeeAdviceOption(slug, slot))
                .FirstOrDefault(item => item is not null);

            if (option is not null && selected.All(item => !item.Product.Slug.Equals(option.Product.Slug, StringComparison.OrdinalIgnoreCase)))
            {
                selected.Add(option);
            }
        }

        return selected;
    }

    private CoffeeAdviceOption? BuildCoffeeAdviceOption(string productSlug, CoffeeAdviceSlot slot)
    {
        var product = data.GetProducts(productSlug)
            .FirstOrDefault(item => item.Slug.Equals(productSlug, StringComparison.OrdinalIgnoreCase));
        if (product is null)
        {
            return null;
        }

        var offer = data.BuildSellerOffersForProduct(product, productSlug)
            .Where(ComparisonDataService.IsConfirmedStoreOffer)
            .OrderBy(offer => offer.PriceCents)
            .ThenByDescending(offer => offer.ReliabilityScore)
            .FirstOrDefault();

        return offer is null
            ? null
            : new CoffeeAdviceOption(
                text.Pick(slot.LabelPt, slot.LabelEn),
                product,
                offer,
                text.Pick(slot.ReasonPt, slot.ReasonEn));
    }

    private string BuildCoffeeMachineRecommendationMessage(IReadOnlyList<CoffeeAdviceOption> options)
    {
        var builder = new StringBuilder();
        builder.AppendLine(text.Pick(
            "Vou montar 4 opcoes praticas: mais barata, intermedia, melhor custo/beneficio e topo, com foco em lojas que vendem em Portugal.",
            "I will build 4 practical options: cheapest, mid-range, best value and premium, focused on stores that sell in your market."));
        builder.AppendLine();
        builder.AppendLine(text.Pick(
            "Aqui vao 4 opcoes de maquinas de cafe com URLs para compra:",
            "Here are 4 coffee machine options with purchase URLs:"));
        builder.AppendLine();

        for (var index = 0; index < options.Count; index++)
        {
            var option = options[index];
            builder.AppendLine($"{index + 1}. {option.Label}");
            builder.AppendLine($"{option.Product.Name} - {option.Offer.Seller}");
            builder.AppendLine(text.Pick($"Preco visto: {option.Offer.Price}", $"Seen price: {option.Offer.Price}"));
            builder.AppendLine($"URL: {option.Offer.Url}");
            builder.AppendLine(option.Reason);
            builder.AppendLine();
        }

        var recommended = options.FirstOrDefault(option => NormalizeForMatching(option.Label).Contains("custo/beneficio", StringComparison.Ordinal))
            ?? options.OrderByDescending(option => option.Product.Score).First();
        builder.AppendLine(text.Pick(
            $"Escolha recomendada: compra a {recommended.Product.Name} se queres a melhor relacao qualidade/preco.",
            $"Recommended choice: buy the {recommended.Product.Name} if you want the best value for money."));

        return builder.ToString().Trim();
    }

    private IReadOnlyList<FridgeAdviceOption> BuildFridgeAdviceOptions()
    {
        var slots = new[]
        {
            new FridgeAdviceSlot(
                "Mais barata No Frost",
                "Cheapest No Frost",
                ["becken-bc3901n2-frigorifico-combinado", "becken-bdd5394nwh-frigorifico"],
                "Boa se queres gastar pouco, ter No Frost e uma capacidade suficiente para 1-3 pessoas.",
                "Good if you want a lower price, No Frost and enough capacity for 1-3 people."),
            new FridgeAdviceSlot(
                "Melhor preco/qualidade",
                "Best value",
                ["samsung-rb34c600esa-frigorifico-combinado", "hisense-rb390n4bcc-frigorifico-combinado"],
                "Para mim e a escolha mais equilibrada: No Frost, 344 L, classe A e preco forte para uso familiar normal.",
                "For me this is the most balanced choice: No Frost, 344 L, class A and strong price for normal family use."),
            new FridgeAdviceSlot(
                "Maior capacidade familiar",
                "Best family capacity",
                ["bosch-kgn497ldf-frigorifico-combinado", "lg-gbbs726cmb-frigorifico-combinado"],
                "Boa escolha quando precisas de mais litros, altura alta e uma solucao para compras semanais maiores.",
                "Good when you need more capacity, a tall unit and a solution for larger weekly shopping."),
            new FridgeAdviceSlot(
                "Topo equilibrado",
                "Balanced premium",
                ["lg-gbbs726cmb-frigorifico-combinado", "bosch-serie-6-frigorifico"],
                "Opcao mais premium quando valorizas acabamento, capacidade e uma marca forte para cozinha principal.",
                "More premium option when you value finish, capacity and a strong brand for the main kitchen.")
        };

        var selected = new List<FridgeAdviceOption>();
        foreach (var slot in slots)
        {
            var option = slot.ProductSlugs
                .Select(slug => BuildFridgeAdviceOption(slug, slot))
                .FirstOrDefault(item => item is not null);

            if (option is not null && selected.All(item => !item.Product.Slug.Equals(option.Product.Slug, StringComparison.OrdinalIgnoreCase)))
            {
                selected.Add(option);
            }
        }

        return selected;
    }

    private FridgeAdviceOption? BuildFridgeAdviceOption(string productSlug, FridgeAdviceSlot slot)
    {
        var product = data.GetProducts(productSlug)
            .FirstOrDefault(item => item.Slug.Equals(productSlug, StringComparison.OrdinalIgnoreCase));
        if (product is null)
        {
            return null;
        }

        var offer = data.BuildSellerOffersForProduct(product, productSlug)
            .Where(ComparisonDataService.IsConfirmedStoreOffer)
            .OrderBy(offer => offer.PriceCents)
            .ThenByDescending(offer => offer.ReliabilityScore)
            .FirstOrDefault();

        return offer is null
            ? null
            : new FridgeAdviceOption(
                text.Pick(slot.LabelPt, slot.LabelEn),
                product,
                offer,
                text.Pick(slot.ReasonPt, slot.ReasonEn));
    }

    private string BuildFridgeRecommendationMessage(IReadOnlyList<FridgeAdviceOption> options)
    {
        var builder = new StringBuilder();
        builder.AppendLine(text.Pick(
            "Vou procurar opcoes de frigorificos a venda em Portugal e priorizar modelos comuns, com boa disponibilidade e links diretos para compra.",
            "I will look for fridge options available in your market and prioritize common models with good availability and direct purchase links."));
        builder.AppendLine();
        builder.AppendLine(text.Pick(
            "Aqui tens algumas opcoes para comprar em Portugal:",
            "Here are some options to buy in your market:"));
        builder.AppendLine();
        builder.AppendLine(text.Pick("Links diretos:", "Direct links:"));
        builder.AppendLine();

        for (var index = 0; index < options.Count; index++)
        {
            var option = options[index];
            builder.AppendLine($"{index + 1}. {option.Label}");
            builder.AppendLine($"{option.Product.Name} - {option.Offer.Seller}");
            builder.AppendLine(text.Pick($"Preco visto: {option.Offer.Price}", $"Seen price: {option.Offer.Price}"));
            builder.AppendLine($"URL: {option.Offer.Url}");
            builder.AppendLine(option.Reason);
            builder.AppendLine();
        }

        var bestValue = options.FirstOrDefault(option => NormalizeForMatching(option.Label).Contains("preco/qualidade", StringComparison.Ordinal))
            ?? options.OrderByDescending(option => option.Product.Score).First();
        var family = options.FirstOrDefault(option => NormalizeForMatching(option.Label).Contains("capacidade", StringComparison.Ordinal))
            ?? options.OrderByDescending(option => ExtractCapacityLiters(option.Product)).First();

        builder.AppendLine(text.Pick(
            $"Melhor escolha preco/qualidade: {bestValue.Product.Name}.",
            $"Best value choice: {bestValue.Product.Name}."));
        builder.AppendLine(text.Pick(
            $"Melhor escolha para familia/grande capacidade: {family.Product.Name}.",
            $"Best choice for family/high capacity: {family.Product.Name}."));
        builder.AppendLine();
        builder.AppendLine(text.Pick(
            "Para afinar melhor: queres combinado ou americano, largura/altura maxima, capacidade minima, classe energetica, cor, orcamento e prioridade entre entrega rapida ou preco mais baixo?",
            "To refine this: do you want combi or American style, maximum width/height, minimum capacity, energy class, color, budget and priority between fast delivery or lowest price?"));

        return builder.ToString().Trim();
    }

    private static int ExtractCapacityLiters(ProductResult product)
    {
        return product.Specs
            .Select(spec => Regex.Match(spec, @"(?<liters>\d{2,4})\s*L", RegexOptions.IgnoreCase))
            .Where(match => match.Success)
            .Select(match => int.Parse(match.Groups["liters"].Value, CultureInfo.InvariantCulture))
            .DefaultIfEmpty(0)
            .Max();
    }

    private static bool ContainsAny(string value, params string[] terms)
    {
        return terms.Any(term => value.Contains(term, StringComparison.Ordinal));
    }

    private static ProductConversationOption ToConversationOption(ProductResult product, bool hasConfirmedStore)
    {
        return new ProductConversationOption(
            product.Slug,
            product.Name,
            product.Brand,
            product.Price,
            product.Badge,
            product.AiSummary,
            product.Specs.Take(4).ToList(),
            product.Accent,
            hasConfirmedStore);
    }

    private sealed record CoffeeAdviceSlot(
        string LabelPt,
        string LabelEn,
        IReadOnlyList<string> ProductSlugs,
        string ReasonPt,
        string ReasonEn);

    private sealed record CoffeeAdviceOption(
        string Label,
        ProductResult Product,
        SellerOffer Offer,
        string Reason);

    private sealed record FridgeAdviceSlot(
        string LabelPt,
        string LabelEn,
        IReadOnlyList<string> ProductSlugs,
        string ReasonPt,
        string ReasonEn);

    private sealed record FridgeAdviceOption(
        string Label,
        ProductResult Product,
        SellerOffer Offer,
        string Reason);
}

public sealed record ProductConversationDecision(
    bool CanProceed,
    string ResolvedQuery,
    string? ProductName,
    string AssistantMessage,
    IReadOnlyList<ProductConversationOption> Options)
{
    public static ProductConversationDecision Ready(string query, string productName, string message)
    {
        return new ProductConversationDecision(true, query, productName, message, Array.Empty<ProductConversationOption>());
    }

    public static ProductConversationDecision NeedsClarification(string query, string message)
    {
        return new ProductConversationDecision(false, query, null, message, Array.Empty<ProductConversationOption>());
    }

    public static ProductConversationDecision ProductAdvice(
        string query,
        string message,
        IReadOnlyList<ProductConversationOption> options)
    {
        return new ProductConversationDecision(false, query, null, message, options);
    }
}

public sealed record ProductConversationOption(
    string Slug,
    string Name,
    string Brand,
    string Price,
    string Badge,
    string Summary,
    IReadOnlyList<string> Specs,
    string Accent,
    bool HasConfirmedStore);
