using Microsoft.Extensions.Options;
using TrueCompare.Options;

namespace TrueCompare.Services;

public sealed class SearchMaintenanceService(
    IServiceScopeFactory scopeFactory,
    IOptions<SearchQuotaOptions> optionsAccessor,
    ILogger<SearchMaintenanceService> logger) : BackgroundService
{
    private readonly SearchQuotaOptions options = optionsAccessor.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunMaintenanceAsync(stoppingToken);

        var intervalHours = Math.Clamp(options.SnapshotCleanupIntervalHours, 1, 24);
        using var timer = new PeriodicTimer(TimeSpan.FromHours(intervalHours));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunMaintenanceAsync(stoppingToken);
        }
    }

    private async Task RunMaintenanceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var quotaService = scope.ServiceProvider.GetRequiredService<SearchQuotaService>();
            var snapshotService = scope.ServiceProvider.GetRequiredService<SearchResultSnapshotService>();

            var refundedReservations = await quotaService.RefundExpiredPendingReservationsAsync(cancellationToken);
            var deletedSnapshots = await snapshotService.DeleteExpiredSnapshotsAsync(cancellationToken);

            if (refundedReservations > 0 || deletedSnapshots > 0)
            {
                logger.LogInformation(
                    "Search maintenance refunded {RefundedReservations} pending reservations and deleted {DeletedSnapshots} expired snapshots.",
                    refundedReservations,
                    deletedSnapshots);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Search maintenance failed.");
        }
    }
}
