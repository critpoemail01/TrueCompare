using System.Globalization;
using System.Text;
using TrueCompare.Services;

namespace TrueCompare.Tests;

public sealed class ProductConversationServiceTests
{
    public ProductConversationServiceTests()
    {
        var culture = CultureInfo.GetCultureInfo("pt-PT");
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("asdasdasd")]
    public void Evaluate_AsksForClarification_WhenPromptDoesNotIdentifyAValidatedProduct(string prompt)
    {
        var service = CreateService();

        var decision = service.Evaluate(prompt);

        Assert.False(decision.CanProceed);
        Assert.Null(decision.ProductName);
        Assert.Contains("produto", decision.AssistantMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(decision.Options);
    }

    [Fact]
    public void Evaluate_ReturnsAdvice_WhenKnownProductHasNoValidatedStore()
    {
        var service = CreateService();

        var decision = service.Evaluate("iphone 15 pro");

        Assert.False(decision.CanProceed);
        Assert.NotEmpty(decision.Options);
        Assert.Contains("opcoes", RemoveDiacritics(decision.AssistantMessage), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_AsksFollowUp_WhenUserIsStillAskingForAdvice()
    {
        var service = CreateService();

        var decision = service.Evaluate("qual smartphone devo comprar?");

        Assert.False(decision.CanProceed);
        Assert.NotEmpty(decision.Options);
        Assert.Contains("orcamento", RemoveDiacritics(decision.AssistantMessage), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_ReturnsFourCoffeeMachineScalesWithValidatedUrls_WhenAskedGenericCoffeeMachineAdvice()
    {
        var service = CreateService();

        var decision = service.Evaluate("Maquinas de Cafe da me os url para comprar, 4 opcoes custo beneficio, topo, mais barato intermedio");
        var message = RemoveDiacritics(decision.AssistantMessage);

        Assert.False(decision.CanProceed);
        Assert.Equal(4, decision.Options.Count);
        Assert.All(decision.Options, option => Assert.True(option.HasConfirmedStore));
        Assert.Contains("4 opcoes", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Mais barata", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Intermedia", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Melhor custo/beneficio", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Topo", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Escolha recomendada", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("URL: https://www.worten.pt/produtos/maquina-de-cafe", decision.AssistantMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(decision.Options, option => option.Name.Contains("Essenza", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(decision.Options, option => option.Name.Contains("Magnifica", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain("PlayStation", decision.AssistantMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_AllowsNextStep_WhenCoffeeCategoryPromptAsksForComparison()
    {
        var service = CreateService();

        var decision = service.Evaluate("Maquinas de cafe: melhor preco, vendedores verificados, baixo risco.");

        Assert.True(decision.CanProceed);
        Assert.NotNull(decision.ProductName);
        Assert.Contains("valid", decision.AssistantMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(decision.Options);
    }

    [Fact]
    public void Evaluate_ReturnsFridgeOptionsWithValidatedUrlsAndFollowUp_WhenAskedGenericFridgeAdvice()
    {
        var service = CreateService();

        var decision = service.Evaluate("Frigorificos da me os url para comprar");
        var message = RemoveDiacritics(decision.AssistantMessage);

        Assert.False(decision.CanProceed);
        Assert.Equal(4, decision.Options.Count);
        Assert.All(decision.Options, option => Assert.True(option.HasConfirmedStore));
        Assert.Contains("frigorificos", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Links diretos", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Melhor escolha preco/qualidade", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("familia", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Para afinar", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("URL: https://www.worten.pt/produtos/frigorifico", decision.AssistantMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(decision.Options, option => option.Name.Contains("Samsung", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(decision.Options, option => option.Name.Contains("Bosch", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain("PlayStation", decision.AssistantMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_ReturnsPracticalAdviceAndOptions_ForIndustrialTablet()
    {
        var service = CreateService();

        var decision = service.Evaluate("tablet industrial");

        Assert.False(decision.CanProceed);
        Assert.Contains("tablet industrial", RemoveDiacritics(decision.AssistantMessage), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Windows", decision.AssistantMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Android", decision.AssistantMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("RFID", decision.AssistantMessage, StringComparison.OrdinalIgnoreCase);
        var getac = Assert.Single(decision.Options, option => option.Name == "Getac UX10 G3");
        Assert.Equal("getac-ux10-g3", getac.Slug);
        Assert.False(getac.HasConfirmedStore);
        Assert.Contains(decision.Options, option => option.Name.Contains("Zebra", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("microondas", "Teka MW FS20")]
    [InlineData("rato gaming logitech g305 lightspeed", "Logitech G305 Lightspeed")]
    [InlineData("Smartphones premium: melhor preco, vendedores verificados, baixo risco.", "iPhone 17 Pro")]
    public void Evaluate_AllowsNextStep_WhenPromptHasProductWithConfirmedStore(string prompt, string expectedProduct)
    {
        var service = CreateService();

        var decision = service.Evaluate(prompt);

        Assert.True(decision.CanProceed);
        Assert.Equal(prompt, decision.ResolvedQuery);
        Assert.Contains(expectedProduct, decision.ProductName, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("valid", decision.AssistantMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(decision.Options);
    }

    private static ProductConversationService CreateService()
    {
        var text = new AppText();
        return new ProductConversationService(new ComparisonDataService(text), text);
    }

    private static string RemoveDiacritics(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
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
}
