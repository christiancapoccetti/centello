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
    public bool HasSentence => Sentence.HasValue();

    internal static PaceChart For(SpendComparison spending, long? budget) => new(
        spending.Period.Kind == PeriodKind.Month ? "Il passo del mese" : "Il passo dell'anno",
        [], null, null, null, new PaceAxis(new PaceTick(0, ""), new PaceTick(0, ""), null), [],
        SplitVerdict.NothingToCompare, "", "");
}
