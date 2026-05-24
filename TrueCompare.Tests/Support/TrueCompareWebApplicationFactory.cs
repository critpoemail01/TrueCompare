using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using TrueCompare.Data;
using TrueCompare.Models;
using TrueCompare.Services;

namespace TrueCompare.Tests.Support;

public sealed class TrueCompareWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration(configuration =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=TrueCompareTests;Trusted_Connection=True;TrustServerCertificate=True",
                ["Database:ApplyMigrationsOnStartup"] = "false",
                ["Llm:Enabled"] = "false",
                ["SearchQuota:LocalUnlimitedEnabled"] = "true",
                ["Security:RequireConfirmedEmail"] = "false",
                ["Security:AutoConfirmLocalAccounts"] = "true",
                ["Security:AutoConfirmEmailChanges"] = "true"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();
            services.RemoveAll<IStoreOfferValidationService>();
            services.RemoveAll<IHostedService>();

            var databaseName = $"TrueCompare.Web.{Guid.NewGuid():N}";
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options
                    .UseInMemoryDatabase(databaseName)
                    .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            });
            services.AddSingleton<IStoreOfferValidationService, DeterministicStoreOfferValidationService>();
        });
    }

    private sealed class DeterministicStoreOfferValidationService : IStoreOfferValidationService
    {
        public Task<IReadOnlyList<SellerOffer>> ValidateConfirmedOffersAsync(
            ProductResult? product,
            IReadOnlyList<SellerOffer> offers,
            CancellationToken cancellationToken = default)
        {
            var validatedUtc = DateTime.UtcNow;
            return Task.FromResult((IReadOnlyList<SellerOffer>)offers
                .Where(ComparisonDataService.IsConfirmedStoreOffer)
                .Select(offer => offer with
                {
                    IsLivePrice = true,
                    ValidationState = OfferPriceValidationState.LiveValidated,
                    ValidatedUtc = validatedUtc
                })
                .ToList());
        }
    }
}
