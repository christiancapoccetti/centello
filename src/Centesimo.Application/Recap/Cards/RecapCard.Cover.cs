namespace Centesimo.Application;

public abstract partial record RecapCard
{
    /// <summary>The signature of the recap: the days of a month, or the twelve months of a year, as bars.</summary>
    public sealed record Cover(string Title, string Subtitle, RecapBars Chart, string Hint) : RecapCard
    {
        public override T Accept<T>(IRecapCardVisitor<T> visitor) => visitor.Cover(this);

        internal static Cover Of(RecapScope scope)
        {
            var span = scope.Span;
            return span.Period.Kind switch
            {
                PeriodKind.Month => new Cover($"Il tuo\n{ItalianFormat.MonthName(span.Period.Start.Month)}",
                    "Le spese del mese, in poche schede.", DayBars(scope), "Tocca per andare avanti"),
                PeriodKind.Year => new Cover($"Il tuo\n{span.Period.Start.Year}", YearSubtitle(span), MonthBars(scope),
                    "Tocca per andare avanti")
            };
        }

        private static string YearSubtitle(RecapSpan span) => span.IsSoFar
            ? $"Fino {ItalianFormat.UntilLongDate(span.Through)}, in poche schede."
            : "Un anno di spese, in poche schede.";

        // Variable spending only: the day of the rent would tower over every month.
        private static RecapBars DayBars(RecapScope scope)
        {
            var period = scope.Span.Period;
            var cents = scope.Variable
                .GroupBy(expense => expense.OccurredOn.Day)
                .ToDictionary(day => day.Key, day => day.Sum(expense => expense.Amount.Cents));
            var peak = cents.Count == 0 ? 0 : cents.Values.Max();
            var bars = Enumerable.Range(1, DateTime.DaysInMonth(period.Start.Year, period.Start.Month))
                .Select(day => cents.GetValueOrDefault(day) is var spent and > 0
                    ? new RecapBar((double)spent / peak, RecapBarKind.Bar)
                    : new RecapBar(0, RecapBarKind.Stub))
                .ToList();
            var name = ItalianFormat.MonthName(period.Start.Month);
            if (peak == 0)
                return new RecapBars(bars, [], $"Spesa giorno per giorno a {name}.");

            var peakDay = cents.Where(day => day.Value == peak).Min(day => day.Key);
            var peakDate = new DateOnly(period.Start.Year, period.Start.Month, peakDay);
            return new RecapBars(bars, [],
                $"Spesa giorno per giorno a {name}: picco di {ItalianFormat.Money(peak)} {ItalianFormat.Weekday(peakDate.DayOfWeek)} {peakDay}");
        }

        private static RecapBars MonthBars(RecapScope scope)
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
                return new RecapBars(bars, [], $"{description}.");

            description += $": {ItalianFormat.MonthName(dearest)} il più caro con {ItalianFormat.WholeMoney(cents[dearest])}";
            if (lightest != dearest)
                description += $", {ItalianFormat.MonthName(lightest)} il più leggero con {ItalianFormat.WholeMoney(cents[lightest])}";

            return new RecapBars(bars, [], description);
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
}
