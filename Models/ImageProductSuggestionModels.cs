namespace TrueCompare.Models;

public sealed record ImageProductSuggestionResult(
    bool FromLlm,
    string SourceLabel,
    string DetectedProductType,
    string SuggestedQuery,
    string Summary,
    int Confidence,
    IReadOnlyList<ImageProductLead> ProductLeads,
    IReadOnlyList<string> Warnings);

public sealed record ImageProductLead(
    string Name,
    string Reason,
    string SearchHint,
    string Source);
