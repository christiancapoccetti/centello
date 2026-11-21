namespace Centesimo.Application;

public abstract partial record RecapCard
{
    /// <summary>Where the money went: a block per category, as tall as its share. Past six named categories the rest
    /// fold into one "Altre N categorie" block, unless that would hide a single category.</summary>
    public sealed record Categories(RecapPhrase Lead, IReadOnlyList<RecapShare> Shares) : RecapCard
    {
        public override RecapSurface Bottom => RecapSurface.Block(Shares[^1].Category.ColorKey);

        public override T Accept<T>(IRecapCardVisitor<T> visitor) => visitor.Categories(this);

        internal static RecapCard? Of(RecapScope scope)
        {
            var span = scope.Span;
            var spent = scope.Spending.Spent;
            var spends = scope.Spending.Current
                .GroupBy(expense => expense.CategoryId)
                .Select(group => (Category: scope.Catalog.Category(group.Key), Cents: group.Sum(expense => expense.Amount.Cents)))
                .OrderByDescending(spend => spend.Cents)
                .ThenBy(spend => spend.Category.Name, StringComparer.Ordinal)
                .ToList();
            if (spends.Count < RecapRules.MinCategories)
                return null;

            var folded = spends.Count > RecapRules.MaxNamedCategories + 1;
            var shares = spends
                .Take(folded ? RecapRules.MaxNamedCategories : spends.Count)
                .Select(spend => Share(span, spend.Category, spend.Cents, spent))
                .ToList();
            if (folded)
                shares.Add(Others(span, spends.Skip(RecapRules.MaxNamedCategories).ToList(), spent));

            var first = shares[0];
            return new Categories(new RecapPhrase("Dove sono andati i ", RecapTexts.TotalMoney(span, spent)), shares)
            {
                Row = new RecapSummaryRow(MaterialSymbols.DonutSmall, "Prima categoria", $"{first.Category.Name} · {first.Share}")
            };
        }

        private static RecapShare Others(RecapSpan span, List<(RecapCategory Category, long Cents)> rest, long spent)
        {
            var others = new RecapCategory(Guid.Empty, $"Altre {rest.Count} categorie", CategoryIcons.Fallback.Key,
                CategoryPalette.Fallback.Key);
            return Share(span, others, rest.Sum(spend => spend.Cents), spent);
        }

        private static RecapShare Share(RecapSpan span, RecapCategory category, long cents, long spent)
        {
            var fraction = (double)cents / spent;
            return new RecapShare(category, fraction, RecapTexts.TotalMoney(span, cents),
                ItalianFormat.Percent((int)Math.Round(fraction * 100, MidpointRounding.AwayFromZero)));
        }
    }
}
