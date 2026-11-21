using Centesimo.Domain;

namespace Centesimo.Application;

/// <summary>The days a recap covers. A closed month or year covers all of them. The year of today covers 1 January
/// to yesterday (IsSoFar) and compares with the same days a year earlier, but only from YearOpensOn. The month of
/// today, any future period and the year of today before that day have no span, so no recap.</summary>
public sealed record RecapSpan
{
    /// <summary>The month and day from which the year of today can be recapped.</summary>
    public static readonly (int Month, int Day) YearOpensOn = (12, 30);

    private RecapSpan(Period period, DateOnly through) => (Period, Through) = (period, through);

    public Period Period { get; }
    public DateOnly Through { get; }
    public bool IsSoFar => Through < Period.End;

    /// <summary>The days the span is compared with: the whole previous period for a closed one, the same days a year
    /// earlier for the year so far.</summary>
    public (DateOnly From, DateOnly To) Compared => (Window.PreviousFrom, Window.PreviousTo);

    /// <summary>Insight's window rule. A closed period is handed a day outside it, so it compares whole with whole and
    /// a 31-day previous month is not clamped to the length of this one.</summary>
    internal ComparisonWindow Window => ComparisonWindow.For(Period, IsSoFar ? Through : Through.AddDays(1));

    public static RecapSpan? Of(Period period, DateOnly today)
    {
        if (period.End < today)
            return new RecapSpan(period, period.End);

        return period.Kind == PeriodKind.Year && period.Start.Year == today.Year && today >= new DateOnly(today.Year, YearOpensOn.Month, YearOpensOn.Day)
            ? new RecapSpan(period, today.AddDays(-1))
            : null;
    }

    /// <summary>The single eligibility rule. Insight's row, the deck and the notification all call this. Expenses
    /// outside the span are ignored, so a read that also covers the compared window can be passed whole.</summary>
    internal bool HasEnough(IEnumerable<Expense> expenses) => RecapRules.For(Period.Kind).HasEnough(expenses
        .Where(expense => expense.OccurredOn >= Period.Start && expense.OccurredOn <= Through)
        .ToList());
}
