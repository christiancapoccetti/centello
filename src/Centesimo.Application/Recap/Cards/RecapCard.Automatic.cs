namespace Centesimo.Application;

public abstract partial record RecapCard
{
    public sealed record Automatic(string Headline, RecapPhrase Detail, RecapSplit Split, IReadOnlyList<RecapLine> Payments,
        RecapPhrase Note) : RecapCard
    {
        public override T Accept<T>(IRecapCardVisitor<T> visitor) => visitor.Automatic(this);
    }
}
