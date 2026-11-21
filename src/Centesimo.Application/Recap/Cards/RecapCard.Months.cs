namespace Centesimo.Application;

public abstract partial record RecapCard
{
    public sealed record Months(string Headline, string Detail, RecapBars Chart) : RecapCard
    {
        public override T Accept<T>(IRecapCardVisitor<T> visitor) => visitor.Months(this);
    }
}
