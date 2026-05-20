namespace TrueCompare.Models;

public sealed record ProductDiscoveryResult(
    string Query,
    IReadOnlyList<ProductResult> Products,
    IReadOnlyList<SellerOffer> Offers,
    bool FromLlm,
    string SourceLabel);
