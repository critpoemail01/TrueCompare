using TrueCompare.Models;

namespace TrueCompare.Services;

public interface IProductDiscoveryService
{
    Task<ProductDiscoveryResult> DiscoverAsync(string? query, CancellationToken cancellationToken = default);

    Task<ProductResult?> FindProductAsync(string? slug, string? query = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SellerOffer>> GetSellerOffersAsync(
        string? queryOrSlug,
        string? originalQuery = null,
        CancellationToken cancellationToken = default);
}
