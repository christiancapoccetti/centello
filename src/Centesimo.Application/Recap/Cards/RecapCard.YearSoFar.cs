namespace Centesimo.Application;

public abstract partial record RecapCard
{
    public sealed record YearSoFar(string Lead, string Amount, RecapChip? Chip, RecapBars Chart, string ThisYear,
        RecapPhrase LastYear) : RecapCard
    {
        public override T Accept<T>(IRecapCardVisitor<T> visitor) => visitor.YearSoFar(this);
    }
}
