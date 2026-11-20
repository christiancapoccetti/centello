using Centesimo.Domain;

namespace Centesimo.Application;

/// <summary>What a slot holds. A month has 31 slots and a year 12, so bar widths never change between periods. Spent
/// is a day or month that has passed: a bar. Today is today's slot in the period of today: the bar in ink. Ahead is a
/// day or month of this period still to come: a stub. Outside is past the period's last day, such as slot 31 of
/// November: nothing.</summary>
public enum PaceSlotKind
{
    Spent,
    Today,
    Ahead,
    Outside
}

/// <summary>Automatic and Total are cumulative and 0..1 of the chart's scale, with Automatic &lt;= Total. Ahead and
/// Outside slots carry 0. Without automatic spending Automatic is 0 everywhere and the bar is one tone.</summary>
public sealed record PaceSlot(PaceSlotKind Kind, double Automatic, double Total);

/// <summary>The previous period's cumulative spending as a dashed step line, as it really was: one level per slot of
/// the previous period (28 to 31, or 12), never bent to meet the bars. Levels[i] is the spending through the end of
/// slot i, so the line rises to it at the left edge of slot i and runs flat to the right edge after the last level.
/// Levels never decrease. The line does not carry the compared total: that is <see cref="PaceGap.From"/>.
/// CloseLabel reads "ottobre ha chiuso a 1.590 €".</summary>
public sealed record PaceLine(IReadOnlyList<double> Levels, string CloseLabel);

/// <summary>The budget hairline. Label reads "budget 1.600 €".</summary>
public sealed record PaceBudget(double Fraction, string Label);

/// <summary>The difference where the two windows are compared: today's slot, or the last day's for a past period.
/// From is the exact compared total and To the exact spent total, both as fractions of the scale. They are not read
/// off the previous line, which stays truthful. For the month of today From meets the line at this slot. In year mode
/// it lies inside the step of last year's month, which holds that whole month. For a past 30-day month after a 31-day
/// one it reaches the previous closing level, above the line at that slot. Amount reads "82 € in meno" and Caption
/// "di ottobre a oggi".</summary>
public sealed record PaceGap(int Slot, double From, double To, string Amount, string Caption);

public sealed record PaceTick(int Slot, string Text);

/// <summary>Start is under slot 0, End under the period's last day (slot 29 for November, 27 for February), Today
/// under today's slot. Today is null outside the period of today.</summary>
public sealed record PaceAxis(PaceTick Start, PaceTick End, PaceTick? Today);

/// <summary>Which series a key item names. The App decides how each one looks.</summary>
public enum PaceSeries { Automatic, Variable, Spent, Previous }

/// <summary>One item of the key. Amount reads "412,99 €" and is empty when the item names a period alone.</summary>
public sealed record PaceKeyItem(PaceSeries Series, string Name, string Amount);

/// <summary>How the automatic and the variable spending moved between the compared windows. Automatic amounts count
/// as the same when they differ by at most 1 € or 2 % of the larger, so a small change of price does not break the
/// verdict. The decision is kept apart from its wording: <see cref="PaceChart.Sentence"/> is chosen from it.
/// NothingToCompare is no spending in the compared days. NoAutomatic is no automatic spending on either side.
/// OnlyOneSide is automatic spending on one side only, as in the months after an import. BothSame is automatic and
/// variable spending both in line. OnlyVariableMoved is automatic spending the same and the variable moved.
/// OnlyAutomaticMoved is the automatic moved and the variable in line. BothMoved is both moved.</summary>
public enum SplitVerdict
{
    NothingToCompare,
    NoAutomatic,
    OnlyOneSide,
    BothSame,
    OnlyVariableMoved,
    OnlyAutomaticMoved,
    BothMoved
}

