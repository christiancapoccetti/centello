namespace Centello.Application;

public abstract partial record RecapCard
{
    /// <summary>A tag used often this month and not at all in the previous one. Habit card: variable expenses only.</summary>
    public sealed record NewHabit(RecapCategory Category, string Lead, string Tag, RecapPhrase Detail, RecapBars Days,
        string From, string To) : RecapCard
    {
        public override RecapSurface Surface => RecapSurface.ForCategory(Category.ColorKey);

        public override T Accept<T>(IRecapCardVisitor<T> visitor) => visitor.NewHabit(this);

        internal static RecapCard? Of(RecapScope scope)
        {
            if (RecapTagPicks.NewHabits(scope).FirstOrDefault() is not { } habit)
                return null;

            var period = scope.Span.Period;
            var detail = new RecapPhrase("", RecapTexts.Times(habit.Count),
                $", {ItalianFormat.Money(habit.Cents)} in tutto, nella categoria {habit.Category.Name}. "
                + $"{RecapTexts.Capitalized(ItalianFormat.InPeriod(period.Previous()))} nessuna.");
            return new NewHabit(habit.Category, "Una nuova abitudine", habit.Tag.Name, detail, DaysOf(scope, habit),
                ItalianFormat.ShortDate(period.Start), ItalianFormat.ShortDate(period.End))
            {
                Row = new RecapSummaryRow(MaterialSymbols.FiberNew, "Nuova abitudine", habit.Tag.Name)
            };
        }

        private static RecapBars DaysOf(RecapScope scope, TagUse habit)
        {
            var period = scope.Span.Period;
            var surface = RecapSurface.ForCategory(habit.Category.ColorKey);
            var used = habit.Expenses.Select(expense => expense.OccurredOn.Day).Distinct().Order().ToList();
            var bars = Enumerable.Range(1, DateTime.DaysInMonth(period.Start.Year, period.Start.Month))
                .Select(day => used.Contains(day)
                    ? RecapTagPicks.InkBar(surface, habit.Category, 1)
                    : new RecapBar(0, RecapBarKind.Stub))
                .ToList();
            var days = RecapTexts.List([.. used.Select(RecapTexts.TheDay)]);
            return new RecapBars(bars, [], $"{habit.Tag.Name} {days} {ItalianFormat.MonthName(period.Start.Month)}");
        }
    }
}
