using TrueCompare.Models;

namespace TrueCompare.Services;

public interface IProductSuggestionService
{
    Task<LlmSuggestionResult> GetSuggestionsAsync(
        string? query,
        IReadOnlyList<ProductResult> products,
        IReadOnlyList<SellerOffer> offers,
        CancellationToken cancellationToken = default);
}
