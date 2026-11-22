namespace Centello.Application;

public abstract partial record RecapCard
{
    /// <summary>The tag present in the most weeks of the year, other than the two the tag cards show. Weeks are seven
    /// days from 1 January, the last one taking the remaining one or two days, so a year reads "su 52" and a year so far
    /// "su 46". Habit card: variable expenses only.</summary>
    public sealed record SteadyTag(RecapCategory Category, string Lead, string Tag, RecapPhrase Detail, RecapBars Weeks,
        string Start, string Middle, string End) : RecapCard
    {
        private const int WeeksInYear = 52;

        public override RecapSurface Surface => RecapSurface.ForCategory(Category.ColorKey);

        public override T Accept<T>(IRecapCardVisitor<T> visitor) => visitor.SteadyTag(this);

        internal static RecapCard? Of(RecapScope scope)
        {
            var counted = WeekOf(scope.Span.Through) + 1;
            var taken = new[] { RecapTagPicks.TagOfYear(scope), RecapTagPicks.Costliest(scope) }
                .OfType<TagUse>()
                .Select(use => use.Tag.TagId)
                .ToHashSet();
            var steadiest = scope.TagUses
                .Where(use => !taken.Contains(use.Tag.TagId))
                .Select(use => (Use: use, Weeks: use.Expenses.Select(expense => WeekOf(expense.OccurredOn)).ToHashSet()))
                .OrderByDescending(candidate => candidate.Weeks.Count)
                .FirstOrDefault();
            if (steadiest.Use is null || steadiest.Weeks.Count < RecapRules.MinSteadyWeeks || steadiest.Weeks.Count * 2 < counted)
                return null;

            var (use, weeks) = steadiest;
            var surface = RecapSurface.ForCategory(use.Category.ColorKey);
            var bars = Enumerable.Range(0, counted)
                .Select(week => weeks.Contains(week)
                    ? RecapTagPicks.InkBar(surface, use.Category, 1)
                    : new RecapBar(0, RecapBarKind.Stub))
                .ToList();
            var span = $"{weeks.Count} settimane su {counted}";
            return new SteadyTag(use.Category, "La tua abitudine più costante", use.Tag.Name,
                new RecapPhrase("In ", span, $", {RecapTexts.Times(use.Count)}."),
                new RecapBars(bars, [], $"{use.Tag.Name} in {span}"), "Gennaio", "Una barra per settimana",
                scope.Span.IsSoFar ? ItalianFormat.MonthTitle(scope.Span.Through.Month) : "Dicembre")
            {
                Row = new RecapSummaryRow(MaterialSymbols.Repeat, "La più costante", use.Tag.Name)
            };
        }

        private static int WeekOf(DateOnly day) => Math.Min((day.DayOfYear - 1) / 7, WeeksInYear - 1);
    }
}
