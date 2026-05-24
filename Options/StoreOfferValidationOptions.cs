namespace TrueCompare.Options;

public sealed class StoreOfferValidationOptions
{
    public int RequestTimeoutSeconds { get; set; } = 8;

    public int MaxConcurrentRequests { get; set; } = 3;

    public int MaxGlobalConcurrentRequests { get; set; } = 8;

    public int DomainBackoffSeconds { get; set; } = 15;

    public int MaxRequestsPerDomainPerMinute { get; set; } = 30;

    public int MaxOffersPerProduct { get; set; } = 6;

    public int MaxResponseBytes { get; set; } = 1_000_000;
}
