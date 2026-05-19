namespace TrueCompare.Models;

public sealed record LlmSuggestionResult(
    bool FromLlm,
    string SourceLabel,
    string Intent,
    string Summary,
    int Confidence,
    IReadOnlyList<string> BuyingSignals,
    IReadOnlyList<string> SuggestedQueries,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<LlmProductLead> ProductLeads);

public sealed record LlmProductLead(
    string Name,
    string Reason,
    string TargetPrice,
    string SearchHint,
    string Source);
