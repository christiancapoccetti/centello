namespace Centesimo.Application;

public abstract partial record RecapCard
{
    public sealed record Summary(string Headline, IReadOnlyList<RecapSummaryRow> Rows, RecapExit Exit, string Restart) : RecapCard
    {
        public override T Accept<T>(IRecapCardVisitor<T> visitor) => visitor.Summary(this);
    }
}
