namespace Centesimo.Application;

public abstract partial record RecapCard
{
    public sealed record Consistency(string Lead, string Headline, RecapGrid Days, string Note) : RecapCard
    {
        public override T Accept<T>(IRecapCardVisitor<T> visitor) => visitor.Consistency(this);
    }
}
