using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TrueCompare.Data;
using TrueCompare.Models;
using TrueCompare.Options;

namespace TrueCompare.Services;

public sealed class TargetPriceAlertService(
    ApplicationDbContext dbContext,
    ComparisonDataService comparisonData,
    IStoreOfferValidationService? storeOfferValidator = null,
    IOptions<PriceAlertOptions>? optionsAccessor = null)
{
    public async Task<CreatedPriceAlert> CreateAsync(
        string userId,
        string productSlug,
        string productName,
        decimal targetPrice,
        CancellationToken cancellationToken = default)
    {
        var options = optionsAccessor?.Value ?? new PriceAlertOptions();
        var activeAlerts = await CountActiveForUserAsync(userId, cancellationToken);
        if (activeAlerts >= options.MaxActiveAlertsPerUser)
        {
            throw new PriceAlertCreationException(PriceAlertCreationError.ActiveAlertLimitReached);
        }

        var selectedOffer = await GetBestOfferForAlertCreationAsync(productSlug, cancellationToken)
            ?? throw new PriceAlertCreationException(PriceAlertCreationError.NoConfirmedStoreOffer);
        var alert = new TargetPriceAlert
        {
            UserId = userId,
            ProductSlug = productSlug,
            ProductName = productName,
            TargetPriceCents = ToCents(targetPrice),
            LastSeenPriceCents = selectedOffer.PriceCents,
            LastSeenSeller = selectedOffer.Seller,
            ProductUrl = selectedOffer.Url,
            LastValidationState = selectedOffer.ValidationState.ToString(),
            LastValidatedUtc = selectedOffer.ValidatedUtc
        };

        dbContext.TargetPriceAlerts.Add(alert);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new CreatedPriceAlert(
            alert.ProductSlug,
            alert.ProductName,
            alert.TargetPriceCents,
            alert.LastSeenPriceCents,
            alert.LastSeenSeller,
            alert.ProductUrl,
            alert.LastValidationState,
            alert.LastValidatedUtc,
            alert.IsActive,
            alert.EmailSent);
    }

    public SellerOffer? GetBestConfirmedOffer(string productSlug)
    {
        return GetConfirmedOfferCandidates(productSlug)
            .OrderBy(offer => offer.PriceCents)
            .ThenByDescending(offer => offer.ReliabilityScore)
            .FirstOrDefault();
    }

    public IReadOnlyList<SellerOffer> GetConfirmedOfferCandidates(string productSlug)
    {
        return comparisonData.GetSellerOffers(productSlug)
            .Where(ComparisonDataService.IsConfirmedStoreOffer)
            .OrderBy(offer => offer.PriceCents)
            .ThenByDescending(offer => offer.ReliabilityScore)
            .ToList();
    }

    public async Task<SellerOffer?> GetBestOfferForAlertCreationAsync(
        string productSlug,
        CancellationToken cancellationToken = default)
    {
        var catalogOffer = GetBestConfirmedOffer(productSlug);
        if (catalogOffer is null)
        {
            return null;
        }

        if (storeOfferValidator is null)
        {
            return catalogOffer;
        }

        var liveOffer = await GetBestLiveValidatedOfferAsync(productSlug, cancellationToken);
        if (liveOffer is not null)
        {
            return liveOffer;
        }

        return catalogOffer with
        {
            ValidationState = OfferPriceValidationState.PendingValidation,
            ValidatedUtc = null
        };
    }

    public async Task<SellerOffer?> GetBestLiveValidatedOfferAsync(
        string productSlug,
        CancellationToken cancellationToken = default)
    {
        var product = comparisonData.FindProduct(productSlug);
        if (product is null)
        {
            return null;
        }

        var candidateOffers = comparisonData.GetSellerOffers(productSlug)
            .Where(ComparisonDataService.IsConfirmedStoreOffer)
            .ToList();
        if (candidateOffers.Count == 0)
        {
            return null;
        }

        if (storeOfferValidator is null)
        {
            return null;
        }

        var validatedOffers = await storeOfferValidator.ValidateConfirmedOffersAsync(
            product,
            candidateOffers,
            cancellationToken);

        return validatedOffers
            .Where(offer => offer.IsLiveValidated)
            .OrderBy(offer => offer.PriceCents)
            .ThenByDescending(offer => offer.ReliabilityScore)
            .FirstOrDefault();
    }

    public Task<int> CountActiveForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        return dbContext.TargetPriceAlerts
            .AsNoTracking()
            .CountAsync(alert => alert.UserId == userId && alert.IsActive && !alert.EmailSent, cancellationToken);
    }

    public async Task<IReadOnlyList<PriceAlertListItem>> GetActiveForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.TargetPriceAlerts
            .AsNoTracking()
            .Where(alert => alert.UserId == userId && alert.IsActive && !alert.EmailSent)
            .OrderByDescending(alert => alert.CreatedUtc)
            .Select(alert => new PriceAlertListItem(
                alert.ProductSlug,
                alert.ProductName,
                alert.TargetPriceCents,
                alert.LastSeenPriceCents,
                alert.LastSeenSeller,
                alert.ProductUrl,
                alert.LastValidationState,
                alert.LastValidatedUtc,
                alert.CreatedUtc))
            .ToListAsync(cancellationToken);
    }

    public static bool TryParsePrice(string? rawValue, out decimal price)
    {
        price = 0;
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return false;
        }

        var normalized = rawValue.Trim().Replace("€", string.Empty).Replace(" ", string.Empty);
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.GetCultureInfo("pt-PT"), out price)
            || decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out price);
    }

    public static long ToCents(decimal value)
    {
        return (long)Math.Round(value * 100, MidpointRounding.AwayFromZero);
    }

    public static string FormatCents(long cents)
    {
        return (cents / 100m).ToString("C", CultureInfo.GetCultureInfo("pt-PT"));
    }
}

public sealed record PriceAlertListItem(
    string ProductSlug,
    string ProductName,
    long TargetPriceCents,
    long LastSeenPriceCents,
    string? LastSeenSeller,
    string? ProductUrl,
    string LastValidationState,
    DateTime? LastValidatedUtc,
    DateTime CreatedUtc);

public sealed record CreatedPriceAlert(
    string ProductSlug,
    string ProductName,
    long TargetPriceCents,
    long LastSeenPriceCents,
    string? LastSeenSeller,
    string? ProductUrl,
    string LastValidationState,
    DateTime? LastValidatedUtc,
    bool IsActive,
    bool EmailSent)
{
    public bool IsLiveValidated => string.Equals(LastValidationState, nameof(OfferPriceValidationState.LiveValidated), StringComparison.OrdinalIgnoreCase);
}


public static class PriceAlertCreationError
{
    public const string NoConfirmedStoreOffer = "NoConfirmedStoreOffer";
    public const string ActiveAlertLimitReached = "ActiveAlertLimitReached";
}

public sealed class PriceAlertCreationException(string code) : InvalidOperationException(code)
{
    public string Code { get; } = code;
}
