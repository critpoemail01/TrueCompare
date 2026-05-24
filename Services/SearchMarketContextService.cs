using System.Globalization;

namespace TrueCompare.Services;

public sealed class SearchMarketContextService(AppText text)
{
    public SearchMarketContext Current()
    {
        var culture = CultureInfo.CurrentUICulture;
        var usesUsMarket = culture.Name.Equals("en-US", StringComparison.OrdinalIgnoreCase)
            || culture.Name.EndsWith("-US", StringComparison.OrdinalIgnoreCase);

        return usesUsMarket
            ? new SearchMarketContext(
                text.Pick("Estados Unidos", "United States"),
                "US",
                CultureLabel(culture, "en-US"),
                "USD",
                "Best Buy, Walmart, Amazon.com, Apple US, B&H, Newegg",
                text.Pick("A procurar nos Estados Unidos", "Searching in the United States"))
            : new SearchMarketContext(
                text.Pick("Portugal", "Portugal"),
                "PT",
                CultureLabel(culture, "pt-PT"),
                "EUR",
                text.Pick(
                    "Worten, FNAC, PCDIGA, PcComponentes, KuantoKusta, Decathlon, Leroy Merlin, IKEA, Continente, Wells, Norauto, Staples",
                    "Worten, FNAC, PCDIGA, PcComponentes, KuantoKusta, Decathlon, Leroy Merlin, IKEA, Continente, Wells, Norauto, Staples"),
                text.Pick("A procurar em Portugal", "Searching in Portugal"));
    }

    private static string CultureLabel(CultureInfo culture, string fallback)
    {
        return string.IsNullOrWhiteSpace(culture.Name) ? fallback : culture.Name;
    }
}

public sealed record SearchMarketContext(
    string Location,
    string MarketCode,
    string LanguageCode,
    string CurrencyCode,
    string StoreExamples,
    string Heading);
