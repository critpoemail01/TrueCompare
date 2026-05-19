using System.Text.Json;
using TrueCompare.Data;
using TrueCompare.Tests.Support;

namespace TrueCompare.Tests;

public sealed class SubscriptionBillingTests
{
    [Fact]
    public void AppSettings_DefinesMonthlyAndAnnualSubscriptionPlans()
    {
        var appSettingsPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "appsettings.json"));
        using var document = JsonDocument.Parse(File.ReadAllText(appSettingsPath));

        var plans = document.RootElement
            .GetProperty("Stripe")
            .GetProperty("SubscriptionPlans")
            .EnumerateArray()
            .ToList();

        Assert.Contains(plans, plan =>
            plan.GetProperty("Id").GetString() == "monthly"
            && plan.GetProperty("BillingPeriod").GetString() == "month"
            && plan.GetProperty("CreditsPerPeriod").GetInt32() > 0);
        Assert.Contains(plans, plan =>
            plan.GetProperty("Id").GetString() == "annual"
            && plan.GetProperty("BillingPeriod").GetString() == "year"
            && plan.GetProperty("AmountCents").GetInt64() > 0);
    }

    [Fact]
    public void CreditPurchase_UsesOneTimeBillingKindByDefault()
    {
        using var dbContext = TestDbContextFactory.Create();

        var entity = dbContext.Model.FindEntityType(typeof(CreditPurchase));
        var property = entity?.FindProperty(nameof(CreditPurchase.BillingKind));

        Assert.NotNull(property);
        Assert.Equal(
            CreditBillingKind.OneTime,
            property.GetAnnotations().Single(annotation => annotation.Name == "Relational:DefaultValue").Value);
    }
}
