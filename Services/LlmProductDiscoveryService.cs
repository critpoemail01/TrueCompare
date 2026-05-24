using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using TrueCompare.Models;
using TrueCompare.Options;

namespace TrueCompare.Services;

public sealed partial class LlmProductDiscoveryService(
    ComparisonDataService data,
    LlmProviderRouter providerRouter,
    IMemoryCache cache,
    ILogger<LlmProductDiscoveryService> logger,
    AppText text) : IProductDiscoveryService
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    private static readonly string[] AccentPalette = ["#5EE9A8", "#7BE8E0", "#B49CFF", "#E9D67B"];

    public async Task<ProductDiscoveryResult> DiscoverAsync(string? query, CancellationToken cancellationToken = default)
    {
        var normalizedQuery = NormalizeQuery(query);
        var cacheKey = $"product-discovery:{normalizedQuery.ToLowerInvariant()}";
        if (cache.TryGetValue(cacheKey, out ProductDiscoveryResult? cached) && cached is not null)
        {
            return cached;
        }

        var maxBudgetCents = TryExtractMaxBudgetCents(normalizedQuery);
        var localProducts = data.GetProducts(normalizedQuery);
        if (localProducts.Count > 0)
        {
            var localOffers = data.GetSellerOffers(normalizedQuery);
            var localResult = new ProductDiscoveryResult(
                normalizedQuery,
                localProducts,
                localOffers,
                false,
                text.Pick("Catalogo local", "Local catalog"));

            Remember(localResult, localOffers);
            cache.Set(cacheKey, localResult, TimeSpan.FromMinutes(30));
            return localResult;
        }

        var emptyFallback = new ProductDiscoveryResult(
            normalizedQuery,
            Array.Empty<ProductResult>(),
            Array.Empty<SellerOffer>(),
            false,
            text.Pick("Sem catalogo validado", "No validated catalog"));

        if (maxBudgetCents.HasValue && data.GetProducts(RemoveBudgetTerms(normalizedQuery)).Count > 0)
        {
            cache.Set(cacheKey, emptyFallback, TimeSpan.FromMinutes(10));
            return emptyFallback;
        }

        try
        {
            var providerResponse = await providerRouter.TryGetValidJsonAsync(
                "product-discovery",
                requiresVision: false,
                provider => BuildRequestJson(provider, normalizedQuery, maxBudgetCents),
                content => IsValidPayload(ReadPayload(content), normalizedQuery, maxBudgetCents),
                cancellationToken);

            if (providerResponse is null)
            {
                return BuildValidatedFallback(normalizedQuery, maxBudgetCents) ?? emptyFallback;
            }

            var payload = ReadPayload(providerResponse.JsonContent);
            var result = NormalizePayload(payload, normalizedQuery, providerResponse.ProviderName, maxBudgetCents);
            if (result.Products.Count == 0)
            {
                return BuildValidatedFallback(normalizedQuery, maxBudgetCents) ?? emptyFallback;
            }

            cache.Set(cacheKey, result, TimeSpan.FromMinutes(30));
            return result;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Product discovery LLM request timed out");
            return emptyFallback;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Product discovery failed");
            return emptyFallback;
        }
    }

    public async Task<ProductResult?> FindProductAsync(
        string? slug,
        string? query = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        var localProduct = data.FindProduct(slug);
        if (localProduct is not null)
        {
            if (!string.IsNullOrWhiteSpace(query))
            {
                var queryProducts = data.GetProducts(query);
                var queryMatch = queryProducts.FirstOrDefault(product => product.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
                if (queryMatch is not null)
                {
                    return queryMatch;
                }

                if (queryProducts.Count > 0 && !QueryMentionsProduct(localProduct, query))
                {
                    return queryProducts[0];
                }

                if (!QueryMentionsProduct(localProduct, query))
                {
                    var queryDiscovery = await DiscoverAsync(query, cancellationToken);
                    var discoveredProduct = queryDiscovery.Products.FirstOrDefault();
                    if (discoveredProduct is not null)
                    {
                        return discoveredProduct;
                    }
                }
            }

            return localProduct;
        }

        if (cache.TryGetValue(ProductCacheKey(slug), out ProductResult? cachedProduct) && cachedProduct is not null)
        {
            return cachedProduct;
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        var discovery = await DiscoverAsync(query, cancellationToken);
        return discovery.Products.FirstOrDefault(product => product.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IReadOnlyList<SellerOffer>> GetSellerOffersAsync(
        string? queryOrSlug,
        string? originalQuery = null,
        CancellationToken cancellationToken = default)
    {
        var localProductForStores = !string.IsNullOrWhiteSpace(queryOrSlug)
            ? data.FindProduct(queryOrSlug)
            : null;
        if (localProductForStores is not null)
        {
            return data.BuildSellerOffersForProduct(localProductForStores, originalQuery ?? queryOrSlug);
        }

        if (!string.IsNullOrWhiteSpace(queryOrSlug)
            && cache.TryGetValue(OffersCacheKey(queryOrSlug), out IReadOnlyList<SellerOffer>? cachedOffers)
            && cachedOffers is not null
            && cachedOffers.Count > 0)
        {
            return cachedOffers;
        }

        var query = string.IsNullOrWhiteSpace(originalQuery) ? queryOrSlug : originalQuery;
        if (string.IsNullOrWhiteSpace(query))
        {
            return data.GetSellerOffers(null);
        }

        var discovery = await DiscoverAsync(query, cancellationToken);
        if (!string.IsNullOrWhiteSpace(queryOrSlug)
            && cache.TryGetValue(OffersCacheKey(queryOrSlug), out cachedOffers)
            && cachedOffers is not null
            && cachedOffers.Count > 0)
        {
            return cachedOffers;
        }

        if (discovery.Offers.Count > 0)
        {
            return discovery.Offers;
        }

        var productForStores = discovery.Products
            .FirstOrDefault(product => !string.IsNullOrWhiteSpace(queryOrSlug)
                && product.Slug.Equals(queryOrSlug, StringComparison.OrdinalIgnoreCase))
            ?? discovery.Products.FirstOrDefault()
            ?? data.GetProducts(query).FirstOrDefault();

        if (productForStores is null)
        {
            return Array.Empty<SellerOffer>();
        }

        var generatedStoreSearches = data.BuildSellerOffersForProduct(productForStores, query);
        if (!string.IsNullOrWhiteSpace(queryOrSlug) && generatedStoreSearches.Count > 0)
        {
            cache.Set(OffersCacheKey(queryOrSlug), generatedStoreSearches, TimeSpan.FromMinutes(30));
        }

        return generatedStoreSearches;
    }

    private string BuildRequestJson(LlmProviderOptions provider, string query, long? maxBudgetCents)
    {
        var request = new
        {
            model = provider.Model,
            temperature = provider.Temperature,
            max_tokens = Math.Clamp(provider.MaxTokens, 800, 2400),
            response_format = provider.SupportsJsonObjectResponseFormat ? new { type = "json_object" } : null,
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = """
                    Es o motor de descoberta de produtos da TrueCompare. Responde apenas JSON valido, sem markdown.
                    Tens de sugerir produtos reais, identificaveis por marca e modelo, que correspondem diretamente ao pedido. Nao uses o catalogo de portateis por defeito.
                    Devolve uma lista curta e util: idealmente 3 produtos, no minimo 2 e no maximo 4. Se o pedido for um modelo exato, devolve apenas variantes realmente compatíveis desse modelo.
                    Se o pedido for rato, devolve ratos. Se for cadeira, devolve cadeiras. Se for ferramenta, devolve ferramentas. Se for carregador ou cabo de iPhone, devolve carregadores ou cabos, nao telemoveis.
                    Considera categorias comuns de comparadores portugueses como informatica, smartphones, imagem e som, gaming, electrodomesticos, bricolage, auto, animais, puericultura, casa, escritorio, desporto e saude/beleza.
                    A categoria principal do pedido e obrigatoria: cadeira nao pode devolver mesa/escrivaninha, rato nao pode devolver portatil, pneu nao pode devolver bicicleta, racao nao pode devolver fraldas.
                    Se houver orcamento maximo, todos os produtos devem ficar dentro desse orcamento. Nao forces uma recomendacao se so existirem opcoes acima do limite.
                    Ordena por utilidade real para o pedido: 1) correspondencia de categoria/modelo, 2) adequacao ao uso descrito, 3) preco plausivel, 4) garantia/baixo risco, 5) diversidade de marcas.
                    Usa precos aproximados e publicamente plausiveis; nao afirmes stock real nem disponibilidade em tempo real.
                    Nao cries URLs de pesquisa nem links de loja. A aplicacao so mostra lojas validadas ou pesquisas externas selecionadas fora da resposta do LLM.
                    O JSON deve ter: category, confidence, products.
                    products deve ter 2 a 4 itens com: name, brand, price, priceCents, score, badge, specs, highlights, verificationChecks, summary, warnings.
                    Mantem specs, highlights, verificationChecks e warnings curtos. Nao preenchas sellerOffers; usa sellerOffers: [].
                    Se nao conseguires sugerir produtos reais e compativeis, devolve products: [].
                    """
                },
                new
                {
                    role = "user",
                    content = JsonSerializer.Serialize(new
                    {
                        language = text.IsEnglish ? "en-US" : "pt-PT",
                        query,
                        maxBudgetCents,
                        validation = new
                        {
                            mustMatchQuery = true,
                            rejectWrongCategory = true,
                            rejectAboveBudget = maxBudgetCents.HasValue,
                            minProductsWhenPossible = 2,
                            maxProducts = 4,
                            preferRealModelsOverGenericDescriptions = true,
                            avoidStoreLinks = true
                        }
                    }, JsonOptions)
                }
            }
        };

        return JsonSerializer.Serialize(request, JsonOptions);
    }

    private ProductDiscoveryResult NormalizePayload(
        DiscoveryPayload? payload,
        string query,
        string providerName,
        long? maxBudgetCents)
    {
        var products = Clean(payload?.Products)
            .Select(product => NormalizeProduct(product, query))
            .Where(product => product is not null)
            .Cast<ProductResult>()
            .Where(product => IsRelevantProduct(product, query, maxBudgetCents))
            .Select(product => data.EnrichProduct(product, query))
            .GroupBy(product => product.Slug, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderByDescending(product => ScoreGeneratedProduct(product, query, maxBudgetCents))
            .ThenByDescending(product => product.Score)
            .Take(4)
            .Select((product, index) => product with { Rank = index + 1, Accent = AccentPalette[index % AccentPalette.Length] })
            .ToList();

        var offersBySlug = new Dictionary<string, IReadOnlyList<SellerOffer>>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < products.Count; index++)
        {
            var sourceProduct = Clean(payload?.Products).ElementAtOrDefault(index);
            var offers = NormalizeOffers(sourceProduct?.SellerOffers, products[index]);
            if (offers.Count == 0)
            {
                offers = data.BuildSellerOffersForProduct(products[index], query);
            }

            offersBySlug[products[index].Slug] = offers;
            cache.Set(OffersCacheKey(products[index].Slug), offers, TimeSpan.FromMinutes(30));
            cache.Set(ProductCacheKey(products[index].Slug), products[index], TimeSpan.FromMinutes(30));
        }

        var resultOffers = products.Count > 0 && offersBySlug.TryGetValue(products[0].Slug, out var firstOffers)
            ? firstOffers
            : Array.Empty<SellerOffer>();

        var result = new ProductDiscoveryResult(
            query,
            products,
            resultOffers,
            true,
            text.Pick($"LLM online - {providerName}", $"Online LLM - {providerName}"));

        Remember(result, resultOffers);
        return result;
    }

    private ProductResult? NormalizeProduct(DiscoveryProductPayload product, string query)
    {
        var name = CleanText(product.Name);
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var brand = CleanText(product.Brand, GuessBrand(name));
        var priceCents = product.PriceCents > 0
            ? (long)Math.Round(product.PriceCents, MidpointRounding.AwayFromZero)
            : ParsePriceCents(product.Price);
        if (priceCents <= 0)
        {
            return null;
        }

        var specs = Clean(product.Specs)
            .Select(item => CleanText(item))
            .Where(item => item.Length > 0)
            .Take(6)
            .ToList();
        while (specs.Count < 4)
        {
            specs.Add(text.Pick("Confirmar especificacao", "Confirm specification"));
        }

        var highlights = Clean(product.Highlights)
            .Select(item => CleanText(item))
            .Where(item => item.Length > 0)
            .Take(4)
            .DefaultIfEmpty(text.Pick("Boa correspondencia com o pedido", "Good match for the request"))
            .ToList();

        var verificationChecks = Clean(product.VerificationChecks)
            .Select(item => CleanText(item))
            .Where(item => item.Length > 0)
            .Take(5)
            .DefaultIfEmpty(text.Pick("Produto real identificado pela IA", "Real product identified by AI"))
            .Append(text.Pick("Preco e vendedor a confirmar antes da compra", "Confirm price and seller before buying"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToList();

        var warnings = Clean(product.Warnings)
            .Select(item => CleanText(item))
            .Where(item => item.Length > 0)
            .Take(3)
            .Select((warning, index) => new FraudAlert(text.Pick($"Validacao {index + 1}", $"Validation {index + 1}"), warning))
            .ToList();

        return new ProductResult(
            Slugify($"{brand} {name}"),
            1,
            (int)Math.Round(Math.Clamp(product.Score <= 0 ? 78 : product.Score, 60, 98), MidpointRounding.AwayFromZero),
            name,
            brand,
            CleanText(product.Price, FormatCurrency(priceCents)),
            AccentPalette[0],
            CleanText(product.Badge, text.Pick("Recomendado", "Recommended")),
            specs,
            highlights,
            verificationChecks,
            warnings,
            CleanText(product.Summary, text.Pick(
                $"Sugestao gerada para: {query}. Confirma o preco final no vendedor antes de encomendar.",
                $"Suggestion generated for: {query}. Confirm final price with the seller before ordering.")));
    }

    private ProductDiscoveryResult? BuildValidatedFallback(string query, long? maxBudgetCents)
    {
        var primaryTerm = ExtractMeaningfulTerms(query).FirstOrDefault(IsProductNounTerm);
        if (string.IsNullOrWhiteSpace(primaryTerm))
        {
            return null;
        }

        var products = BuildKnownFallbackProducts(primaryTerm, maxBudgetCents)
            .Where(product => IsRelevantProduct(product, query, maxBudgetCents))
            .Select(product => data.EnrichProduct(product, query))
            .Take(4)
            .Select((product, index) => product with { Rank = index + 1, Accent = AccentPalette[index % AccentPalette.Length] })
            .ToList();

        if (products.Count == 0)
        {
            return null;
        }

        var offers = BuildDefaultOffers(products[0]);
        var result = new ProductDiscoveryResult(
            query,
            products,
            offers,
            false,
            text.Pick("Fallback validado", "Validated fallback"));

        Remember(result, offers);
        return result;
    }

    private IReadOnlyList<ProductResult> BuildKnownFallbackProducts(string primaryTerm, long? maxBudgetCents)
    {
        var budget = long.MaxValue;

        return primaryTerm switch
        {
            "cadeira" or "chair" => new[]
            {
                CreateFallbackProduct("ikea-flintan", "IKEA FLINTAN", "IKEA", 7999, "Melhor valor", "Cadeira escritorio", "Altura ajustavel"),
                CreateFallbackProduct("songmics-obg22b", "SONGMICS OBG22B", "SONGMICS", 8999, "Mais confortavel", "Cadeira escritorio", "Apoio lombar"),
                CreateFallbackProduct("vinsetto-cadeira-escritorio", "Vinsetto Cadeira de Escritorio", "Vinsetto", 9499, "Boa ergonomia", "Cadeira escritorio", "Bracos ajustaveis")
            },
            "mesa" or "desk" => new[]
            {
                CreateFallbackProduct("ikea-lagkapten-adils", "IKEA LAGKAPTEN / ADILS", "IKEA", Math.Min(budget, 3999), "Home office económico", "Mesa escritorio", "120x60 cm"),
                CreateFallbackProduct("flexispot-e7", "FlexiSpot E7", "FlexiSpot", Math.Min(budget, 34900), "Altura ajustável", "Secretária elevatória", "Motor duplo"),
                CreateFallbackProduct("homcom-mesa-escritorio", "HOMCOM Mesa de Escritório", "HOMCOM", Math.Min(budget, 7999), "Boa relação valor", "Mesa escritorio", "Arrumação lateral")
            },
            "teclado" or "keyboard" => new[]
            {
                CreateFallbackProduct("logitech-k380", "Logitech K380", "Logitech", Math.Min(budget, 4499), "Melhor compacto", "Teclado bluetooth", "Multi-dispositivo"),
                CreateFallbackProduct("keychron-c3-pro", "Keychron C3 Pro", "Keychron", Math.Min(budget, 6999), "Mecanico", "Teclado mecanico", "USB-C"),
                CreateFallbackProduct("logitech-k120", "Logitech K120", "Logitech", Math.Min(budget, 1499), "Mais barato", "Teclado com fio", "Layout PT")
            },
            "microfone" or "microfones" or "microphone" or "microphones" => new[]
            {
                CreateFallbackProduct("blue-snowball-ice", "Blue Snowball iCE", "Logitech", Math.Min(budget, 5499), "Melhor valor", "Microfone USB", "Podcast e chamadas"),
                CreateFallbackProduct("rode-nt-usb-mini", "Rode NT-USB Mini", "Rode", Math.Min(budget, 8900), "Voz compacta", "Microfone USB", "Cardioide"),
                CreateFallbackProduct("hyperx-solocast", "HyperX SoloCast", "HyperX", Math.Min(budget, 5999), "Streaming económico", "Microfone USB", "Tap-to-mute")
            },
            "carregador" or "carregadores" or "charger" or "chargers" or "adaptador" or "cabo" or "lightning" or "magsafe" or "powerbank" => new[]
            {
                CreateFallbackProduct("apple-usb-c-20w-power-adapter", "Apple Carregador USB-C 20W", "Apple", Math.Min(budget, 1999), "Oficial iPhone", "Carregador USB-C", "20W Power Delivery"),
                CreateFallbackProduct("anker-nano-usb-c-30w", "Anker Nano USB-C 30W", "Anker", Math.Min(budget, 2499), "Mais compacto", "Carregador USB-C", "30W GaN"),
                CreateFallbackProduct("belkin-boostcharge-magsafe-15w", "Belkin BoostCharge MagSafe 15W", "Belkin", Math.Min(budget, 3999), "MagSafe", "Carregador iPhone", "15W sem fios")
            },
            "monitor" or "display" or "ecra" or "ecran" => new[]
            {
                CreateFallbackProduct("lg-ultragear-27gp850-b", "LG UltraGear 27GP850-B", "LG", Math.Min(budget, 27900), "Melhor QHD gaming", "Monitor 27 polegadas", "QHD 165 Hz"),
                CreateFallbackProduct("samsung-odyssey-g5-27", "Samsung Odyssey G5 27", "Samsung", Math.Min(budget, 22900), "Curvo competitivo", "Monitor 27 polegadas", "QHD 144 Hz"),
                CreateFallbackProduct("dell-p2723d", "Dell P2723D", "Dell", Math.Min(budget, 23900), "Boa garantia", "Monitor 27 polegadas", "QHD IPS")
            },
            "auscultadores" or "headphones" or "auriculares" => new[]
            {
                CreateFallbackProduct("sony-wh-ch520", "Sony WH-CH520", "Sony", Math.Min(budget, 4999), "Melhor bateria", "Auscultadores bluetooth", "50h bateria"),
                CreateFallbackProduct("jbl-tune-520bt", "JBL Tune 520BT", "JBL", Math.Min(budget, 3999), "Bom valor", "Auscultadores bluetooth", "Graves JBL"),
                CreateFallbackProduct("soundcore-q20i", "Soundcore Q20i", "Anker", Math.Min(budget, 5999), "Cancelamento ruido", "Auscultadores ANC", "Bluetooth")
            },
            "disco" or "hdd" or "ssd" or "armazenamento" => new[]
            {
                CreateFallbackProduct("western-digital-my-passport-1tb", "Western Digital My Passport 1TB", "Western Digital", Math.Min(budget, 5880), "Melhor Escolha", "Disco externo", "1TB USB 3.2"),
                CreateFallbackProduct("seagate-expansion-portable-2tb", "Seagate Expansion Portable 2TB", "Seagate", Math.Min(budget, 6999), "Mais capacidade", "Disco externo", "2TB USB 3.0"),
                CreateFallbackProduct("samsung-t7-shield-1tb", "Samsung T7 Shield 1TB", "Samsung", Math.Min(budget, 10990), "SSD resistente", "SSD externo", "USB 3.2 Gen 2")
            },
            "televisor" or "televisao" or "tv" => new[]
            {
                CreateFallbackProduct("lg-oled-c4-55", "LG OLED C4 55", "LG", Math.Min(budget, 119900), "Melhor OLED", "Televisor 55 polegadas", "OLED 4K"),
                CreateFallbackProduct("samsung-qn90d-55", "Samsung QN90D 55", "Samsung", Math.Min(budget, 109900), "Melhor brilho", "Televisor 55 polegadas", "Neo QLED"),
                CreateFallbackProduct("tcl-55c805", "TCL 55C805", "TCL", Math.Min(budget, 59900), "Preco forte", "Televisor 55 polegadas", "Mini LED 4K")
            },
            "cafe" or "espresso" => new[]
            {
                CreateFallbackProduct("delonghi-magnifica-start", "De'Longhi Magnifica Start", "De'Longhi", Math.Min(budget, 32900), "Automatica equilibrada", "Maquina de cafe", "Grao e moinho"),
                CreateFallbackProduct("sage-bambino-plus", "Sage Bambino Plus", "Sage", Math.Min(budget, 49900), "Melhor espresso manual", "Maquina de cafe", "Porta-filtro"),
                CreateFallbackProduct("nespresso-vertuo-pop", "Nespresso Vertuo Pop", "Nespresso", Math.Min(budget, 6999), "Mais simples", "Maquina de cafe", "Capsulas Vertuo")
            },
            "berbequim" or "drill" or "aparafusadora" => new[]
            {
                CreateFallbackProduct("bosch-professional-gsb-18v-55", "Bosch Professional GSB 18V-55", "Bosch", Math.Min(budget, 16900), "Melhor 18V", "Berbequim sem fios", "18V brushless"),
                CreateFallbackProduct("makita-dhp482z", "Makita DHP482Z", "Makita", Math.Min(budget, 8900), "Corpo economico", "Berbequim sem fios", "18V LXT"),
                CreateFallbackProduct("dewalt-dcd796p2", "DeWalt DCD796P2", "DeWalt", Math.Min(budget, 23900), "Kit completo", "Berbequim sem fios", "18V com 2 baterias")
            },
            "pneu" or "pneus" or "tyre" or "tyres" => new[]
            {
                CreateFallbackProduct("michelin-primacy-4-plus-205-55-r16", "Michelin Primacy 4+ 205/55 R16", "Michelin", Math.Min(budget, 9200), "Melhor seguranca", "Pneu 205/55 R16", "Verao"),
                CreateFallbackProduct("continental-premiumcontact-7-205-55-r16", "Continental PremiumContact 7 205/55 R16", "Continental", Math.Min(budget, 8800), "Muito equilibrado", "Pneu 205/55 R16", "Verao"),
                CreateFallbackProduct("bridgestone-turanza-t005-205-55-r16", "Bridgestone Turanza T005 205/55 R16", "Bridgestone", Math.Min(budget, 7900), "Bom preco premium", "Pneu 205/55 R16", "Verao")
            },
            "impressora" or "printer" => new[]
            {
                CreateFallbackProduct("hp-officejet-pro-9120e", "HP OfficeJet Pro 9120e", "HP", Math.Min(budget, 14900), "Escritorio compacto", "Impressora multifuncoes", "Wi-Fi duplex"),
                CreateFallbackProduct("epson-ecotank-l3250", "Epson EcoTank L3250", "Epson", Math.Min(budget, 17900), "Baixo custo por pagina", "Impressora multifuncoes", "Tanques de tinta"),
                CreateFallbackProduct("brother-dcp-l2620dw", "Brother DCP-L2620DW", "Brother", Math.Min(budget, 16900), "Laser mono", "Impressora laser", "Duplex Wi-Fi")
            },
            "racao" or "cao" or "gato" => new[]
            {
                CreateFallbackProduct("royal-canin-medium-adult-15kg", "Royal Canin Medium Adult 15kg", "Royal Canin", Math.Min(budget, 6200), "Marca premium", "Racao para cao", "15kg adulto"),
                CreateFallbackProduct("purina-pro-plan-medium-adult-14kg", "Purina Pro Plan Medium Adult 14kg", "Purina", Math.Min(budget, 5400), "Bom equilibrio", "Racao para cao", "14kg adulto"),
                CreateFallbackProduct("libra-adult-frango-14kg", "Libra Adult Frango 14kg", "Libra", Math.Min(budget, 2999), "Mais economica", "Racao para cao", "14kg frango")
            },
            "fralda" or "fraldas" or "bebe" or "baby" => new[]
            {
                CreateFallbackProduct("pampers-premium-protection-t4", "Pampers Premium Protection T4", "Pampers", Math.Min(budget, 2499), "Fralda premium", "Fraldas bebe", "Tamanho 4"),
                CreateFallbackProduct("dodot-aqua-pure", "Dodot Aqua Pure Toalhitas", "Dodot", Math.Min(budget, 1999), "Toalhitas sensiveis", "Higiene bebe", "99% agua"),
                CreateFallbackProduct("chicco-next2me", "Chicco Next2Me", "Chicco", Math.Min(budget, 19900), "Berco lateral", "Puericultura", "0-6 meses")
            },
            "bicicleta" or "bike" => new[]
            {
                CreateFallbackProduct("rockrider-e-st100", "Rockrider E-ST 100", "Rockrider", Math.Min(budget, 99900), "Entrada e-bike", "Bicicleta eletrica", "BTT assistida"),
                CreateFallbackProduct("xiaomi-electric-scooter-4", "Xiaomi Electric Scooter 4", "Xiaomi", Math.Min(budget, 44900), "Mobilidade urbana", "Trotinete eletrica", "Autonomia urbana"),
                CreateFallbackProduct("garmin-forerunner-255", "Garmin Forerunner 255", "Garmin", Math.Min(budget, 24900), "Desporto conectado", "Relogio desportivo", "GPS")
            },
            _ => Array.Empty<ProductResult>()
        };
    }

    private ProductResult CreateFallbackProduct(
        string slug,
        string name,
        string brand,
        long priceCents,
        string badge,
        string productType,
        string keySpec)
    {
        return new ProductResult(
            slug,
            1,
            78,
            name,
            brand,
            FormatCurrency(priceCents),
            AccentPalette[0],
            badge,
            new[] { productType, keySpec, "Preco estimado", "Garantia a validar", "Vendedor autorizado", "Confirmar stock" },
            new[] { "Categoria correta", "Dentro do criterio indicado", "Pesquisa pronta para vendedor autorizado" },
            new[] { "Produto reconhecido", "Preco final a confirmar", "Garantia e vendedor a validar" },
            Array.Empty<FraudAlert>(),
            text.Pick(
                "Sugestao validada por categoria quando os providers LLM nao devolveram JSON utilizavel. Confirma sempre o preco final antes de comprar.",
                "Category-validated suggestion used when LLM providers did not return usable JSON. Always confirm final price before buying."));
    }

    private IReadOnlyList<SellerOffer> NormalizeOffers(IReadOnlyList<DiscoveryOfferPayload>? offers, ProductResult product)
    {
        return BuildDefaultOffers(product);
    }

    private IReadOnlyList<SellerOffer> BuildDefaultOffers(ProductResult product)
    {
        return data.BuildSellerOffersForProduct(product)
            .Take(6)
            .ToList();
    }

    private static DiscoveryPayload? ReadPayload(string content)
    {
        return JsonSerializer.Deserialize<DiscoveryPayload>(content, JsonOptions);
    }

    private static bool IsValidPayload(DiscoveryPayload? payload, string query, long? maxBudgetCents)
    {
        return Clean(payload?.Products)
            .Select(product => NormalizeValidationProduct(product))
            .Any(product => product is not null && IsRelevantProduct(product, query, maxBudgetCents));
    }

    private static ProductResult? NormalizeValidationProduct(DiscoveryProductPayload product)
    {
        var name = CleanText(product.Name);
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var priceCents = product.PriceCents > 0
            ? (long)Math.Round(product.PriceCents, MidpointRounding.AwayFromZero)
            : ParsePriceCents(product.Price);
        if (priceCents <= 0)
        {
            return null;
        }

        var brand = CleanText(product.Brand, GuessBrand(name));
        return new ProductResult(
            Slugify($"{brand} {name}"),
            1,
            (int)Math.Round(Math.Clamp(product.Score <= 0 ? 78 : product.Score, 60, 98), MidpointRounding.AwayFromZero),
            name,
            brand,
            CleanText(product.Price, FormatCurrency(priceCents)),
            AccentPalette[0],
            CleanText(product.Badge, "Recomendado"),
            Clean(product.Specs).Select(item => CleanText(item)).Where(item => item.Length > 0).ToList(),
            Clean(product.Highlights).Select(item => CleanText(item)).Where(item => item.Length > 0).ToList(),
            Clean(product.VerificationChecks).Select(item => CleanText(item)).Where(item => item.Length > 0).ToList(),
            Array.Empty<FraudAlert>(),
            CleanText(product.Summary));
    }

    private static int ScoreGeneratedProduct(ProductResult product, string query, long? maxBudgetCents)
    {
        var productText = NormalizeText(string.Join(' ', new[]
        {
            product.Name,
            product.Brand,
            product.Badge,
            product.AiSummary,
            string.Join(' ', product.Specs),
            string.Join(' ', product.Highlights)
        }));
        var terms = ExtractMeaningfulTerms(query).ToList();
        var score = product.Score;

        foreach (var term in terms)
        {
            var expandedMatches = ExpandTerm(term)
                .Any(expanded => productText.Contains(expanded, StringComparison.OrdinalIgnoreCase));
            if (expandedMatches)
            {
                score += IsProductNounTerm(term) ? 18 : 8;
            }
        }

        if (maxBudgetCents.HasValue)
        {
            var priceCents = ParsePriceCents(product.Price);
            if (priceCents > 0 && priceCents <= maxBudgetCents.Value)
            {
                score += 12;
                if (priceCents <= maxBudgetCents.Value * 0.85m)
                {
                    score += 6;
                }
            }
        }

        return score;
    }

    private static bool IsRelevantProduct(ProductResult product, string query, long? maxBudgetCents)
    {
        var priceCents = ParsePriceCents(product.Price);
        if (maxBudgetCents.HasValue && priceCents > maxBudgetCents.Value)
        {
            return false;
        }

        var terms = ExtractMeaningfulTerms(query).ToList();
        if (terms.Count == 0)
        {
            return true;
        }

        var identityText = NormalizeText(string.Join(' ', new[]
        {
            product.Name,
            product.Brand,
            product.Badge,
            string.Join(' ', product.Specs)
        }));

        var productText = NormalizeText(string.Join(' ', new[]
        {
            product.Name,
            product.Brand,
            product.Badge,
            product.AiSummary,
            string.Join(' ', product.Specs),
            string.Join(' ', product.Highlights)
        }));

        if (LooksLikePhoneQuery(query) && !LooksLikeAccessoryQuery(query) && !LooksLikePhoneProduct(identityText))
        {
            return false;
        }

        if (LooksLikeLaptopQuery(query) && !LooksLikeLaptopProduct(identityText))
        {
            return false;
        }

        var primaryProductTerms = terms.Where(IsProductNounTerm).ToList();
        if (primaryProductTerms.Count > 0)
        {
            return !primaryProductTerms.Any(term => HasContradictingProductType(term, identityText))
                && primaryProductTerms.Any(term => ExpandTerm(term).Any(expanded =>
                    identityText.Contains(expanded, StringComparison.OrdinalIgnoreCase)
                    || productText.Contains(expanded, StringComparison.OrdinalIgnoreCase)));
        }

        var requiredTerms = primaryProductTerms.Count > 0
            ? primaryProductTerms
            : terms.Where(term => !IsDescriptorTerm(term)).ToList();
        var termsToMatch = requiredTerms.Count > 0 ? requiredTerms : terms;

        return termsToMatch.Any(term => ExpandTerm(term).Any(expanded => productText.Contains(expanded, StringComparison.OrdinalIgnoreCase)));
    }

    private void Remember(ProductDiscoveryResult result, IReadOnlyList<SellerOffer> defaultOffers)
    {
        foreach (var product in result.Products)
        {
            cache.Set(ProductCacheKey(product.Slug), product, TimeSpan.FromMinutes(30));
            if (!cache.TryGetValue(OffersCacheKey(product.Slug), out _))
            {
                cache.Set(OffersCacheKey(product.Slug), defaultOffers.Count > 0 ? defaultOffers : BuildDefaultOffers(product), TimeSpan.FromMinutes(30));
            }
        }
    }

    private static string NormalizeQuery(string? query)
    {
        return string.IsNullOrWhiteSpace(query)
            ? "produto com melhor preco garantia e baixo risco"
            : query.Trim();
    }

    private static bool QueryMentionsProduct(ProductResult product, string query)
    {
        var normalizedQuery = NormalizeText(query);
        var normalizedName = NormalizeText(product.Name);
        var normalizedBrand = NormalizeText(product.Brand);
        var normalizedSlug = NormalizeText(product.Slug).Replace('-', ' ');

        return normalizedQuery.Contains(normalizedName, StringComparison.OrdinalIgnoreCase)
            || normalizedQuery.Contains(normalizedSlug, StringComparison.OrdinalIgnoreCase)
            || normalizedQuery.Contains(normalizedBrand, StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> ExtractMeaningfulTerms(string query)
    {
        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "a", "o", "os", "as", "um", "uma", "uns", "umas", "de", "do", "da", "dos", "das",
            "com", "sem", "para", "por", "que", "queres", "quero", "queria", "procuro", "procurar", "comprar",
            "compra", "melhor", "bom", "boa", "bons", "boas", "ate", "at", "euros", "euro", "eur",
            "preco", "valor", "orcamento", "max", "maximo", "menos", "mais", "produto", "produtos",
            "vendedor", "revendedor", "autorizado", "autorizada", "garantia", "seller", "store", "best",
            "buy", "under", "below", "with", "for", "the", "and"
        };

        return NormalizeText(query)
            .Replace('-', ' ')
            .Replace('/', ' ')
            .Replace(',', ' ')
            .Replace('.', ' ')
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(term => term.Length > 2 && !long.TryParse(term, out _) && !stopWords.Contains(term))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> ExpandTerm(string term)
    {
        yield return term;

        foreach (var expanded in term switch
        {
            "rato" or "ratos" or "mouse" or "mice" => new[] { "rato", "mouse", "mice" },
            "teclado" or "keyboard" => new[] { "teclado", "keyboard" },
            "carregador" or "carregadores" or "charger" or "chargers" or "adaptador" or "cabo" or "lightning" or "magsafe" or "powerbank" => new[] { "carregador", "charger", "usb-c", "power delivery", "magsafe", "lightning", "iphone" },
            "cadeira" or "chair" => new[] { "cadeira", "chair" },
            "mesa" or "desk" => new[] { "mesa", "desk" },
            "monitor" or "ecra" or "ecran" or "display" => new[] { "monitor", "display", "ecra", "ecran" },
            "auscultadores" or "headphones" or "auriculares" => new[] { "auscultadores", "headphones", "auriculares" },
            "camara" or "camera" => new[] { "camara", "camera" },
            "microfone" or "microfones" or "microphone" or "microphones" => new[] { "microfone", "microphone", "usb", "podcast", "streaming" },
            "frigorifico" or "fridge" => new[] { "frigorifico", "fridge", "combinado" },
            "tablet" or "tablets" or "tablete" or "tabela" => new[] { "tablet", "touch", "rugged", "industrial" },
            "rugged" or "robusto" or "todoterreno" or "industrial" or "fabrica" => new[] { "rugged", "industrial", "ip66", "ip68", "mil-std" },
            "bicicleta" or "bike" => new[] { "bicicleta", "bike" },
            "berbequim" or "drill" or "aparafusadora" => new[] { "berbequim", "drill", "aparafusadora" },
            "disco" or "hdd" or "ssd" or "armazenamento" => new[] { "disco", "hdd", "ssd", "drive", "armazenamento" },
            "televisor" or "televisao" or "tv" => new[] { "televisor", "televisao", "tv" },
            "cafe" or "espresso" => new[] { "cafe", "espresso", "maquina" },
            "pneu" or "pneus" or "tyre" or "tyres" => new[] { "pneu", "tyre", "205/55", "r16" },
            "racao" or "cao" or "gato" => new[] { "racao", "cao", "gato", "pet" },
            "fralda" or "fraldas" or "bebe" or "baby" => new[] { "fralda", "fraldas", "bebe", "baby" },
            _ => Array.Empty<string>()
        })
        {
            yield return expanded;
        }
    }

    private static bool LooksLikePhoneQuery(string query)
    {
        var normalized = NormalizeText(query);
        return ContainsAny(normalized, "iphone", "galaxy", "pixel", "smartphone", "telemovel", "telemoveis", "telefone");
    }

    private static bool LooksLikeAccessoryQuery(string query)
    {
        var normalized = NormalizeText(query);
        return ContainsAny(normalized, "carregador", "charger", "cabo", "adaptador", "magsafe", "lightning", "powerbank", "capa", "pelicula", "pelicula");
    }

    private static bool LooksLikePhoneProduct(string identityText)
    {
        return ContainsAny(identityText, "smartphone", "telemovel", "telefone", "iphone", "galaxy s", "galaxy a", "pixel", "redmi", "poco");
    }

    private static bool LooksLikeLaptopQuery(string query)
    {
        var normalized = NormalizeText(query);
        return ContainsAny(normalized, "portatil", "laptop", "notebook", "macbook", "thinkpad", "vivobook", "zenbook");
    }

    private static bool LooksLikeLaptopProduct(string identityText)
    {
        return ContainsAny(identityText, "portatil", "laptop", "notebook", "macbook", "thinkpad", "vivobook", "zenbook", "computador");
    }

    private static bool IsDescriptorTerm(string term)
    {
        return term is "barato" or "barata" or "economico" or "economica" or "premium"
            or "gaming" or "ergonomico" or "ergonomica" or "confortavel" or "leve"
            or "rapido" or "rapida" or "silencioso" or "silenciosa" or "wireless"
            or "bluetooth" or "rugged" or "robusto" or "robusta" or "todoterreno"
            or "industrial" or "bom" or "boa";
    }

    private static bool IsProductNounTerm(string term)
    {
        return term is "rato" or "ratos" or "mouse" or "mice" or "teclado" or "keyboard"
            or "carregador" or "carregadores" or "charger" or "chargers" or "adaptador" or "cabo" or "lightning" or "magsafe" or "powerbank"
            or "cadeira" or "chair" or "mesa" or "desk" or "monitor" or "ecra" or "ecran"
            or "display" or "auscultadores" or "headphones" or "auriculares" or "camara"
            or "camera" or "frigorifico" or "fridge" or "tablet" or "tablets" or "tablete" or "tabela"
            or "bicicleta" or "bike" or "berbequim" or "drill" or "aparafusadora"
            or "impressora" or "printer" or "microfone" or "microfones" or "microphone" or "microphones" or "disco" or "hdd"
            or "ssd" or "armazenamento" or "televisor" or "televisao" or "tv" or "cafe"
            or "espresso" or "pneu" or "pneus" or "tyre" or "tyres" or "racao"
            or "cao" or "gato" or "fralda" or "fraldas" or "bebe" or "baby";
    }

    private static bool HasContradictingProductType(string requestedTerm, string identityText)
    {
        return requestedTerm switch
        {
            "cadeira" or "chair" => ContainsAny(identityText, "mesa", "desk", "table", "escrivaninha", "escrivaneta", "consola", "console", "playstation", "ps5", "xbox", "nintendo", "smartphone", "telemovel", "telefone", "rato", "mouse", "teclado", "keyboard", "monitor"),
            "rato" or "ratos" or "mouse" or "mice" => ContainsAny(identityText, "portatil", "laptop", "notebook", "smartphone", "telefone"),
            "carregador" or "carregadores" or "charger" or "chargers" or "adaptador" or "cabo" or "lightning" or "magsafe" or "powerbank" => ContainsAny(identityText, "smartphone", "telefone", "telemovel", "iphone 17", "iphone 16", "galaxy s", "pixel")
                && !ContainsAny(identityText, "carregador", "charger", "adaptador", "cabo", "usb-c", "magsafe", "lightning", "power delivery", "powerbank"),
            "teclado" or "keyboard" => ContainsAny(identityText, "rato", "mouse", "monitor", "headphone", "auscultador"),
            "monitor" or "display" or "ecra" or "ecran" => ContainsAny(identityText, "portatil", "laptop", "televisao", "tv"),
            "tablet" or "tablets" or "tablete" or "tabela" => ContainsAny(identityText, "frigorifico", "fridge", "rato", "mouse", "cadeira", "chair"),
            "disco" or "hdd" or "ssd" or "armazenamento" => ContainsAny(identityText, "portatil", "laptop", "smartphone", "cadeira", "televisor", "tv"),
            "televisor" or "televisao" or "tv" => ContainsAny(identityText, "monitor", "portatil", "laptop", "smartphone"),
            "cafe" or "espresso" => ContainsAny(identityText, "frigorifico", "lavadora", "lava", "televisor", "monitor"),
            "berbequim" or "drill" or "aparafusadora" => ContainsAny(identityText, "portatil", "laptop", "smartphone", "cadeira", "pneu"),
            "pneu" or "pneus" or "tyre" or "tyres" => ContainsAny(identityText, "bicicleta", "cadeira", "smartphone", "monitor"),
            "impressora" or "printer" => ContainsAny(identityText, "monitor", "portatil", "smartphone", "teclado", "mouse"),
            "racao" or "cao" or "gato" => ContainsAny(identityText, "fralda", "bebe", "smartphone", "monitor"),
            "fralda" or "fraldas" or "bebe" or "baby" => ContainsAny(identityText, "racao", "cao", "gato", "smartphone", "monitor"),
            _ => false
        };
    }

    private static bool ContainsAny(string value, params string[] terms)
    {
        return terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private static long? TryExtractMaxBudgetCents(string query)
    {
        var normalized = NormalizeText(query);
        var match = BudgetRegex().Match(normalized);
        if (!match.Success)
        {
            return null;
        }

        var rawAmount = match.Groups["amount"].Value.Replace(',', '.');
        return decimal.TryParse(rawAmount, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)
            ? (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero)
            : null;
    }

    private static string RemoveBudgetTerms(string query)
    {
        var cleaned = BudgetRegex().Replace(NormalizeText(query), " ");
        return Regex.Replace(cleaned, @"\s+", " ").Trim();
    }

    private static long ParsePriceCents(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        var match = PriceRegex().Match(value);
        if (!match.Success)
        {
            return 0;
        }

        var amount = match.Groups[1].Value.Replace(" ", string.Empty);
        if (amount.Contains(',', StringComparison.Ordinal))
        {
            amount = amount.Replace(".", string.Empty).Replace(',', '.');
        }
        else if (Regex.IsMatch(amount, @"\.\d{3}$"))
        {
            amount = amount.Replace(".", string.Empty);
        }

        return decimal.TryParse(amount, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? (long)Math.Round(parsed * 100m, MidpointRounding.AwayFromZero)
            : 0;
    }

    private static string FormatCurrency(long cents)
    {
        return $"{(cents / 100m).ToString("N2", CultureInfo.CurrentCulture)} \u20AC";
    }

    private static string GuessBrand(string name)
    {
        var firstWord = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
        return string.IsNullOrWhiteSpace(firstWord) ? "Produto" : firstWord;
    }

    private static string ToDisplayName(string value)
    {
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value.Replace('-', ' '));
    }

    private static string Slugify(string value)
    {
        var normalized = NormalizeText(value);
        var builder = new StringBuilder(normalized.Length);
        var previousDash = false;

        foreach (var character in normalized)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                previousDash = false;
            }
            else if (!previousDash)
            {
                builder.Append('-');
                previousDash = true;
            }
        }

        return builder.ToString().Trim('-');
    }

    private static string NormalizeText(string value)
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

    private static IReadOnlyList<T> Clean<T>(IReadOnlyList<T>? values)
    {
        return values ?? Array.Empty<T>();
    }

    private static string CleanText(string? value, string fallback = "")
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string ProductCacheKey(string slug)
    {
        return $"product-discovery:product:{slug.ToLowerInvariant()}";
    }

    private static string OffersCacheKey(string slug)
    {
        return $"product-discovery:offers:{slug.ToLowerInvariant()}";
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };
        options.Converters.Add(new FlexibleStringConverter());
        options.Converters.Add(new FlexibleStringListConverter());
        options.Converters.Add(new FlexibleOfferListConverter());
        return options;
    }

    [GeneratedRegex(@"(?:ate|at[eé]|under|below|maximo|max|orcamento)\s*(?:de\s*)?(?:eur|euros?|\u20ac)?\s*(?<amount>\d+(?:[\.,]\d{1,2})?)|(?<amount>\d+(?:[\.,]\d{1,2})?)\s*(?:eur|euros?|\u20ac)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BudgetRegex();

    [GeneratedRegex(@"(\d{1,3}(?:[\.\s]\d{3})*(?:,\d{1,2})?|\d+(?:[\.,]\d{1,2})?)", RegexOptions.CultureInvariant)]
    private static partial Regex PriceRegex();

    private sealed class DiscoveryPayload
    {
        public string? Category { get; set; }

        public double Confidence { get; set; }

        public IReadOnlyList<DiscoveryProductPayload>? Products { get; set; }
    }

    private sealed class DiscoveryProductPayload
    {
        public string? Name { get; set; }

        public string? Brand { get; set; }

        public string? Price { get; set; }

        public decimal PriceCents { get; set; }

        public double Score { get; set; }

        public string? Badge { get; set; }

        public IReadOnlyList<string>? Specs { get; set; }

        public IReadOnlyList<string>? Highlights { get; set; }

        public IReadOnlyList<string>? VerificationChecks { get; set; }

        public string? Summary { get; set; }

        public IReadOnlyList<string>? Warnings { get; set; }

        public IReadOnlyList<DiscoveryOfferPayload>? SellerOffers { get; set; }
    }

    private sealed class DiscoveryOfferPayload
    {
        public string? Seller { get; set; }

        public string? Price { get; set; }

        public decimal PriceCents { get; set; }

        public string? Delivery { get; set; }

        public string? Warranty { get; set; }

        public string? Status { get; set; }

        public string? Url { get; set; }

        public bool Preferred { get; set; }
    }

    private sealed class FlexibleStringConverter : JsonConverter<string?>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.String => reader.GetString(),
                JsonTokenType.Number => reader.TryGetInt64(out var longValue)
                    ? longValue.ToString(CultureInfo.InvariantCulture)
                    : reader.GetDecimal().ToString(CultureInfo.InvariantCulture),
                JsonTokenType.True => bool.TrueString,
                JsonTokenType.False => bool.FalseString,
                JsonTokenType.Null => null,
                _ => JsonDocument.ParseValue(ref reader).RootElement.ToString()
            };
        }

        public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }

    private sealed class FlexibleStringListConverter : JsonConverter<IReadOnlyList<string>?>
    {
        public override IReadOnlyList<string>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            using var document = JsonDocument.ParseValue(ref reader);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Array)
            {
                return root.EnumerateArray()
                    .Select(ElementToString)
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .ToList();
            }

            return new[] { ElementToString(root) };
        }

        public override void Write(Utf8JsonWriter writer, IReadOnlyList<string>? value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, options);
        }

        private static string ElementToString(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString() ?? string.Empty,
                JsonValueKind.Number => element.GetRawText(),
                JsonValueKind.True => bool.TrueString,
                JsonValueKind.False => bool.FalseString,
                JsonValueKind.Null => string.Empty,
                _ => element.ToString()
            };
        }
    }

    private sealed class FlexibleOfferListConverter : JsonConverter<IReadOnlyList<DiscoveryOfferPayload>?>
    {
        public override IReadOnlyList<DiscoveryOfferPayload>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            using var document = JsonDocument.ParseValue(ref reader);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Array)
            {
                return root.EnumerateArray()
                    .Select(element => JsonSerializer.Deserialize<DiscoveryOfferPayload>(element.GetRawText(), options))
                    .Where(offer => offer is not null)
                    .Cast<DiscoveryOfferPayload>()
                    .ToList();
            }

            if (root.ValueKind == JsonValueKind.Object)
            {
                var offer = JsonSerializer.Deserialize<DiscoveryOfferPayload>(root.GetRawText(), options);
                return offer is null ? Array.Empty<DiscoveryOfferPayload>() : new[] { offer };
            }

            return Array.Empty<DiscoveryOfferPayload>();
        }

        public override void Write(Utf8JsonWriter writer, IReadOnlyList<DiscoveryOfferPayload>? value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, options);
        }
    }
}
