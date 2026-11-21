namespace Centesimo.Application;

public abstract partial record RecapCard
{
    public sealed record SmallExpenses(string Lead, string Amount, RecapPhrase Detail, RecapUnits Units,
        RecapTagLine? TopTag) : RecapCard
    {
        public override T Accept<T>(IRecapCardVisitor<T> visitor) => visitor.SmallExpenses(this);
    }
}
