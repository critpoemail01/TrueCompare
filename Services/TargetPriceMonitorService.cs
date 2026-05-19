using Microsoft.EntityFrameworkCore;
using TrueCompare.Data;
using TrueCompare.Models;

namespace TrueCompare.Services;

public sealed class TargetPriceMonitorService(
    IServiceScopeFactory scopeFactory,
    ILogger<TargetPriceMonitorService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(15));
        await CheckAlertsAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await CheckAlertsAsync(stoppingToken);
        }
    }

    private async Task CheckAlertsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var comparisonData = scope.ServiceProvider.GetRequiredService<ComparisonDataService>();
            var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

            var alerts = await dbContext.TargetPriceAlerts
                .Include(alert => alert.User)
                .Where(alert => alert.IsActive && !alert.EmailSent)
                .ToListAsync(cancellationToken);

            foreach (var alert in alerts)
            {
                var bestOffer = comparisonData.GetBestOffer(alert.ProductSlug);
                alert.LastSeenPriceCents = bestOffer.PriceCents;
                alert.LastSeenSeller = bestOffer.Seller;
                alert.ProductUrl = bestOffer.Url;

                if (bestOffer.PriceCents > alert.TargetPriceCents || string.IsNullOrWhiteSpace(alert.User.Email))
                {
                    continue;
                }

                var sent = await emailSender.SendAsync(
                    alert.User.Email,
                    $"Alerta de preço: {alert.ProductName}",
                    BuildEmail(alert, bestOffer),
                    cancellationToken);

                if (sent)
                {
                    alert.EmailSent = true;
                    alert.IsActive = false;
                    alert.TriggeredUtc = DateTime.UtcNow;
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to process target price alerts.");
        }
    }

    private static string BuildEmail(TargetPriceAlert alert, SellerOffer offer)
    {
        var currentPrice = TargetPriceAlertService.FormatCents(offer.PriceCents);
        var targetPrice = TargetPriceAlertService.FormatCents(alert.TargetPriceCents);
        return $"""
            <p>O produto <strong>{alert.ProductName}</strong> chegou ao preço definido.</p>
            <p>Preço alvo: <strong>{targetPrice}</strong><br>
            Melhor preço atual: <strong>{currentPrice}</strong> em <strong>{offer.Seller}</strong>.</p>
            <p><a href="{offer.Url}">Abrir vendedor recomendado</a></p>
            """;
    }
}
