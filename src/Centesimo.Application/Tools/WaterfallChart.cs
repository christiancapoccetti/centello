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
    // Eight columns are about 43dp each on a 360dp phone: room for a 24dp tile and a four-digit label.
    private const int MaxSteps = 6;
    private const long OneEuro = 100;
    private const string FoldName = "Altre categorie";
    private const char Minus = '−';

    /// <summary>Null when nothing was spent in the compared days, or when nothing changed at all.</summary>
    internal static WaterfallChart? For(SpendComparison spending)
    {
        if (!spending.IsComparable)
            return null;

        var changes = Changes(spending);
        if (changes.Count == 0)
            return null;

        var period = spending.Period;
        var levels = Running(spending.Compared, changes);
        var high = levels.Max();
        var steps = changes
            .Select((change, index) => new WaterfallStep(change.Name, change.IconKey, change.ColorKey, change.Delta,
                Level(levels[index], high), Level(levels[index + 1], high), Label(levels[index], levels[index + 1])))
            .ToList();
        return new WaterfallChart(InsightTexts.FromTo(period),
            Total(spending.Compared, high, InsightTexts.Short(period.Previous())), steps,
            Total(spending.Spent, high, InsightTexts.Short(period)), false, "", "");
    }

    /// <summary>What a column of the chart changes by: a category, or the rest folded into one.</summary>
    private sealed record Change(string Name, string IconKey, string ColorKey, long Delta)
    {
        public static Change Of(CategorySpend spend) => new(spend.Category.Name,
            CategoryIcons.Resolve(spend.Category.Icon).Key, CategoryPalette.Resolve(spend.Category.Color).Key, spend.Delta);

        public static Change Fold(long delta) => new(FoldName, CategoryIcons.Fallback.Key, CategoryPalette.Fallback.Key, delta);
    }

    /// <summary>The changes of at least 1 € and the residual that closes the total, falls first then by name.</summary>
    private static List<Change> Changes(SpendComparison spending)
    {
        var changed = spending.Categories
            .Where(spend => Math.Abs(spend.Delta) >= OneEuro)
            .OrderByDescending(spend => Math.Abs(spend.Delta))
            .ThenBy(spend => spend.Category.Name)
            .ToList();
        var (kept, fold) = Fold(changed, spending.Spent - spending.Compared);
        var changes = kept.Select(Change.Of).ToList();
        if (fold != 0)
            changes.Add(Change.Fold(fold));

        return [.. changes.OrderBy(change => change.Delta).ThenBy(change => change.Name)];
    }

    /// <summary>Keeps the largest changes and folds everything else into one residual. The residual holds archived and
    /// unknown categories, changes under 1 € and changes past the column limit, so the steps always reach the end.</summary>
    private static (IReadOnlyList<CategorySpend> Kept, long Fold) Fold(IReadOnlyList<CategorySpend> changed, long net)
    {
        var fold = net - changed.Sum(spend => spend.Delta);
        if (changed.Count + (fold == 0 ? 0 : 1) <= MaxSteps)
            return (changed, fold);

        var kept = changed.Take(MaxSteps - 1).ToList();
        return (kept, net - kept.Sum(spend => spend.Delta));
    }

    /// <summary>The cents of the compared total, then after each change in turn: one function, so a step ends on
    /// exactly the level the next one starts from.</summary>
    private static long[] Running(long start, IReadOnlyList<Change> changes)
    {
        var levels = new long[changes.Count + 1];
        levels[0] = start;
        for (var index = 0; index < changes.Count; index++)
            levels[index + 1] = levels[index] + changes[index].Delta;

        return levels;
    }

    private static double Level(long cents, long high) => (double)cents / high;

    private static WaterfallTotal Total(long cents, long high, string caption) =>
        new(cents, Level(cents, high), ItalianFormat.WholeEuros(cents), caption);

    /// <summary>The whole euros of the level after a step minus those of the level before it, signed. Neighbouring
    /// labels share a rounded level, so they add up from the rounded start to the rounded end.</summary>
    private static string Label(long before, long after)
    {
        var euros = Euros(after) - Euros(before);
        if (euros == 0)
            return "0";

        return euros > 0 ? $"+{ItalianFormat.Count(euros)}" : $"{Minus}{ItalianFormat.Count(-euros)}";
    }

    // Levels are never negative, so half up is half away from zero.
    private static long Euros(long cents) => (cents + 50) / 100;
}
