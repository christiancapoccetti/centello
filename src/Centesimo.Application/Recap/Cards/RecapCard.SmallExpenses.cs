using Centesimo.Domain;

namespace Centesimo.Application;

public abstract partial record RecapCard
{
    /// <summary>What the expenses under 5 euros add up to, one square each in its category's ink. Habit card: variable
    /// expenses only. A month names the tag that gathers most of them in <see cref="TopTag"/>; a year says it in the
    /// detail.</summary>
    public sealed record SmallExpenses(string Lead, string Amount, RecapPhrase Detail, RecapUnits Units,
        RecapTagLine? TopTag) : RecapCard
    {
        private const int NamedCategories = 3;

        public override T Accept<T>(IRecapCardVisitor<T> visitor) => visitor.SmallExpenses(this);

        internal static RecapCard? Of(RecapScope scope)
        {
            var small = scope.Variable
                .Where(expense => expense.Amount.Cents < RecapRules.SmallLimitCents)
                .ToList();
            if (small.Count < scope.Rules.MinSmall)
                return null;

            var top = TopTagOf(scope, small);
            var count = RecapTexts.Count(small.Count, "spesa", "spese");
            var isMonth = scope.Span.Period.Kind == PeriodKind.Month;
            var detail = isMonth || top is null
                ? new RecapPhrase($"in {count}")
                : new RecapPhrase($"in {count}. Il primo: ", $"{top.Tag.Name}, {RecapTexts.Times(top.Count)}", ".");
            var total = small.Sum(expense => expense.Amount.Cents);
            return new SmallExpenses($"Le spese sotto i {ItalianFormat.WholeMoney(RecapRules.SmallLimitCents)} fanno",
                ItalianFormat.Amount(total), detail, UnitsOf(scope, small), isMonth && top is not null ? Line(top) : null);
        }

        private static TagUse? TopTagOf(RecapScope scope, List<Expense> small) =>
            TagUse.Of(small, scope.Catalog).FirstOrDefault();

        private static RecapTagLine Line(TagUse use) => new(use.Category, use.Tag.Name,
            $"{RecapTexts.Times(use.Count)}, in media {ItalianFormat.Money((long)Math.Round((double)use.Cents / use.Count))}",
            ItalianFormat.Money(use.Cents));

        private static RecapUnits UnitsOf(RecapScope scope, List<Expense> small)
        {
            var categories = small
                .GroupBy(expense => scope.Catalog.Category(expense.CategoryId))
                .Select(group => (Category: group.Key, Count: group.Count()))
                .OrderByDescending(group => group.Count)
                .ThenBy(group => group.Category.Name, StringComparer.Ordinal)
                .ToList();
            var colorKeys = categories
                .SelectMany(group => Enumerable.Repeat(CategoryPalette.Resolve(group.Category.ColorKey).Key, group.Count))
                .ToList();
            var named = categories
                .Take(NamedCategories)
                .Select(group => $"{ItalianFormat.Count(group.Count)} in {group.Category.Name}")
                .ToList();
            var others = categories.Skip(NamedCategories).Sum(group => group.Count);
            if (others > 0)
                named.Add($"{ItalianFormat.Count(others)} in altre categorie");

            return new RecapUnits(colorKeys,
                $"{RecapTexts.Count(small.Count, "spesa", "spese")} sotto i {ItalianFormat.WholeMoney(RecapRules.SmallLimitCents)}: {string.Join(", ", named)}");
        }
    }
}
