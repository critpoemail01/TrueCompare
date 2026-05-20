using TrueCompare.Models;

namespace TrueCompare.Services;

public interface IProductImageSuggestionService
{
    Task<ImageProductSuggestionResult> AnalyzeImageAsync(
        string fileName,
        string contentType,
        byte[] imageBytes,
        CancellationToken cancellationToken = default);
}
