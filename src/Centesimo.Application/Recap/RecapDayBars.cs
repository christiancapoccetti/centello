namespace Centesimo.Application;

/// <summary>A month's variable spending, one bar per day, shared by the cover and the record day. Variable only: the
/// day of the rent would tower over every day.</summary>
internal static class RecapDayBars
{
    public static RecapBars Of(RecapScope scope, bool strongPeak)
    {
        var period = scope.Span.Period;
        var cents = scope.Variable
            .GroupBy(expense => expense.OccurredOn.Day)
            .ToDictionary(day => day.Key, day => day.Sum(expense => expense.Amount.Cents));
        var peak = cents.Count == 0 ? 0 : cents.Values.Max();
        var peakDay = peak == 0 ? 0 : cents.Where(day => day.Value == peak).Min(day => day.Key);
        var bars = Enumerable.Range(1, DateTime.DaysInMonth(period.Start.Year, period.Start.Month))
            .Select(day => cents.GetValueOrDefault(day) is var spent and > 0
                ? new RecapBar((double)spent / peak, strongPeak && day == peakDay ? RecapBarKind.Strong : RecapBarKind.Bar)
                : new RecapBar(0, RecapBarKind.Stub))
            .ToList();
        var name = ItalianFormat.MonthName(period.Start.Month);
        if (peak == 0)
            return new RecapBars(bars, [], $"Spesa giorno per giorno a {name}.");

        var peakDate = new DateOnly(period.Start.Year, period.Start.Month, peakDay);
        return new RecapBars(bars, [],
            $"Spesa giorno per giorno a {name}: picco di {ItalianFormat.Money(peak)} {ItalianFormat.Weekday(peakDate.DayOfWeek)} {peakDay}");
    }
}
