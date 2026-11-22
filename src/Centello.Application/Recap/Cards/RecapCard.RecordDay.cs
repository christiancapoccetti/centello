namespace Centello.Application;

public abstract partial record RecapCard
{
    /// <summary>The day with the most variable spending. Automatic expenses are left out, or the day of the rent would
    /// win every month.</summary>
    public sealed record RecordDay(string Lead, string Amount, RecapPhrase Detail, RecapBars? Days,
        IReadOnlyList<RecapExpense> Expenses, string Note) : RecapCard
    {
        private const int ShownExpenses = 3;

        public override T Accept<T>(IRecapCardVisitor<T> visitor) => visitor.RecordDay(this);

        internal static RecapCard? Of(RecapScope scope)
        {
            var days = scope.Variable
                .GroupBy(expense => expense.OccurredOn)
                .Select(day => (Day: day.Key, Expenses: day.ToList(), Cents: day.Sum(expense => expense.Amount.Cents)))
                .ToList();
            if (days.Count < RecapRules.MinRecordDays)
                return null;

            var record = days
                .OrderByDescending(day => day.Cents)
                .ThenBy(day => day.Day)
                .First();
            var isMonth = scope.Span.Period.Kind == PeriodKind.Month;
            var expenses = record.Expenses
                .OrderByDescending(expense => expense.Amount.Cents)
                .ThenBy(expense => expense.Note, StringComparer.Ordinal)
                .Take(ShownExpenses)
                .Select(expense => new RecapExpense(expense.ExpenseId, scope.Catalog.Category(expense.CategoryId),
                    expense.Note, ItalianFormat.Money(expense.Amount.Cents)))
                .ToList();
            var detail = new RecapPhrase("", RecapTexts.WeekdayLongDate(record.Day),
                $", in {RecapTexts.Count(record.Expenses.Count, "spesa", "spese")}");
            return new RecordDay(isMonth ? "Il giorno più caro" : "Il giorno più caro dell'anno",
                ItalianFormat.Amount(record.Cents), detail, isMonth ? RecapDayBars.Of(scope, strongPeak: true) : null,
                expenses, scope.HasAutomatic ? "Senza le spese automatiche." : "")
            {
                Row = new RecapSummaryRow(MaterialSymbols.Event, "Il giorno più caro",
                    $"{RowDay(record.Day, isMonth)} · {ItalianFormat.Money(record.Cents)}")
            };
        }

        private static string RowDay(DateOnly day, bool isMonth) => isMonth
            ? $"{ItalianFormat.ShortWeekday(day.DayOfWeek)} {day.Day}"
            : ItalianFormat.ShortDate(day);
    }
}
