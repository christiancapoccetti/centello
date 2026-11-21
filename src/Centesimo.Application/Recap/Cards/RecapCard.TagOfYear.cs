namespace Centesimo.Application;

public abstract partial record RecapCard
{
    /// <summary>The tag used the most times in a year, from twelve uses on. Habit card: variable expenses only. Tags have
    /// no icon: the tile and the surface are the tag's category's.</summary>
    public sealed record TagOfYear(RecapCategory Category, string Lead, string Tag, RecapPhrase Detail, RecapBars Chart) : RecapCard
    {
        public override RecapSurface Surface => RecapSurface.ForCategory(Category.ColorKey);

        public override T Accept<T>(IRecapCardVisitor<T> visitor) => visitor.TagOfYear(this);

        internal static RecapCard? Of(RecapScope scope)
        {
            if (RecapTagPicks.TagOfYear(scope) is not { } use)
                return null;

            var average = (long)Math.Round((double)use.Cents / use.Count);
            var detail = new RecapPhrase("", RecapTexts.Times(use.Count),
                $", {ItalianFormat.Money(use.Cents)} in tutto, {ItalianFormat.Money(average)} in media.");
            return new TagOfYear(use.Category, "Il tag dell'anno", use.Tag.Name, detail, RecapTagPicks.MonthBars(use))
            {
                Row = new RecapSummaryRow(MaterialSymbols.Sell, "Il tag dell'anno", $"{use.Tag.Name} · {use.Count}")
            };
        }
    }
}
