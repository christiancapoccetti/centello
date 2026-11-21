namespace Centesimo.Application;

public abstract partial record RecapCard
{
    public sealed record Total(string Lead, string Amount, RecapPhrase Detail, IReadOnlyList<RecapChip> Chips,
        IReadOnlyList<RecapMeasure> Measures) : RecapCard
    {
        public override T Accept<T>(IRecapCardVisitor<T> visitor) => visitor.Total(this);
    }
}
