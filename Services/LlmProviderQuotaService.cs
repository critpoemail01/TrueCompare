using TrueCompare.Options;

namespace TrueCompare.Services;

public sealed class LlmProviderQuotaService
{
    private readonly object gate = new();
    private readonly Dictionary<string, ProviderQuotaState> states = new(StringComparer.OrdinalIgnoreCase);

    public bool TryReserve(LlmProviderOptions provider, out string reason)
    {
        var now = DateTimeOffset.UtcNow;
        var state = GetState(provider.Name);

        lock (gate)
        {
            if (state.BlockedUntilUtc is not null && state.BlockedUntilUtc > now)
            {
                reason = $"blocked until {state.BlockedUntilUtc:O}";
                return false;
            }

            ResetWindowsIfNeeded(state, now);

            if (provider.RequestsPerMinute > 0 && state.MinuteCount >= provider.RequestsPerMinute)
            {
                state.BlockedUntilUtc = state.MinuteStartedUtc.AddMinutes(1);
                reason = "minute limit reached";
                return false;
            }

            if (provider.RequestsPerDay > 0 && state.DayCount >= provider.RequestsPerDay)
            {
                state.BlockedUntilUtc = state.DayStartedUtc.AddDays(1);
                reason = "daily limit reached";
                return false;
            }

            state.MinuteCount++;
            state.DayCount++;
            reason = string.Empty;
            return true;
        }
    }

    public void MarkRateLimited(string providerName, TimeSpan? retryAfter = null)
    {
        MarkUnavailable(providerName, retryAfter ?? TimeSpan.FromMinutes(1));
    }

    public void MarkTemporarilyUnavailable(string providerName, TimeSpan cooldown)
    {
        MarkUnavailable(providerName, cooldown);
    }

    private void MarkUnavailable(string providerName, TimeSpan cooldown)
    {
        if (string.IsNullOrWhiteSpace(providerName))
        {
            return;
        }

        var state = GetState(providerName);
        lock (gate)
        {
            state.BlockedUntilUtc = DateTimeOffset.UtcNow.Add(cooldown);
        }
    }

    private ProviderQuotaState GetState(string providerName)
    {
        lock (gate)
        {
            if (!states.TryGetValue(providerName, out var state))
            {
                state = new ProviderQuotaState(DateTimeOffset.UtcNow);
                states[providerName] = state;
            }

            return state;
        }
    }

    private static void ResetWindowsIfNeeded(ProviderQuotaState state, DateTimeOffset now)
    {
        if (now - state.MinuteStartedUtc >= TimeSpan.FromMinutes(1))
        {
            state.MinuteStartedUtc = now;
            state.MinuteCount = 0;
        }

        if (now.Date > state.DayStartedUtc.Date)
        {
            state.DayStartedUtc = now;
            state.DayCount = 0;
        }
    }

    private sealed class ProviderQuotaState(DateTimeOffset now)
    {
        public DateTimeOffset MinuteStartedUtc { get; set; } = now;

        public int MinuteCount { get; set; }

        public DateTimeOffset DayStartedUtc { get; set; } = now;

        public int DayCount { get; set; }

        public DateTimeOffset? BlockedUntilUtc { get; set; }
    }
}
