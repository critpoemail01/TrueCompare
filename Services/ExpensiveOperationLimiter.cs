using System.Collections.Concurrent;

namespace TrueCompare.Services;

public sealed class ExpensiveOperationLimiter
{
    private readonly ConcurrentDictionary<string, WindowCounter> counters = new(StringComparer.OrdinalIgnoreCase);

    public bool TryAcquire(string scope, string subject, int permitLimit, TimeSpan window)
    {
        if (permitLimit <= 0)
        {
            return false;
        }

        var key = $"{scope}:{subject}";
        var now = DateTimeOffset.UtcNow;
        var counter = counters.GetOrAdd(key, _ => new WindowCounter(now));

        lock (counter.SyncRoot)
        {
            if (now - counter.WindowStartedUtc >= window)
            {
                counter.WindowStartedUtc = now;
                counter.Count = 0;
            }

            if (counter.Count >= permitLimit)
            {
                return false;
            }

            counter.Count++;
            return true;
        }
    }

    private sealed class WindowCounter(DateTimeOffset windowStartedUtc)
    {
        public object SyncRoot { get; } = new();

        public DateTimeOffset WindowStartedUtc { get; set; } = windowStartedUtc;

        public int Count { get; set; }
    }
}
