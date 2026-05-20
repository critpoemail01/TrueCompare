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
        var packages = document.RootElement
            .GetProperty("Stripe")
            .GetProperty("CreditPackages")
            .EnumerateArray()
            .ToList();
        var proPackage = packages.Single(package => package.GetProperty("Id").GetString() == "pro");
        var monthly = plans.Single(plan => plan.GetProperty("Id").GetString() == "monthly");
        var annual = plans.Single(plan => plan.GetProperty("Id").GetString() == "annual");

        Assert.Equal("month", monthly.GetProperty("BillingPeriod").GetString());
        Assert.Equal(0, monthly.GetProperty("CreditsPerPeriod").GetInt32());
        Assert.Contains("ilimitado", monthly.GetProperty("Name").GetString(), StringComparison.OrdinalIgnoreCase);
        Assert.True(monthly.GetProperty("AmountCents").GetInt64() < proPackage.GetProperty("AmountCents").GetInt64());

        Assert.Equal("year", annual.GetProperty("BillingPeriod").GetString());
        Assert.Equal(0, annual.GetProperty("CreditsPerPeriod").GetInt32());
        Assert.Contains("ilimitado", annual.GetProperty("Name").GetString(), StringComparison.OrdinalIgnoreCase);
        Assert.True(annual.GetProperty("AmountCents").GetInt64() < monthly.GetProperty("AmountCents").GetInt64() * 12);
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
