using Centesimo.Domain;

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
    private const int MaxSteps = 6;
    private const long OneEuro = 100;
    private const long DriverPercent = 75;
    private const string FoldName = "Altre categorie";
    private const char Minus = '−';
    private const char Nbsp = '\u00A0';

    /// <summary>Null when nothing was spent in the compared days, or when nothing changed by 1 € or more.</summary>
    internal static WaterfallChart? For(SpendComparison spending)
    {
        if (!spending.IsComparable)
            return null;

        var changes = Changes(spending);
        if (changes.Count == 0)
            return null;

        var period = spending.Period;
        var levels = Running(spending.Compared, changes);
        var shifts = Shifts(levels);
        var scale = Scale.Of(levels);
        var start = Total(spending.Compared, scale, InsightTexts.Short(period.Previous()));
        var end = Total(spending.Spent, scale, InsightTexts.Short(period));
        var steps = changes
            .Select((change, index) => new WaterfallStep(change.Name, change.IconKey, change.ColorKey, change.Delta,
                scale.Level(levels[index]), scale.Level(levels[index + 1]), Label(shifts[index])))
            .ToList();
        var title = InsightTexts.FromTo(period);
        return new WaterfallChart(title, start, steps, end, scale.IsCut, Say(spending, changes, scale),
            Describe(title, start, end, changes, shifts));
    }

    /// <summary>What a column of the chart changes by: a category, or the rest folded into one. The fold is named in
    /// the plural, so the sentence agrees with it.</summary>
    private sealed record Change(string Name, string IconKey, string ColorKey, long Delta, bool IsFold = false)
    {
        public static Change Of(CategorySpend spend) => new(spend.Category.Name,
            CategoryIcons.Resolve(spend.Category.Icon).Key, CategoryPalette.Resolve(spend.Category.Color).Key, spend.Delta);

        public static Change Fold(long delta) =>
            new(FoldName, CategoryIcons.Fallback.Key, CategoryPalette.Fallback.Key, delta, true);

        public bool IsNamed => !IsFold || Math.Abs(Delta) >= OneEuro;
    }

    /// <summary>The changes of at least 1 € and the residual that closes the total, falls first then by name. Empty when
    /// no category changed by 1 € and the residual is under 1 €: that is noise, not a chart.</summary>
    private static List<Change> Changes(SpendComparison spending)
    {
        var changed = spending.Categories
            .Where(spend => Math.Abs(spend.Delta) >= OneEuro)
            .OrderByDescending(spend => Math.Abs(spend.Delta))
            .ThenBy(spend => spend.Category.Name)
            .ToList();
        var (kept, fold) = Fold(changed, spending.Spent - spending.Compared);
        var changes = kept.Select(Change.Of).ToList();
        var isNoise = kept.Count == 0 && Math.Abs(fold) < OneEuro;
        if (fold != 0 && !isNoise)
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

    /// <summary>What explains the change, which categories did not change and, when the plot is cut, where it starts.</summary>
    private static string Say(SpendComparison spending, IReadOnlyList<Change> changes, Scale scale) => string.Join(" ",
        new[]
        {
            Explanation(spending.Period, changes, spending.Spent - spending.Compared),
            Unchanged(spending.Categories),
            scale.IsCut ? $"La scala parte da {ItalianFormat.Budget(scale.Floor)}{Nbsp}€." : ""
        }.Where(part => part.HasValue()));

    /// <summary>Names what explains the change: the fewest categories that make up 75 % of everything that went the way
    /// of the net change, then the largest one that went the other way. A net change under 0,50 € is a total that held.</summary>
    private static string Explanation(Period period, IReadOnlyList<Change> changes, long net)
    {
        if (Euros(Math.Abs(net)) == 0)
            return Held(period, changes);

        var movers = Ranked(changes, Math.Sign(net));
        var drivers = Drivers(movers);
        var lead = Lead(drivers, drivers.Count == movers.Count, net < 0);
        if (Ranked(changes, -Math.Sign(net)).FirstOrDefault() is not { } opposite)
            return $"{lead}.";

        return $"{lead}; {opposite.Name} {Agree(opposite, "va", "vanno")} nella direzione opposta.";
    }

    /// <summary>"Il totale è in linea con ottobre: A e B si compensano." The pair is the largest fall and the largest
    /// rise, and is left out when everything went one way, as when only the fold moved.</summary>
    private static string Held(Period period, IReadOnlyList<Change> changes)
    {
        var held = $"Il totale è in linea con {With(period.Previous())}";
        if (Ranked(changes, -1).FirstOrDefault() is not { } fall || Ranked(changes, 1).FirstOrDefault() is not { } rise)
            return $"{held}.";

        return $"{held}: {fall.Name} e {rise.Name} si compensano.";
    }

    /// <summary>"ottobre" or "il 2025".</summary>
    private static string With(Period previous) => previous.Kind == PeriodKind.Month
        ? ItalianFormat.MonthName(previous.Start.Month)
        : $"il {previous.Start.Year}";

    /// <summary>Categories that spent the same in both windows, down to the cent. A category that moved by less than
    /// 1 € is in the fold, not here.</summary>
    private static string Unchanged(IReadOnlyList<CategorySpend> categories)
    {
        var names = categories
            .Where(spend => spend.Delta == 0 && spend.Spent > 0)
            .Select(spend => spend.Category.Name)
            .Order()
            .ToList();
        return names.Count switch
        {
            0 => "",
            1 => $"{names[0]} è invariata.",
            2 => $"{names[0]} e {names[1]} sono invariate.",
            _ => $"{names.Count} categorie sono invariate."
        };
    }

    /// <summary>The changes that went <paramref name="direction"/> (-1 a fall, 1 a rise), largest first. The sentence
    /// never names a fold under 1 €, which stays on the chart as a column only.</summary>
    private static List<Change> Ranked(IReadOnlyList<Change> changes, int direction) => changes
        .Where(change => change.IsNamed && Math.Sign(change.Delta) == direction)
        .OrderByDescending(change => Math.Abs(change.Delta))
        .ThenBy(change => change.Name)
        .ToList();

    private static List<Change> Drivers(IReadOnlyList<Change> movers)
    {
        var gross = movers.Sum(change => Math.Abs(change.Delta));
        var drivers = new List<Change>();
        var covered = 0L;
        foreach (var mover in movers)
        {
            drivers.Add(mover);
            covered += Math.Abs(mover.Delta);
            if (covered * 100 >= gross * DriverPercent)
                break;
        }

        return drivers;
    }

    private static string Lead(IReadOnlyList<Change> drivers, bool isAll, bool isFall)
    {
        var word = isFall ? "il calo" : "l'aumento";
        if (drivers.Count > 2)
            return $"{char.ToUpperInvariant(word[0])}{word[1..]} viene da più categorie, soprattutto da {drivers[0].Name}";

        var how = isAll ? "tutto" : "quasi tutto";
        return drivers.Count == 1
            ? $"{drivers[0].Name} {Agree(drivers[0], "spiega", "spiegano")} {how} {word}"
            : $"{drivers[0].Name} e {drivers[1].Name} spiegano {how} {word}";
    }

    private static string Agree(Change change, string singular, string plural) => change.IsFold ? plural : singular;

    /// <summary>Where the plot starts and ends, in cents. It cuts only when the change is small against the totals: the
    /// spread of the levels is at most a third of the highest one. Then it starts at a round number below the lowest
    /// level, with half the spread as margin so the label under the lowest fall fits. Otherwise it starts at zero.</summary>
    private readonly record struct Scale(long Floor, long High)
    {
        private static readonly long[] NiceMultiples = [1, 2, 5];

        public bool IsCut => Floor > 0;

        public static Scale Of(IReadOnlyList<long> levels)
        {
            var high = levels.Max();
            var low = levels.Min();
            var spread = Math.Max(high - low, high / 10);
            if (spread * 3 > high)
                return new Scale(0, high);

            var step = NiceStep(spread);
            return new Scale((2 * low - spread) / (2 * step) * step, high);
        }

        /// <summary>0..1 of the plot above the floor.</summary>
        public double Level(long cents) => (double)(cents - Floor) / (High - Floor);

        private static long NiceStep(long spread)
        {
            for (var decade = OneEuro; ; decade *= 10)
                foreach (var multiple in NiceMultiples)
                    if (decade * multiple * 4 >= spread)
                        return decade * multiple;
        }
    }

    private static WaterfallTotal Total(long cents, Scale scale, string caption) =>
        new(cents, scale.Level(cents), ItalianFormat.WholeEuros(cents), caption);

    /// <summary>The whole euros of the level after each step minus those of the level before it. Neighbouring shifts
    /// share a rounded level, so they add up from the rounded start to the rounded end.</summary>
    private static long[] Shifts(IReadOnlyList<long> levels) =>
        [.. levels.Zip(levels.Skip(1), (before, after) => Euros(after) - Euros(before))];

    /// <summary>"+5", "−60" or "0".</summary>
    private static string Label(long euros)
    {
        if (euros == 0)
            return "0";

        return euros > 0 ? $"+{ItalianFormat.Count(euros)}" : $"{Minus}{ItalianFormat.Count(-euros)}";
    }

    /// <summary>"Da ottobre a novembre, da 1.367 € a 1.285 €: Trasporti meno 60 €, Altro più 5 €." It follows the labels
    /// on the chart, euro for euro.</summary>
    private static string Describe(string title, WaterfallTotal start, WaterfallTotal end, IReadOnlyList<Change> changes,
        IReadOnlyList<long> shifts)
    {
        var parts = changes.Select((change, index) => $"{change.Name} {Spoken(shifts[index])}");
        return $"{title}, da {start.Value}{Nbsp}€ a {end.Value}{Nbsp}€: {string.Join(", ", parts)}.";
    }

    private static string Spoken(long euros) => euros switch
    {
        0 => $"0{Nbsp}€",
        > 0 => $"più {ItalianFormat.Count(euros)}{Nbsp}€",
        _ => $"meno {ItalianFormat.Count(-euros)}{Nbsp}€"
    };

    private static long Euros(long cents) => (cents + 50) / 100;
}
