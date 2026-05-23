using TrueCompare.Models;

namespace TrueCompare.Services;

public interface IStoreOfferValidationService
{
    Task<IReadOnlyList<SellerOffer>> ValidateConfirmedOffersAsync(
        ProductResult? product,
        IReadOnlyList<SellerOffer> offers,
        CancellationToken cancellationToken = default);
}
