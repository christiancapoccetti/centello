namespace Centello.Application;

/// <summary>The current period compares to date with the same position in the previous one, clamped to its
/// last valid day (Mar 31 -> Feb 28; Feb 29 -> Feb 28 of the previous year). A past period compares whole with whole.</summary>
internal sealed record ComparisonWindow(DateOnly CurrentFrom, DateOnly CurrentTo, DateOnly PreviousFrom,
    DateOnly PreviousTo, bool IsPartial)
{
    public static ComparisonWindow For(Period period, DateOnly today)
    {
        var previous = period.Previous();
        if (!period.Contains(today))
            return new ComparisonWindow(period.Start, period.End, previous.Start, previous.End, false);

        var monthStart = previous.Start.AddMonths(period.Kind == PeriodKind.Year ? today.Month - 1 : 0);
        var lastDay = Math.Min(today.Day, DateTime.DaysInMonth(monthStart.Year, monthStart.Month));
        return new ComparisonWindow(period.Start, today, previous.Start, monthStart.AddDays(lastDay - 1), true);
    }
}
