using Centello.Domain;

namespace Centello.Application;

public abstract partial record RecapCard
{
    /// <summary>What the span cost, in how many expenses, and how it compares: with the previous period and, for a
    /// month, with the same month a year earlier (the season counts). Green only when it is lower.</summary>
    public sealed record Total(string Lead, string Amount, RecapPhrase Detail, IReadOnlyList<RecapChip> Chips,
        IReadOnlyList<RecapMeasure> Measures) : RecapCard
    {
        public override T Accept<T>(IRecapCardVisitor<T> visitor) => visitor.Total(this);

        internal static RecapCard? Of(RecapScope scope)
        {
            var span = scope.Span;
            var spent = scope.Spending.Spent;
            var comparisons = Comparisons(scope);
            var chips = Enumerable.Reverse(comparisons)
                .Select(comparison => Chip(span, spent, comparison))
                .OfType<RecapChip>()
                .ToList();
            return new Total(LeadOf(span), RecapTexts.Total(span, spent), DetailOf(scope), chips, MeasuresOf(span, spent, comparisons))
            {
                Row = new RecapSummaryRow(MaterialSymbols.Payments, "Totale", RecapTexts.TotalMoney(span, spent))
            };
        }

        private sealed record Comparison(Period Period, long Cents);

        // Oldest first, the order of the bars.
        private static List<Comparison> Comparisons(RecapScope scope)
        {
            var period = scope.Span.Period;
            var comparisons = new List<Comparison>();
            if (period.Kind == PeriodKind.Month && scope.SameMonthLastYear > 0)
                comparisons.Add(new Comparison(Period.Month(period.Start.Year - 1, period.Start.Month), scope.SameMonthLastYear));

            if (scope.Spending.IsComparable)
                comparisons.Add(new Comparison(period.Previous(), scope.Spending.Compared));

            return comparisons;
        }

        private static string LeadOf(RecapSpan span) => span.IsSoFar
            ? $"Fino {ItalianFormat.UntilLongDate(span.Through)} hai speso"
            : $"{RecapTexts.Capitalized(ItalianFormat.InPeriod(span.Period))} hai speso";

        private static RecapPhrase DetailOf(RecapScope scope)
        {
            var span = scope.Span;
            var count = $"in {RecapTexts.Count(scope.Spending.Current.Count, "spesa", "spese")}";
            return span.Period.Kind == PeriodKind.Month
                ? new RecapPhrase(count)
                : new RecapPhrase($"{count}, ", ItalianFormat.WholeMoney(MonthlyAverage(scope)), " al mese in media");
        }

        // A year so far averages over the months its days make up, so December is not owed to a year that is not over.
        private static long MonthlyAverage(RecapScope scope)
        {
            var year = scope.Span.Period.Start.Year;
            var daysInYear = DateTime.IsLeapYear(year) ? 366 : 365;
            return (long)Math.Round((double)scope.Spending.Spent * daysInYear / (scope.DaysCounted * 12));
        }

        private static RecapChip? Chip(RecapSpan span, long spent, Comparison comparison)
        {
            var difference = spent - comparison.Cents;
            if (difference == 0)
                return null;

            var words = $"{RecapTexts.TotalMoney(span, Math.Abs(difference))} {(difference < 0 ? "meno" : "più")} {RecapTexts.Against(span, comparison.Period)}";
            return new RecapChip(words, difference < 0 ? ComparisonTrend.Lower : ComparisonTrend.Higher);
        }

        private static List<RecapMeasure> MeasuresOf(RecapSpan span, long spent, List<Comparison> comparisons)
        {
            var peak = Math.Max(spent, comparisons.Count == 0 ? 0 : comparisons.Max(comparison => comparison.Cents));
            RecapMeasure Measure(string label, long cents, bool isCurrent) =>
                new(label, (double)cents / peak, RecapTexts.TotalMoney(span, cents), isCurrent);

            return
            [
                .. comparisons.Select(comparison => Measure(Label(span, comparison.Period), comparison.Cents, false)),
                Measure(CurrentLabel(span), spent, true)
            ];
        }

        private static string CurrentLabel(RecapSpan span) => span.Period.Kind switch
        {
            PeriodKind.Month => ItalianFormat.MonthTitle(span.Period.Start.Month),
            PeriodKind.Year => SoFar(span, $"{span.Period.Start.Year}", span.Through)
        };

        private static string Label(RecapSpan span, Period compared) => compared.Kind switch
        {
            PeriodKind.Month => compared.Start.Year == span.Period.Start.Year
                ? ItalianFormat.MonthTitle(compared.Start.Month)
                : $"{ItalianFormat.MonthTitle(compared.Start.Month)} {compared.Start.Year}",
            PeriodKind.Year => SoFar(span, $"{compared.Start.Year}", span.Compared.To)
        };

        private static string SoFar(RecapSpan span, string year, DateOnly until) =>
            span.IsSoFar ? $"{year} {ItalianFormat.UntilLongDate(until)}" : year;
    }
}
