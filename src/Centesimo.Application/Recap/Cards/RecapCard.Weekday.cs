namespace Centesimo.Application;

public abstract partial record RecapCard
{
    public sealed record Weekday(string Headline, RecapPhrase Detail, RecapBars Days) : RecapCard
    {
        public override T Accept<T>(IRecapCardVisitor<T> visitor) => visitor.Weekday(this);
    }
}
