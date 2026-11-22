namespace Centello.Application;

public abstract partial record RecapCard
{
    /// <summary>Up to three tags a year used often and its previous year not at all, most used first. They span several
    /// categories, so the card stays on the ground. "da settembre" is the month of a tag's first use in the year. Habit
    /// card: variable expenses only.</summary>
    public sealed record NewHabits(string Headline, string Detail, IReadOnlyList<RecapTagLine> Habits) : RecapCard
    {
        public override T Accept<T>(IRecapCardVisitor<T> visitor) => visitor.NewHabits(this);

        internal static RecapCard? Of(RecapScope scope)
        {
            var habits = RecapTagPicks.NewHabits(scope);
            if (habits.Count == 0)
                return null;

            var last = scope.Span.Period.Start.Year - 1;
            var single = habits.Count == 1;
            return new NewHabits(single ? "Un'abitudine nuova" : $"{(habits.Count == 2 ? "Due" : "Tre")} abitudini nuove",
                single ? $"Nel {last} non c'era." : $"Nel {last} non c'erano.", [.. habits.Select(Line)]);
        }

        private static RecapTagLine Line(TagUse habit) => new(habit.Category, habit.Tag.Name,
            $"{habit.Category.Name}, {RecapTexts.Since(habit.Expenses.Min(expense => expense.OccurredOn.Month))}, {RecapTexts.Times(habit.Count)}",
            ItalianFormat.Money(habit.Cents));
    }
}
