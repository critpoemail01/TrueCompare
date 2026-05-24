using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using TrueCompare.Options;

namespace TrueCompare.Services;

public sealed class StoreValidationLimiter
{
    private static readonly TimeSpan DomainWindowDuration = TimeSpan.FromMinutes(1);

    private readonly StoreOfferValidationOptions options;
    private readonly SemaphoreSlim globalSemaphore;
    private readonly ConcurrentDictionary<string, DomainState> domains = new(StringComparer.OrdinalIgnoreCase);

    public StoreValidationLimiter(IOptions<StoreOfferValidationOptions> optionsAccessor)
    {
        options = optionsAccessor.Value;
        globalSemaphore = new SemaphoreSlim(Math.Clamp(options.MaxGlobalConcurrentRequests, 1, 32));
    }

    public async Task<IDisposable?> TryAcquireAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        var domain = domains.GetOrAdd(GetHost(uri), _ => new DomainState(DateTime.UtcNow));
        if (!TryAcquireDomainSlot(domain))
        {
            return null;
        }

        try
        {
            await globalSemaphore.WaitAsync(cancellationToken);
            return new StoreValidationLease(globalSemaphore);
        }
        catch
        {
            throw;
        }
    }

    public void RecordDomainFailure(Uri uri)
    {
        var backoffSeconds = Math.Clamp(options.DomainBackoffSeconds, 0, 300);
        if (backoffSeconds <= 0)
        {
            return;
        }

        var domain = domains.GetOrAdd(GetHost(uri), _ => new DomainState(DateTime.UtcNow));
        lock (domain.Sync)
        {
            domain.BackoffUntilUtc = DateTime.UtcNow.AddSeconds(backoffSeconds);
        }
    }

    public void RecordDomainSuccess(Uri uri)
    {
        var domain = domains.GetOrAdd(GetHost(uri), _ => new DomainState(DateTime.UtcNow));
        lock (domain.Sync)
        {
            domain.BackoffUntilUtc = null;
        }
    }

    private bool TryAcquireDomainSlot(DomainState domain)
    {
        var now = DateTime.UtcNow;
        var perMinuteLimit = Math.Clamp(options.MaxRequestsPerDomainPerMinute, 1, 600);

        lock (domain.Sync)
        {
            if (domain.BackoffUntilUtc.HasValue && domain.BackoffUntilUtc.Value > now)
            {
                return false;
            }

            if (now - domain.WindowStartUtc >= DomainWindowDuration)
            {
                domain.WindowStartUtc = now;
                domain.WindowCount = 0;
            }

            if (domain.WindowCount >= perMinuteLimit)
            {
                return false;
            }

            domain.WindowCount++;
            return true;
        }
    }

    private static string GetHost(Uri uri)
    {
        return string.IsNullOrWhiteSpace(uri.Host) ? "unknown" : uri.Host;
    }

    private sealed class DomainState(DateTime startUtc)
    {
        public object Sync { get; } = new();

        public DateTime WindowStartUtc { get; set; } = startUtc;

        public int WindowCount { get; set; }

        public DateTime? BackoffUntilUtc { get; set; }
    }

    private sealed class StoreValidationLease(SemaphoreSlim semaphore) : IDisposable
    {
        private int disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0)
            {
                return;
            }

            semaphore.Release();
        }
    }
}
