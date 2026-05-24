namespace TrueCompare.Options;

public sealed class PriceAlertOptions
{
    public int MaxActiveAlertsPerUser { get; set; } = 5;

    public int MaxEmailFailures { get; set; } = 5;

    public int FirstEmailRetryMinutes { get; set; } = 15;

    public int MaxEmailRetryHours { get; set; } = 24;
}
