using Centello.Domain;

namespace Centello.Application;

public sealed record RecapFlowTag(Guid? TagId, string Name, long AutomaticCents, long VariableCents)
{
    public long TotalCents => AutomaticCents + VariableCents;
}

public sealed record RecapFlowCategory(RecapCategory Category, IReadOnlyList<RecapFlowTag> Tags)
{
    public IReadOnlyList<RecapFlowTag> Tags { get; } = Tags;
    public long TotalCents { get; } = Tags.Sum(tag => tag.TotalCents);
}

public abstract partial record RecapCard
{
    public sealed record SpendingMap(RecapSpan Span, IReadOnlyList<RecapFlowCategory> Categories,
        RecapFlowCategory FocusCategory, RecapFlowTag FocusTag) : RecapCard
    {
        public new IReadOnlyList<RecapFlowCategory> Categories { get; } = Categories;
        public long TotalCents { get; } = Categories.Sum(category => category.TotalCents);

        public string PeriodLabel => Span.IsSoFar
            ? $"{ItalianFormat.PeriodTitle(Span.Period)} fino {ItalianFormat.UntilLongDate(Span.Through)}"
            : ItalianFormat.PeriodTitle(Span.Period);

        public string Headline => $"«{FocusTag.Name}» · {SpokenShare(FocusTag.TotalCents)} delle spese";

        public string Description => $"{PeriodLabel}. {Headline}. Categoria {FocusCategory.Category.Name}. " +
            $"Spese automatiche con questo tag, {SpokenShare(FocusTag.AutomaticCents)} del totale. " +
            $"Spese variabili con questo tag, {SpokenShare(FocusTag.VariableCents)} del totale.";

        public string Share(long cents)
        {
            var fraction = (decimal)cents * 100 / TotalCents;
            if (fraction is > 0 and < 1)
                return "<1%";

            return ItalianFormat.Percent(Math.Round(fraction, MidpointRounding.AwayFromZero));
        }

        public override T Accept<T>(IRecapCardVisitor<T> visitor) => visitor.SpendingMap(this);

        internal static SpendingMap? Of(RecapScope scope, RecapSummaryRow? categoryRow)
        {
            var categories = scope.Spending.Current
                .GroupBy(expense => expense.CategoryId)
                .Select(group => new RecapFlowCategory(scope.Catalog.Category(group.Key), TagsOf(group, scope.Catalog)))
                .OrderByDescending(category => category.TotalCents)
                .ThenBy(category => category.Category.Name, StringComparer.Ordinal)
                .ThenBy(category => category.Category.CategoryId)
                .ToList();
            if (categories.Count == 0)
                return null;

            var focus = categories
                .SelectMany(category => category.Tags
                    .Select(tag => (Category: category, Tag: tag)))
                .OrderByDescending(item => item.Tag.TagId.HasValue)
                .ThenByDescending(item => item.Tag.TotalCents)
                .First();

            var replacesCategories = scope.HasAutomatic && scope.Variable.Count > 0 && focus.Tag.TagId.HasValue &&
                focus.Tag.TotalCents >= scope.Spending.Spent * RecapRules.MinMapFocusShare;
            return new SpendingMap(scope.Span, categories, focus.Category, focus.Tag)
            {
                Row = replacesCategories ? categoryRow : null
            };
        }

        private string SpokenShare(long cents) => Share(cents) switch
        {
            "<1%" => "meno dell’1%",
            var share => share
        };

        private static List<RecapFlowTag> TagsOf(IEnumerable<Expense> expenses, RecapCatalog catalog) => expenses
            .GroupBy(expense => TagOf(expense, catalog))
            .Select(group => new RecapFlowTag(group.Key?.TagId, group.Key?.Name ?? "Senza tag",
                group.Where(expense => expense.IsAutomatic).Sum(expense => expense.Amount.Cents),
                group.Where(expense => !expense.IsAutomatic).Sum(expense => expense.Amount.Cents)))
            .OrderByDescending(tag => tag.TotalCents)
            .ThenBy(tag => tag.Name, StringComparer.Ordinal)
            .ThenBy(tag => tag.TagId)
            .ToList();

        private static Tag? TagOf(Expense expense, RecapCatalog catalog) =>
            expense.TagId is { } id && catalog.Tag(id) is { } tag && tag.CategoryId == expense.CategoryId ? tag : null;
    }
}
