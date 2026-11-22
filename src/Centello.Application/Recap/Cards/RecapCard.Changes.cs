namespace Centello.Application;

public abstract partial record RecapCard
{
    /// <summary>The category that grew most and the one that fell most against the previous period, in euros and not in
    /// percent. Each needs a change worth saying; a rise needs something to grow from. At least one is set.</summary>
    public sealed record Changes(RecapChange? Rise, RecapChange? Fall) : RecapCard
    {
        private const string Minus = "−";

        public override RecapSurface Top => (Rise ?? Fall)!.Surface;
        public override RecapSurface Bottom => (Fall ?? Rise)!.Surface;

        public override T Accept<T>(IRecapCardVisitor<T> visitor) => visitor.Changes(this);

        internal static RecapCard? Of(RecapScope scope)
        {
            if (!scope.Spending.IsComparable)
                return null;

            var span = scope.Span;
            var minimum = scope.Rules.MinChangeCents;
            var rise = scope.Spending.Categories
                .Where(spend => spend.Prior > 0 && spend.Delta >= minimum)
                .MaxBy(spend => spend.Delta);
            var fall = scope.Spending.Categories
                .Where(spend => spend.Delta <= -minimum)
                .MinBy(spend => spend.Delta);
            if (rise is null && fall is null)
                return null;

            return new Changes(
                rise is null ? null : Change(scope, rise, $"+{RecapTexts.Total(span, rise.Delta)}", $" {RecapTexts.Versus(span)}"),
                fall is null ? null : Change(scope, fall, $"{Minus}{RecapTexts.Total(span, -fall.Delta)}", ""))
            {
                Row = RowOf(rise, fall)
            };
        }

        private static RecapChange Change(RecapScope scope, CategorySpend spend, string amount, string versus) => new(
            scope.Catalog.Category(spend.Category.CategoryId), amount,
            $"Da {RecapTexts.Total(scope.Span, spend.Prior)} a {RecapTexts.TotalMoney(scope.Span, spend.Spent)}{versus}.");

        private static RecapSummaryRow RowOf(CategorySpend? rise, CategorySpend? fall) => rise is not null
            ? new RecapSummaryRow(MaterialSymbols.NorthEast, "Cresciuta di più",
                $"{rise.Category.Name} +{ItalianFormat.WholeMoney(rise.Delta)}")
            : new RecapSummaryRow(MaterialSymbols.SouthEast, "Calata di più",
                $"{fall!.Category.Name} {Minus}{ItalianFormat.WholeMoney(-fall.Delta)}");
    }
}
