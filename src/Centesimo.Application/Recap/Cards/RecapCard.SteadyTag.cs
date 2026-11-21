namespace Centesimo.Application;

public abstract partial record RecapCard
{
    public sealed record SteadyTag(RecapCategory Category, string Lead, string Tag, RecapPhrase Detail, RecapBars Weeks,
        string Start, string Middle, string End) : RecapCard
    {
        public override RecapSurface Surface => RecapSurface.ForCategory(Category.ColorKey);

        public override T Accept<T>(IRecapCardVisitor<T> visitor) => visitor.SteadyTag(this);
    }
}
