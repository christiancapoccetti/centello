namespace Centesimo.Application;

public abstract partial record RecapCard
{
    /// <summary>The weekday on which the most variable expenses fall, by count, when it is clearly more than a uniform
    /// week would give. Habit card: variable expenses only.</summary>
    public sealed record Weekday(string Headline, RecapPhrase Detail, RecapBars Days) : RecapCard
    {
        private static readonly DayOfWeek[] MondayFirst =
            [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday];

        public override T Accept<T>(IRecapCardVisitor<T> visitor) => visitor.Weekday(this);

        internal static RecapCard? Of(RecapScope scope)
        {
            var total = scope.Variable.Count;
            if (total < RecapRules.MinWeekdayExpenses)
                return null;

            var counts = MondayFirst.ToDictionary(day => day, day => scope.Variable.Count(expense => expense.OccurredOn.DayOfWeek == day));
            var top = MondayFirst.MaxBy(day => counts[day]);
            if ((double)counts[top] / total < RecapRules.MinWeekdayShare)
                return null;

            var name = ItalianFormat.Weekday(top);
            var percent = ItalianFormat.Percent(Percent(counts[top], total));
            var bars = MondayFirst
                .Select(day => new RecapBar((double)counts[day] / counts[top], day == top ? RecapBarKind.Strong : RecapBarKind.Bar,
                    ItalianFormat.Percent(Percent(counts[day], total)), RecapTexts.Capitalized(ItalianFormat.ShortWeekday(day))))
                .ToList();
            return new Weekday($"{RecapTexts.TheWeekday(top)} è il tuo giorno",
                new RecapPhrase("Il ", percent, $" delle spese variabili cade di {name}."),
                new RecapBars(bars, [], $"Quota delle spese variabili per giorno della settimana: {name} {percent}"))
            {
                Row = new RecapSummaryRow(MaterialSymbols.Weekend, "Il tuo giorno", RecapTexts.Capitalized(name))
            };
        }

        private static int Percent(int count, int total) => (int)Math.Round(100.0 * count / total, MidpointRounding.AwayFromZero);
    }
}
