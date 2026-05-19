using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TrueCompare.Data;

namespace TrueCompare.Services;

public sealed class TargetPriceAlertService(ApplicationDbContext dbContext, ComparisonDataService comparisonData)
{
    public async Task<TargetPriceAlert> CreateAsync(
        string userId,
        string productSlug,
        string productName,
        decimal targetPrice,
        CancellationToken cancellationToken = default)
    {
        var bestOffer = comparisonData.GetBestOffer(productSlug);
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
        return alert;
    }

    public async Task<IReadOnlyList<TargetPriceAlert>> GetActiveForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.TargetPriceAlerts
            .AsNoTracking()
            .Where(alert => alert.UserId == userId && alert.IsActive && !alert.EmailSent)
            .OrderByDescending(alert => alert.CreatedUtc)
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
