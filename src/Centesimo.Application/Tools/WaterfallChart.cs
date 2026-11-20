namespace Centesimo.Application;

/// <summary>An end column: the compared total (start) or the current total (end). Level is 0..1 of the plot above
/// the floor. When the chart is cut, the column is drawn with a break at its foot. Value is whole euros ("1.367") and
/// Caption the period under the column ("ott").</summary>
public sealed record WaterfallTotal(long Cents, double Level, string Value, string Caption);

/// <summary>One change, floating between the running level before it (From) and after it (To), both 0..1. Each step
/// starts where the column before it ends. DeltaCents is exact. Value is the signed change in whole euros ("−59",
/// "+4"): the whole euros of the level after the step minus the whole euros of the level before it. The labels
/// therefore telescope, so they add up from the rounded start to the rounded end, and a change of at least 1 € never
/// reads 0. Only the step that folds the rest can, when its residual is under 0,50 €.</summary>
public sealed record WaterfallStep(string Name, string IconKey, string ColorKey, long DeltaCents, double From,
    double To, string Value)
{
    /// <summary>A rise carries its label above, a fall below.</summary>
    public bool IsRise => DeltaCents > 0;
}

/// <summary>"Da ottobre a novembre": from the compared total to the current one, one step per category that changed.
/// Start.Cents + Σ Steps.DeltaCents == End.Cents, and Start.Value + Σ Steps.Value == End.Value in whole euros. Steps
/// is never empty, and falls come first. IsCut says the scale starts above zero, so the end columns are drawn cut at
/// the foot. Sentence says what explains the change, then where the scale starts when cut.</summary>
public sealed record WaterfallChart(
    string Title, // "Da ottobre a novembre" or "Dal 2025 al 2026"
    WaterfallTotal Start,
    IReadOnlyList<WaterfallStep> Steps,
    WaterfallTotal End,
    bool IsCut,
    string Sentence,
    string Description) // screen-reader text alternative
{
    /// <summary>Null when nothing was spent in the compared days, or when nothing changed at all.</summary>
    internal static WaterfallChart? For(SpendComparison spending) => null;
}
