using System.Globalization;
using TrueCompare.Services;

namespace TrueCompare.ReliabilityGate;

public sealed class ApplicationRecommendationProvider
{
    private readonly AppText text = new();
    private readonly ComparisonDataService data;
    private readonly ProductConversationService conversation;

    public ApplicationRecommendationProvider()
    {
        data = new ComparisonDataService(text);
        conversation = new ProductConversationService(data, text);
    }

    public AppRecommendation Ask(string prompt)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            var culture = CultureInfo.GetCultureInfo("pt-PT");
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            var decision = conversation.Evaluate(prompt);
            var products = new List<AppRecommendedProduct>();
            if (decision.Options.Count > 0)
            {
                products.AddRange(decision.Options.Select(option => new AppRecommendedProduct(
                    option.Name,
                    option.Brand,
                    option.Price,
                    ReliabilityNormalizer.ParsePriceCents(option.Price),
                    option.Badge,
                    option.Summary)));
            }
            else
            {
                var query = decision.ProductName ?? prompt;
                products.AddRange(data.GetProducts(query).Take(4).Select(product => new AppRecommendedProduct(
                    product.Name,
                    product.Brand,
                    product.Price,
                    ReliabilityNormalizer.ParsePriceCents(product.Price),
                    product.Badge,
                    product.AiSummary)));
            }

            return new AppRecommendation(
                EvidenceStatus.Validated,
                prompt,
                decision.AssistantMessage,
                DateTimeOffset.UtcNow,
                products);
        }
        catch (Exception ex)
        {
            return new AppRecommendation(
                EvidenceStatus.Blocked,
                prompt,
                string.Empty,
                DateTimeOffset.UtcNow,
                Array.Empty<AppRecommendedProduct>(),
                BlockReason: ex.Message);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }
}
