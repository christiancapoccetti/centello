namespace Centesimo.Application;

public abstract partial record RecapCard
{
    public sealed record CostliestTag(RecapCategory Category, string Lead, string Tag, RecapPhrase Detail, RecapBars Chart) : RecapCard
    {
        public override RecapSurface Surface => RecapSurface.ForCategory(Category.ColorKey);

        public override T Accept<T>(IRecapCardVisitor<T> visitor) => visitor.CostliestTag(this);
    }
}
