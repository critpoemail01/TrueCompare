using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TrueCompare.Data;
using TrueCompare.Models;
using TrueCompare.Options;

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
            var alertService = scope.ServiceProvider.GetRequiredService<TargetPriceAlertService>();
            var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
            var alertOptions = scope.ServiceProvider.GetRequiredService<IOptions<PriceAlertOptions>>().Value;

            var now = DateTime.UtcNow;
            var alerts = await dbContext.TargetPriceAlerts
                .Include(alert => alert.User)
                .Where(alert => alert.IsActive
                    && !alert.EmailSent
                    && alert.EmailSuppressedUtc == null
                    && (alert.NextEmailRetryUtc == null || alert.NextEmailRetryUtc <= now))
                .ToListAsync(cancellationToken);

            foreach (var productGroup in alerts.GroupBy(alert => alert.ProductSlug))
            {
                var bestOffer = await alertService.GetBestLiveValidatedOfferAsync(
                    productGroup.Key,
                    cancellationToken);

                if (bestOffer is null)
                {
                    MarkValidationFailed(productGroup, productGroup.Key);
                    continue;
                }

                foreach (var alert in productGroup)
                {
                    UpdateAlertWithOffer(alert, bestOffer);
                    if (!ShouldSendAlert(alert, bestOffer))
                    {
                        continue;
                    }

                    await TrySendAlertEmailAsync(alert, bestOffer, emailSender, alertOptions, cancellationToken);
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

    private void MarkValidationFailed(IEnumerable<TargetPriceAlert> alerts, string productSlug)
    {
        var validatedUtc = DateTime.UtcNow;
        foreach (var alert in alerts)
        {
            alert.LastValidationState = "ValidationFailed";
            alert.LastValidatedUtc = validatedUtc;
        }

        logger.LogDebug(
            "Price alerts for {ProductSlug} skipped because no live-validated offer was available.",
            productSlug);
    }

    private static void UpdateAlertWithOffer(TargetPriceAlert alert, SellerOffer bestOffer)
    {
        alert.LastSeenPriceCents = bestOffer.PriceCents;
        alert.LastSeenSeller = bestOffer.Seller;
        alert.ProductUrl = bestOffer.Url;
        alert.LastValidationState = bestOffer.ValidationState.ToString();
        alert.LastValidatedUtc = bestOffer.ValidatedUtc ?? DateTime.UtcNow;
    }

    private static bool ShouldSendAlert(TargetPriceAlert alert, SellerOffer bestOffer)
    {
        return bestOffer.IsLiveValidated
            && bestOffer.PriceCents <= alert.TargetPriceCents
            && !string.IsNullOrWhiteSpace(alert.User.Email)
            && alert.User.EmailConfirmed;
    }

    private async Task TrySendAlertEmailAsync(
        TargetPriceAlert alert,
        SellerOffer bestOffer,
        IEmailSender emailSender,
        PriceAlertOptions alertOptions,
        CancellationToken cancellationToken)
    {
        alert.LastEmailAttemptUtc = DateTime.UtcNow;

        try
        {
            var sent = await emailSender.SendAsync(
                alert.User.Email!,
                PriceAlertEmailTemplate.BuildSubject(alert.ProductName),
                PriceAlertEmailTemplate.Build(
                    new PriceAlertEmailModel(alert.ProductName, alert.TargetPriceCents),
                    bestOffer),
                cancellationToken);

            if (sent)
            {
                alert.EmailSent = true;
                alert.IsActive = false;
                alert.TriggeredUtc = DateTime.UtcNow;
                alert.LastEmailError = null;
                alert.EmailFailureCount = 0;
                alert.NextEmailRetryUtc = null;
                alert.EmailSuppressedUtc = null;
                return;
            }

            RegisterEmailFailure(alert, "Email sender returned false.", alertOptions);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            RegisterEmailFailure(alert, exception.Message, alertOptions);
            logger.LogWarning(
                exception,
                "Failed to send price alert email for alert {AlertId} and product {ProductSlug}.",
                alert.Id,
                alert.ProductSlug);
        }
    }

    private static void RegisterEmailFailure(TargetPriceAlert alert, string message, PriceAlertOptions options)
    {
        alert.EmailFailureCount++;
        alert.LastEmailError = TrimEmailError(message);

        if (alert.EmailFailureCount >= Math.Max(1, options.MaxEmailFailures))
        {
            alert.EmailSuppressedUtc = DateTime.UtcNow;
            alert.NextEmailRetryUtc = null;
            alert.IsActive = false;
            return;
        }

        alert.NextEmailRetryUtc = DateTime.UtcNow.Add(CalculateRetryDelay(alert.EmailFailureCount, options));
    }

    private static TimeSpan CalculateRetryDelay(int failureCount, PriceAlertOptions options)
    {
        var firstDelayMinutes = Math.Max(1, options.FirstEmailRetryMinutes);
        var maxDelay = TimeSpan.FromHours(Math.Max(1, options.MaxEmailRetryHours));
        var exponentialFactor = Math.Pow(2, Math.Max(0, failureCount - 1));
        var delay = TimeSpan.FromMinutes(firstDelayMinutes * exponentialFactor);
        return delay <= maxDelay ? delay : maxDelay;
    }

    private static string TrimEmailError(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "Email sending failed.";
        }

        var trimmed = message.Trim();
        return trimmed.Length <= 500 ? trimmed : trimmed[..500];
    }
}
