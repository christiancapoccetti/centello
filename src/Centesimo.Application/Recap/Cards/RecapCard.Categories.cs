namespace Centesimo.Application;

public abstract partial record RecapCard
{
    public sealed record Categories(RecapPhrase Lead, IReadOnlyList<RecapShare> Shares) : RecapCard
    {
        public override RecapSurface Bottom => RecapSurface.Block(Shares[^1].Category.ColorKey);

        public override T Accept<T>(IRecapCardVisitor<T> visitor) => visitor.Categories(this);
    }
}
