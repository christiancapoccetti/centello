namespace Centesimo.Application;

public abstract partial record RecapCard
{
    public sealed record Cover(string Title, string Subtitle, RecapBars Chart, string Hint) : RecapCard
    {
        public override T Accept<T>(IRecapCardVisitor<T> visitor) => visitor.Cover(this);
    }
}