/// <summary>"Il passo del mese": cumulative spending of this period against the previous one, split into automatic
/// and variable spending. All heights are 0..1 of one scale, the largest of the spent total, the previous closing
/// total and the budget; the App adds fixed headroom above 1 for the labels.</summary>
public sealed record PaceChart(
    string Title, // "Il passo del mese" or "Il passo dell'anno"
    IReadOnlyList<PaceSlot> Slots, // 31 for a month, 12 for a year
    PaceLine? Previous, // null when the previous period has no spending
    PaceBudget? Budget, // null without a budget
    PaceGap? Gap, // null when nothing was spent in the compared days
    PaceAxis Axis,
    IReadOnlyList<PaceKeyItem> Key,
    SplitVerdict Split,
    string Sentence, // the wording of Split; empty for NothingToCompare and NoAutomatic
    string Description) // screen-reader text alternative
{
    private const int MonthSlots = 31;
    private const int YearSlots = 12;
    private const char Nbsp = (char)0xA0;

    // WholeEuros rounds half up, so a difference under 0,50 € would read "0 €".
    private const long HalfEuro = 50;

    public bool HasSentence => Sentence.HasValue();

    internal static PaceChart For(SpendComparison spending, long? budget)
    {
        var period = spending.Period;
        var compared = SlotOf(period, spending.Window.CurrentTo);
        long[] heights = [spending.Spent, spending.PreviousClosed, budget ?? 0];
        var scale = new Scale(heights.Max());
        var split = PaceSplit.Of(spending);
        var line = Line(spending, scale);
        return new PaceChart(
            period.Kind == PeriodKind.Month ? "Il passo del mese" : "Il passo dell'anno",
            Bars(spending, compared, scale), line, BudgetLine(budget, scale), GapOf(spending, compared, scale),
            AxisOf(spending, compared), KeyOf(spending, split, line is not null),
            SplitVerdict.NothingToCompare, "", "");
    }

    /// <summary>Day d of a month is slot d - 1, month m of a year is slot m - 1, for either period.</summary>
    private static int SlotOf(Period period, DateOnly day) => (period.Kind == PeriodKind.Month ? day.Day : day.Month) - 1;

    private static int SlotCount(Period period) => period.Kind == PeriodKind.Month ? MonthSlots : YearSlots;

    private static List<PaceSlot> Bars(SpendComparison spending, int compared, Scale scale)
    {
        var period = spending.Period;
        var count = SlotCount(period);
        var last = SlotOf(period, period.End);
        var total = Cumulative(count, period, spending.Current);
        var automatic = Cumulative(count, period, spending.Current.Where(expense => expense.IsAutomatic));
        return Enumerable
            .Range(0, count)
            .Select(slot =>
            {
                var kind = KindOf(slot, compared, last, spending.IsToDate);
                return kind is PaceSlotKind.Spent or PaceSlotKind.Today
                    ? new PaceSlot(kind, scale.Of(automatic[slot]), scale.Of(total[slot]))
                    : new PaceSlot(kind, 0, 0);
            })
            .ToList();
    }

    private static PaceLine? Line(SpendComparison spending, Scale scale)
    {
        if (spending.PreviousClosed == 0)
            return null;

        var previous = spending.Period.Previous();
        var levels = Cumulative(SlotOf(previous, previous.End) + 1, previous, spending.PreviousWhole);
        return new PaceLine(
            levels.Select(level => scale.Of(level)).ToList(),
            $"{Subject(previous)} ha chiuso a {Euros(spending.PreviousClosed)}");
    }

    private static PaceAxis AxisOf(SpendComparison spending, int compared)
    {
        var period = spending.Period;
        return new PaceAxis(Tick(period, period.Start), Tick(period, period.End),
            spending.IsToDate ? new PaceTick(compared, "oggi") : null);
    }

    private static PaceTick Tick(Period period, DateOnly day) => new(SlotOf(period, day),
        period.Kind == PeriodKind.Month ? ItalianFormat.ShortDate(day) : ItalianFormat.ShortMonth(day.Month));

    private static List<PaceKeyItem> KeyOf(SpendComparison spending, PaceSplit split, bool hasLine)
    {
        var period = spending.Period;
        List<PaceKeyItem> key = [];
        if (split.IsShown)
            key.AddRange([
                new PaceKeyItem(PaceSeries.Automatic, "Automatiche", ItalianFormat.Money(split.Automatic)),
                new PaceKeyItem(PaceSeries.Variable, "Variabili", ItalianFormat.Money(split.Variable))]);
        else
            key.Add(new PaceKeyItem(PaceSeries.Spent, InsightTexts.Name(period), ""));

        if (hasLine)
            key.Add(new PaceKeyItem(PaceSeries.Previous, InsightTexts.Name(period.Previous()), ""));

        return key;
    }

    private static PaceBudget? BudgetLine(long? budget, Scale scale) => budget is { } cents
        ? new PaceBudget(scale.Of(cents), $"budget {ItalianFormat.Budget(cents)}{Nbsp}€")
        : null;

    private static PaceGap? GapOf(SpendComparison spending, int compared, Scale scale)
    {
        if (!spending.IsComparable)
            return null;

        var delta = spending.Spent - spending.Compared;
        var inLine = Math.Abs(delta) < HalfEuro;
        var amount = inLine ? "In linea" : $"{Euros(Math.Abs(delta))} {(delta < 0 ? "in meno" : "in più")}";
        return new PaceGap(compared, scale.Of(spending.Compared), scale.Of(spending.Spent), amount,
            GapCaption(spending, inLine));
    }

    private static string GapCaption(SpendComparison spending, bool inLine)
    {
        var previous = spending.Period.Previous();
        var against = inLine ? $"con {Subject(previous)}" : InsightTexts.OfPeriod(previous);
        return spending.IsToDate ? $"{against} a oggi" : against;
    }

    private static PaceSlotKind KindOf(int slot, int compared, int last, bool isToDate)
    {
        if (slot > last)
            return PaceSlotKind.Outside;

        if (slot > compared)
            return PaceSlotKind.Ahead;

        return slot == compared && isToDate ? PaceSlotKind.Today : PaceSlotKind.Spent;
    }

    /// <summary>Spending through the end of each slot. Slots after the last expense keep the running total.</summary>
    private static long[] Cumulative(int count, Period period, IEnumerable<Expense> expenses)
    {
        var levels = new long[count];
        foreach (var expense in expenses)
            levels[SlotOf(period, expense.OccurredOn)] += expense.Amount.Cents;

        for (var slot = 1; slot < count; slot++)
            levels[slot] += levels[slot - 1];

        return levels;
    }

    /// <summary>"ottobre" or "il 2025": the previous period as a sentence names it.</summary>
    private static string Subject(Period period) => period.Kind == PeriodKind.Month
        ? ItalianFormat.MonthName(period.Start.Month)
        : $"il {period.Start.Year}";

    /// <summary>"1.590 €": the chart's unit is the whole euro.</summary>
    private static string Euros(long cents) => $"{ItalianFormat.WholeEuros(cents)}{Nbsp}€";

    /// <summary>Cents as a fraction of the chart's one scale.</summary>
    private readonly record struct Scale(long Cents)
    {
        public double Of(long cents) => Cents == 0 ? 0 : (double)cents / Cents;
    }
}

/// <summary>The automatic and the variable spending of the two compared windows, in cents.</summary>
internal sealed record PaceSplit(long Automatic, long Variable, long ComparedAutomatic, long ComparedVariable)
{
    /// <summary>Either window holds automatic spending, so the bars and the key tell the two kinds apart.</summary>
    public bool IsShown => Automatic > 0 || ComparedAutomatic > 0;

    public static PaceSplit Of(SpendComparison spending)
    {
        var automatic = AutomaticIn(spending.Current);
        var comparedAutomatic = AutomaticIn(spending.Previous);
        return new PaceSplit(automatic, spending.Spent - automatic, comparedAutomatic,
            spending.Compared - comparedAutomatic);
    }

    private static long AutomaticIn(IEnumerable<Expense> expenses) => expenses
        .Where(expense => expense.IsAutomatic)
        .Sum(expense => expense.Amount.Cents);
}
