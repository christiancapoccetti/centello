namespace Centesimo.Application;

public abstract partial record RecapCard
{
    /// <summary>The tag that cost the most euros in a year, when it is not the tag of the year and was used at least as
    /// often as that card asks. Same layout as <see cref="TagOfYear"/>.</summary>
    public sealed record CostliestTag(RecapCategory Category, string Lead, string Tag, RecapPhrase Detail, RecapBars Chart) : RecapCard
    {
        public override RecapSurface Surface => RecapSurface.ForCategory(Category.ColorKey);

        public override T Accept<T>(IRecapCardVisitor<T> visitor) => visitor.CostliestTag(this);

        internal static RecapCard? Of(RecapScope scope)
        {
            if (RecapTagPicks.Costliest(scope) is not { } use)
                return null;

            var detail = new RecapPhrase("", $"{ItalianFormat.Money(use.Cents)} in tutto", $", {RecapTexts.Times(use.Count)}");
            return new CostliestTag(use.Category, "Il tag che ti è costato di più", use.Tag.Name, detail, RecapTagPicks.MonthBars(use));
        }
    }
}
