using Centesimo.Domain;

namespace Centesimo.Application;

public abstract partial record RecapCard
{
    /// <summary>The year so far at the end of the month, against the same days a year earlier: the cumulative spending
    /// as bars and last year's as the dashed line. Shown from February on, when the same days a year earlier hold
    /// spending. Money card: every expense counts.</summary>
    public sealed record YearSoFar(string Lead, string Amount, RecapChip? Chip, RecapBars Chart, string ThisYear,
        RecapPhrase LastYear) : RecapCard
    {
        public override T Accept<T>(IRecapCardVisitor<T> visitor) => visitor.YearSoFar(this);

        internal static RecapCard? Of(RecapScope scope)
        {
            var period = scope.Span.Period;
            var month = period.Start.Month;
            if (scope.YearToDate is not { IsComparable: true } yearToDate || month < 2)
                return null;

            var year = period.Start.Year;
            var spent = yearToDate.Spent;
            var compared = yearToDate.Compared;
            var thisYear = Cumulative(yearToDate.Current, month);
            var lastYear = Cumulative(yearToDate.Previous, month);
            var peak = Math.Max(thisYear[^1], lastYear[^1]);
            var bars = Enumerable.Range(1, 12)
                .Select(index => index > month
                    ? new RecapBar(0, RecapBarKind.Stub)
                    : new RecapBar((double)thisYear[index - 1] / peak, index == month ? RecapBarKind.Strong : RecapBarKind.Bar))
                .ToList();
            var end = $"a fine {ItalianFormat.MonthName(month)}";
            var chart = new RecapBars(bars, [.. lastYear.Select(cents => (double)cents / peak)],
                $"Spesa accumulata nel {year} contro il {year - 1}: {ItalianFormat.WholeMoney(spent)} contro {ItalianFormat.WholeMoney(compared)} {end}");
            return new YearSoFar($"Nel {year} hai speso finora", ItalianFormat.WholeEuros(spent),
                ChipOf(year, spent - compared, end), chart, $"{year}",
                new RecapPhrase($"{year - 1}, ", ItalianFormat.WholeMoney(compared)));
        }

        private static RecapChip? ChipOf(int year, long difference, string end) => difference == 0
            ? null
            : new RecapChip($"{ItalianFormat.WholeMoney(Math.Abs(difference))} {(difference < 0 ? "meno" : "più")} del {year - 1} {end}",
                difference < 0 ? ComparisonTrend.Lower : ComparisonTrend.Higher);

        private static long[] Cumulative(IEnumerable<Expense> expenses, int months)
        {
            var byMonth = expenses
                .GroupBy(expense => expense.OccurredOn.Month)
                .ToDictionary(month => month.Key, month => month.Sum(expense => expense.Amount.Cents));
            var running = 0L;
            return [.. Enumerable.Range(1, months).Select(month => running += byMonth.GetValueOrDefault(month))];
        }
    }
}
