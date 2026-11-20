namespace Centesimo.Application;

/// <summary>The structured output of the language model, before any validation.</summary>
public sealed record ExpenseInterpretation(
    decimal? Amount,
    string? CategoryKey,
    string? TagKey,
    string? Date,
    string Note);
