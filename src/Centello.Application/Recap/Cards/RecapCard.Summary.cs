namespace Centello.Application;

public abstract partial record RecapCard
{
    /// <summary>The last card: a row from each card that was shown, so a row never says what no card did, and the way
    /// out.</summary>
    public sealed record Summary(string Headline, IReadOnlyList<RecapSummaryRow> Rows, RecapExit Exit, string Restart) : RecapCard
    {
        public override T Accept<T>(IRecapCardVisitor<T> visitor) => visitor.Summary(this);

        internal static Summary Of(RecapScope scope, IReadOnlyList<RecapCard> middle)
        {
            var period = scope.Span.Period;
            var rows = middle.Select(card => card.Row).OfType<RecapSummaryRow>().ToList();
            return period.Kind switch
            {
                PeriodKind.Month => new Summary($"{ItalianFormat.MonthTitle(period.Start.Month)} in breve", rows,
                    new RecapExit($"Vedi le spese di {ItalianFormat.MonthName(period.Start.Month)}", RecapExitTarget.MonthExpenses),
                    "Rivedi dall'inizio"),
                PeriodKind.Year => new Summary($"Il {period.Start.Year} in breve", rows,
                    new RecapExit($"Vedi il {period.Start.Year} in Insight", RecapExitTarget.Insight),
                    "Rivedi dall'inizio")
            };
        }
    }
}
