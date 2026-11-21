namespace Centesimo.Application;

public abstract partial record RecapCard
{
    public sealed record RecordDay(string Lead, string Amount, RecapPhrase Detail, RecapBars? Days,
        IReadOnlyList<RecapExpense> Expenses, string Note) : RecapCard
    {
        public override T Accept<T>(IRecapCardVisitor<T> visitor) => visitor.RecordDay(this);
    }
}
