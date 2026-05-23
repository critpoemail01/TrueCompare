using TrueCompare.Models;

namespace TrueCompare.Services;

public sealed class TestingStoreOfferValidationService : IStoreOfferValidationService
{
    public Task<IReadOnlyList<SellerOffer>> ValidateConfirmedOffersAsync(
        ProductResult? product,
        IReadOnlyList<SellerOffer> offers,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult((IReadOnlyList<SellerOffer>)offers
            .Where(ComparisonDataService.IsConfirmedStoreOffer)
            .ToList());
    }
}
