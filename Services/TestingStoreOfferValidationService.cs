using TrueCompare.Models;

namespace TrueCompare.Services;

public sealed class TestingStoreOfferValidationService : IStoreOfferValidationService
{
    public Task<IReadOnlyList<SellerOffer>> ValidateConfirmedOffersAsync(
        ProductResult? product,
        IReadOnlyList<SellerOffer> offers,
        CancellationToken cancellationToken = default)
    {
        var validatedUtc = DateTime.UtcNow;
        return Task.FromResult((IReadOnlyList<SellerOffer>)offers
            .Where(ComparisonDataService.IsConfirmedStoreOffer)
            .Select(offer => offer with
            {
                IsLivePrice = true,
                ValidationState = OfferPriceValidationState.LiveValidated,
                ValidatedUtc = validatedUtc
            })
            .ToList());
    }
}
