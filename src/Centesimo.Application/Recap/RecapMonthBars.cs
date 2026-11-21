namespace Centesimo.Application;

/// <summary>The twelve months of a year as bars, shared by the cover and the months card: the dearest whole month
/// Strong, the lightest Outlined, both with their euros.</summary>
internal sealed record RecapMonthChart(RecapBars Bars, int Dearest, int Lightest, IReadOnlyDictionary<int, long> Cents,
    int WholeMonths)
{
    /// <summary>Money cards: every expense counts, automatic ones included.</summary>
    public static RecapMonthChart Of(RecapScope scope)
    {
        var span = scope.Span;
        var year = span.Period.Start.Year;
        var cents = scope.Spending.Current
            .GroupBy(expense => expense.OccurredOn.Month)
            .ToDictionary(month => month.Key, month => month.Sum(expense => expense.Amount.Cents));
        var peak = cents.Count == 0 ? 0 : cents.Values.Max();
        var ranked = cents.Where(month => IsWhole(span, year, month.Key)).ToList();
        var dearest = ranked.Count == 0 ? 0 : ranked.MaxBy(month => month.Value).Key;
        var lightest = ranked.Count == 0 ? 0 : ranked.MinBy(month => month.Value).Key;
        var bars = Enumerable.Range(1, 12)
            .Select(month => Bar(month, cents.GetValueOrDefault(month), peak, dearest, lightest))
            .ToList();
        var description = $"Spesa per mese nel {year}";
        if (dearest == 0)
            return new RecapMonthChart(new RecapBars(bars, [], $"{description}."), 0, 0, cents, 0);

        description += $": {ItalianFormat.MonthName(dearest)} il più caro con {ItalianFormat.WholeMoney(cents[dearest])}";
        if (lightest != dearest)
            description += $", {ItalianFormat.MonthName(lightest)} il più leggero con {ItalianFormat.WholeMoney(cents[lightest])}";

        return new RecapMonthChart(new RecapBars(bars, [], description), dearest, lightest, cents, ranked.Count);
    }

    private static RecapBar Bar(int month, long cents, long peak, int dearest, int lightest)
    {
        var label = ItalianFormat.MonthInitial(month);
        if (cents == 0)
            return new RecapBar(0, RecapBarKind.Stub, Label: label);

        var height = (double)cents / peak;
        if (month == dearest)
            return new RecapBar(height, RecapBarKind.Strong, ItalianFormat.WholeEuros(cents), label);

        return month == lightest
            ? new RecapBar(height, RecapBarKind.Outlined, ItalianFormat.WholeEuros(cents), label)
            : new RecapBar(height, RecapBarKind.Bar, Label: label);
    }

    // The month of the last day of a year so far is half spent: it is drawn, never ranked.
    private static bool IsWhole(RecapSpan span, int year, int month) =>
        !span.IsSoFar || new DateOnly(year, month, 1) < new DateOnly(span.Through.Year, span.Through.Month, 1)
        || span.Through.Day == DateTime.DaysInMonth(year, month);
}
