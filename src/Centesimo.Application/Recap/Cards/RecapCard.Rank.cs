namespace Centesimo.Application;

public abstract partial record RecapCard
{
    public sealed record Rank(string Headline, string Detail, RecapBars Chart) : RecapCard
    {
        public override T Accept<T>(IRecapCardVisitor<T> visitor) => visitor.Rank(this);
    }
}
