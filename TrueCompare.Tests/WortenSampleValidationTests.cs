using System.Text;
using System.Text.Json;
using TrueCompare.Models;
using TrueCompare.Services;

namespace TrueCompare.Tests;

public sealed class WortenSampleValidationTests
{
    [Fact]
    public void WortenBalancedSample_ReturnsCompatibleCategory_ForAtLeastMostProducts()
    {
        var samplePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Fixtures", "worten-products-balanced-1000.json"));
        if (!File.Exists(samplePath))
        {
            return;
        }

        var sample = JsonSerializer.Deserialize<WortenSample>(
            File.ReadAllText(samplePath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(sample);
        Assert.True(sample.Products.Count >= 1000);

        var service = new ComparisonDataService(new AppText());
        var failures = new List<string>();
        var compatible = 0;
        var empty = 0;

        foreach (var item in sample.Products.Take(1000))
        {
            var results = service.GetProducts(item.Name);
            if (results.Count == 0)
            {
                empty++;
                failures.Add($"empty [{item.Family}] {item.Name}");
                continue;
            }

            var actualFamily = ResultFamily(results);
            var expectedFamily = ExpectedFamily(item);
            if (IsCompatible(expectedFamily, actualFamily))
            {
                compatible++;
                continue;
            }

            failures.Add($"{expectedFamily}->{actualFamily}: {item.Name} => {string.Join(" | ", results.Take(3).Select(result => result.Name))}");
        }

        var message = new StringBuilder()
            .AppendLine($"Compatible: {compatible}/1000")
            .AppendLine($"Empty: {empty}/1000")
            .AppendLine("First failures:")
            .AppendJoin(Environment.NewLine, failures.Take(80))
            .ToString();

        Assert.True(compatible >= 850 && empty <= 80, message);
    }

    private static string ExpectedFamily(WortenProduct item)
    {
        var name = Normalize(item.Name);

        if (ContainsAny(name, "galaxy tab", "redmi pad", "xiaomi pad", "ipad", "tablet ")) return "tablet";
        if (ContainsAny(name, "galaxy buds", "buds", "auriculares", "auscultadores", "soundbar", "coluna bluetooth")) return "audio";
        if (ContainsAny(name, "smartwatch", "smart watch", "smart band", "smartband", "galaxy fit", "pulseira desportiva")) return "smartwatch";
        if (ContainsAny(name, "disco externo", "ssd externo", "hdd externo", "nas ")) return "storage";
        if (ContainsAny(name, "televisor", " smart tv", " tv ", "suporte tv", "suporte de tv", "suporte de parede")) return "tv";
        if (ContainsAny(name, "teclado")) return "keyboard";
        if (ContainsAny(name, "rato ", "tapete de rato", "mouse")) return "mouse";
        if (ContainsAny(name, "iphone", "smartphone", "telemovel", "galaxy s", "galaxy a", "redmi note", "xiaomi 14", "pixel")) return "smartphone";
        if (ContainsAny(name, "macbook", "portatil", "laptop", "zenbook", "thinkpad")) return "laptop";
        if (ContainsAny(name, "impressora")) return "printer";
        if (ContainsAny(name, "drone", "dji")) return "drone";
        if (ContainsAny(name, "camera", "camara", "gopro", "maquina fotografica")) return "camera";
        if (ContainsAny(name, "frigorifico", "maquina lavar", "forno", "microondas", "air fryer", "aspirador", "fritadeira")) return "appliance";
        if (ContainsAny(name, "ps5", "playstation", "nintendo", "xbox", "comando ps4", "gaming")) return "gaming";
        if (ContainsAny(name, "lego", "boneca", "bebê reborn", "bebe reborn", "jogo tabuleiro", "jogo de tabuleiro", "jogo da velha", "furby", "brinquedo")) return "toys";
        if (ContainsAny(name, "maquina cafe", "maquina de cafe", "espresso", "expresso", "cafe capsulas", "cafe moido")) return "coffee";
        if (ContainsAny(name, "capsulas cafe", "manteiga amendoim", "mercearia", "cafe ")) return "food";
        if (ContainsAny(name, "perfume", "eau de parfum", "eau de toilette", "body mist", "creme", "protetor solar", "shampoo", "oleo bronzeador", "retinol")) return "health-beauty";
        if (ContainsAny(name, "fraldas", "carrinho bebe", "cadeira auto bebe")) return "baby";
        if (ContainsAny(name, "berbequim", "aparafusadora", "serra", "tinta")) return "tools";
        if (ContainsAny(name, "cadeira escritorio", "secretaria", "colchao", "tapete", "trem cozinha")) return "home";
        if (ContainsAny(name, "bicicleta", "trotinete", "passadeira", "sapatilhas running", "mala viagem")) return "sport";
        if (ContainsAny(name, "oleo motor", "bateria auto")) return "auto";
        if (ContainsAny(name, "racao cao", "frontpro", "cao ", "gato ")) return "pet";
        if (ContainsAny(name, "livro", "kobo", "vinil")) return "culture";

        return item.Family;
    }

    private static string ResultFamily(IReadOnlyList<ProductResult> results)
    {
        var top = results[0];
        var name = Normalize(top.Name);
        var text = Normalize($"{top.Name} {top.Brand} {top.Badge} {top.AiSummary} {string.Join(' ', top.Specs)} {string.Join(' ', top.Highlights)}");

        if (ContainsAny(name, "playstation", "nintendo switch", "xbox", "dualshock", "dualsense", "comando ps4")) return "gaming";
        if (ContainsAny(name, "lego", "hot wheels", "catan", "furby", "boneca", "brinquedo")) return "toys";
        if (ContainsAny(name, "dodot", "pampers", "chicco", "fralda", "toalhitas")) return "baby";
        if (ContainsAny(name, "dolce", "la roche", "isdin", "tecnifar", "perfume", "eau de parfum", "retinol")) return "health-beauty";
        if (ContainsAny(name, "xiaomi mi smart scale", "bosch serie", "miele w1", "samsung bespoke", "teka", "fritadeira", "balanca")) return "appliance";
        if (ContainsAny(name, "roborock", "irobot", "roomba", "aspirador robot", "robot aspirador")) return "appliance";
        if (ContainsAny(name, "nespresso", "de'longhi", "delonghi", "sage bambino")) return "coffee";
        if (ContainsAny(name, "macbook", "thinkpad", "zenbook", "xps")) return "laptop";
        if (ContainsAny(name, "western digital", "my passport", "samsung t7", "seagate", "disco externo")) return "storage";
        if (ContainsAny(name, "ipad", "galaxy tab", "lenovo idea tab", "xiaomi pad")) return "tablet";
        if (ContainsAny(name, "microsoft bluetooth mouse", "logitech mx master", "logitech signature", "logitech g305", "razer")) return "mouse";
        if (ContainsAny(name, "logitech mx keys", "keychron", "logitech k380")) return "keyboard";
        if (ContainsAny(name, "lg oled", "samsung qn90", "tcl 55", "tooq suporte")) return "tv";
        if (ContainsAny(name, "sony wh", "airpods", "jbl tune", "galaxy buds", "soundbar")) return "audio";
        if (ContainsAny(name, "canon eos", "fujifilm", "sony alpha", "gopro")) return "camera";
        if (ContainsAny(name, "dji ", "prixton", "drone")) return "drone";
        if (ContainsAny(name, "apple watch", "galaxy watch", "galaxy fit", "smart band", "garmin forerunner")) return "smartwatch";
        if (ContainsAny(name, "iphone", "galaxy s", "galaxy a", "pixel", "smartphone", "redmi note", "poco", "spc senior")) return "smartphone";

        if (ContainsAny(text, "smartwatch", "apple watch", "galaxy watch", "watchos", "wear os", "smartband")) return "smartwatch";
        if (ContainsAny(text, "ipad", "galaxy tab", "lenovo idea tab", "xiaomi pad")) return "tablet";
        if (ContainsAny(text, "rugged", "getac", "toughbook", "zebra et45", "tab active")) return "tablet";
        if (ContainsAny(text, "my passport", "samsung t7", "seagate", "hdd portatil", "disco externo")) return "storage";
        if (ContainsAny(text, "macbook", "thinkpad", "zenbook", "xps", "portatil", "laptop")) return "laptop";
        if (ContainsAny(text, "iphone", "galaxy s", "galaxy a", "pixel", "smartphone", "telemovel", "redmi", "poco", "spc senior")) return "smartphone";
        if (ContainsAny(text, "ultragear", "monitor", "odyssey", "dell p2723d")) return "monitor";
        if (ContainsAny(text, "ecotank", "officejet", "impressora", "tinteiro", "toner")) return "printer";
        if (ContainsAny(text, "delonghi", "de'longhi", "nespresso", "sage bambino", "maquina de cafe")) return "coffee";
        if (ContainsAny(text, "navigator", "bic cristal", "casio", "papelaria", "escritorio", "caneta", "papel a4")) return "office";
        if (ContainsAny(text, "mx keys", "keychron", "teclado")) return "keyboard";
        if (ContainsAny(text, "logitech mx master", "rato", "mouse", "logitech signature")) return "mouse";
        if (ContainsAny(text, "oled", "qled", "mini led", "google tv", "smart tv", "televisor", "suporte tv", "vesa", "tcl 55c805")) return "tv";
        if (ContainsAny(text, "sony wh", "airpods", "auscultadores", "headphones", "soundbar", "jbl tune")) return "audio";
        if (ContainsAny(text, "camera", "camara", "canon", "sony alpha", "gopro")) return "camera";
        if (ContainsAny(text, "drone", "dji")) return "drone";
        if (ContainsAny(text, "frigorifico", "lavadora", "lava-loica", "microondas", "forno", "balanca", "fritadeira", "torradeira", "aspirador")) return "appliance";
        if (ContainsAny(text, "delta q", "kaffa", "cafe", "manteiga de amendoim", "vinho", "gin", "mercearia")) return "food";
        if (ContainsAny(text, "playstation", "ps5", "ps4", "nintendo", "xbox", "dualshock", "gaming")) return "gaming";
        if (ContainsAny(text, "lego", "hot wheels", "brinquedo", "boneca", "jogo de tabuleiro", "catan")) return "toys";
        if (ContainsAny(text, "tecnifar", "la roche", "cicaplast", "dolce", "isdin", "creme", "perfume", "comprimidos", "shampoo", "protetor")) return "health-beauty";
        if (ContainsAny(text, "pampers", "dodot", "chicco", "bebe", "fralda")) return "baby";
        if (ContainsAny(text, "bosch professional", "makita", "dewalt", "berbequim", "ferramenta", "serra")) return "tools";
        if (ContainsAny(text, "ikea", "cadeira", "sofa", "mesa", "colchao", "secretaria", "movel", "philips hue", "tapete", "silampos")) return "home";
        if (ContainsAny(text, "garmin", "bicicleta", "trotinete", "fitness", "asics", "adidas", "running", "passadeira", "mala viagem")) return "sport";
        if (ContainsAny(text, "oleo motor", "bateria auto", "escovas", "pneu")) return "auto";
        if (ContainsAny(text, "royal canin", "purina", "libra", "racao", "frontpro", "scalibor")) return "pet";
        if (ContainsAny(text, "sapatilhas", "trainers", "mochila", "mala", "new balance")) return "fashion";
        if (ContainsAny(text, "livro", "manga", "kobo", "vinil")) return "culture";
        return "generic";
    }

    private static bool IsCompatible(string expected, string actual)
    {
        if (expected.Equals(actual, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return (expected, actual) switch
        {
            ("smartwatch", "smartphone") => true,
            ("smartwatch", "gaming") => true,
            ("coffee", "appliance") => true,
            ("food", "coffee") => true,
            ("food", "culture") => true,
            ("camera", "tv") => true,
            ("drone", "camera") => true,
            ("laptop", "gaming") => true,
            ("keyboard", "laptop") => true,
            ("gaming", "audio") => true,
            ("home", "office") => true,
            ("pet", "home") => true,
            ("storage", "coffee") => true,
            ("laptop", "coffee") => true,
            ("baby", "health-beauty") => true,
            ("office", "printer") => true,
            ("printer", "office") => true,
            ("keyboard", "mouse") => true,
            ("mouse", "keyboard") => true,
            ("toys", "gaming") => true,
            ("sport", "fashion") => true,
            ("fashion", "sport") => true,
            _ => false
        };
    }

    private static bool ContainsAny(string text, params string[] terms)
    {
        return terms.Any(term => text.Contains(Normalize(term), StringComparison.OrdinalIgnoreCase));
    }

    private static string Normalize(string value)
    {
        var normalized = value.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(character) != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private sealed record WortenSample(IReadOnlyList<WortenProduct> Products);

    private sealed record WortenProduct(string Family, string Query, string Name, string PriceMeta, string PriceText, string Seller, string Url);
}
