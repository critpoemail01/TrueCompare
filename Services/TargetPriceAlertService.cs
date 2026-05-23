using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TrueCompare.Data;
using TrueCompare.Models;

namespace TrueCompare.Services;

public sealed class TargetPriceAlertService(ApplicationDbContext dbContext, ComparisonDataService comparisonData)
{
    public async Task<CreatedPriceAlert> CreateAsync(
        string userId,
        string productSlug,
        string productName,
        decimal targetPrice,
        CancellationToken cancellationToken = default)
    {
        var bestOffer = GetBestConfirmedOffer(productSlug)
            ?? throw new InvalidOperationException("Cannot create a price alert without a confirmed store offer.");
        var alert = new TargetPriceAlert
        {
            UserId = userId,
            ProductSlug = productSlug,
            ProductName = productName,
            TargetPriceCents = ToCents(targetPrice),
            LastSeenPriceCents = bestOffer.PriceCents,
            LastSeenSeller = bestOffer.Seller,
            ProductUrl = bestOffer.Url
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
            alert.IsActive,
            alert.EmailSent);
    }

    public SellerOffer? GetBestConfirmedOffer(string productSlug)
    {
        return comparisonData.GetSellerOffers(productSlug)
            .Where(ComparisonDataService.IsConfirmedStoreOffer)
            .OrderBy(offer => offer.PriceCents)
            .ThenByDescending(offer => offer.ReliabilityScore)
            .FirstOrDefault();
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
    DateTime CreatedUtc);

public sealed record CreatedPriceAlert(
    string ProductSlug,
    string ProductName,
    long TargetPriceCents,
    long LastSeenPriceCents,
    string? LastSeenSeller,
    string? ProductUrl,
    bool IsActive,
    bool EmailSent);
