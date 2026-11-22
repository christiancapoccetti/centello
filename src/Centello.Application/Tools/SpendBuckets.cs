using Centello.Domain;

namespace Centello.Application;

/// <summary>Spending per day (month period) or per month (year period), and which bucket holds today. The home
/// chart and the category detail draw the same bars from it.</summary>
internal sealed record SpendBuckets(long[] Cents, int Current)
{
    public int Count => Cents.Length;
    public bool HasToday => Current >= 0 && Current < Count;

    public static SpendBuckets Of(Period period, DateOnly today, IEnumerable<Expense> expenses)
    {
        var cents = new long[period.Kind == PeriodKind.Month ? period.End.Day : 12];
        foreach (var expense in expenses)
            cents[BucketOf(period, expense.OccurredOn)] += expense.Amount.Cents;

        // Index of today's bucket: before the period it is -1, after the period it is the bucket count.
        var current = today < period.Start ? -1 : today > period.End ? cents.Length : BucketOf(period, today);
        return new SpendBuckets(cents, current);
    }

    /// <summary>Today's bar and those before it are drawn; later ones are stubs, so they must not stretch the scale.
    /// <paramref name="floor"/> keeps a reference line, such as the budget pace, on the chart.</summary>
    public double Scale(double floor = 0) => Math.Max(
        Cents.Where((_, index) => KindOf(index) != SpendBarKind.Future).DefaultIfEmpty(0).Max(), floor);

    public IReadOnlyList<SpendBar> Bars(double scale) => Cents
        .Select((cents, index) => new SpendBar(
            KindOf(index) == SpendBarKind.Future || scale == 0 ? 0 : cents / scale, KindOf(index)))
        .ToList();

    private SpendBarKind KindOf(int index) =>
        index < Current ? SpendBarKind.Past : index == Current ? SpendBarKind.Current : SpendBarKind.Future;

    private static int BucketOf(Period period, DateOnly day) => (period.Kind == PeriodKind.Month ? day.Day : day.Month) - 1;
}
