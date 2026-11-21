namespace Centesimo.Application;

public abstract partial record RecapCard
{
    /// <summary>The days with at least one expense: a calendar for a month, a heatmap for a year. A day without one is
    /// an empty square, never a reproach. Habit card: variable expenses only. Shown when at least half the days
    /// counted have one.</summary>
    public sealed record Consistency(string Lead, string Headline, RecapGrid Days, string Note) : RecapCard
    {
        private static readonly string[] WeekdayLabels = ["L", "M", "M", "G", "V", "S", "D"];

        public override T Accept<T>(IRecapCardVisitor<T> visitor) => visitor.Consistency(this);

        internal static RecapCard? Of(RecapScope scope)
        {
            var span = scope.Span;
            var used = scope.Variable.Select(expense => expense.OccurredOn).ToHashSet();
            if (used.Count < RecapRules.MinConsistency * scope.DaysCounted)
                return null;

            var isMonth = span.Period.Kind == PeriodKind.Month;
            var headline = RecapTexts.DaysOf(used.Count, scope.DaysCounted);
            var grid = isMonth ? Calendar(span.Period, used, headline) : Heatmap(span, used, headline);
            var summary = isMonth ? headline : $"{ItalianFormat.Count(used.Count)} su {ItalianFormat.Count(scope.DaysCounted)}";
            return new Consistency("Hai registrato spese in", headline, grid,
                "Un quadrato pieno per ogni giorno con almeno una spesa.")
            {
                Row = new RecapSummaryRow(MaterialSymbols.CheckCircle, "Costanza", summary)
            };
        }

        private static RecapGrid Calendar(Period period, HashSet<DateOnly> used, string headline)
        {
            var leading = ((int)period.Start.DayOfWeek + 6) % 7;
            var days = Enumerable.Range(0, period.End.Day)
                .Select(offset => Cell(used, period.Start.AddDays(offset)));
            return new RecapGrid(7, WeekdayLabels, [], [.. Enumerable.Repeat(RecapCell.None, leading), .. days],
                $"Spese registrate in {headline}");
        }

        private static RecapGrid Heatmap(RecapSpan span, HashSet<DateOnly> used, string headline)
        {
            var year = span.Period.Start.Year;
            var cells = Enumerable.Range(1, 12)
                .SelectMany(month => Enumerable.Range(1, 31)
                    .Select(day => day > DateTime.DaysInMonth(year, month) || new DateOnly(year, month, day) > span.Through
                        ? RecapCell.None
                        : Cell(used, new DateOnly(year, month, day))))
                .ToList();
            return new RecapGrid(31, [], [.. Enumerable.Range(1, 12).Select(ItalianFormat.MonthInitial)], cells,
                $"Spese registrate in {headline}, un quadrato per giorno, un mese per riga");
        }

        private static RecapCell Cell(HashSet<DateOnly> used, DateOnly day) => used.Contains(day) ? RecapCell.Filled : RecapCell.Empty;
    }
}
