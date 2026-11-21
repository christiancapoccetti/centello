namespace Centesimo.Application;

public abstract partial record RecapCard
{
    /// <summary>The signature of the recap: the days of a month, or the twelve months of a year, as bars.</summary>
    public sealed record Cover(string Title, string Subtitle, RecapBars Chart, string Hint) : RecapCard
    {
        public override T Accept<T>(IRecapCardVisitor<T> visitor) => visitor.Cover(this);

        internal static Cover Of(RecapScope scope)
        {
            var span = scope.Span;
            return span.Period.Kind switch
            {
                PeriodKind.Month => new Cover($"Il tuo\n{ItalianFormat.MonthName(span.Period.Start.Month)}",
                    "Le spese del mese, in poche schede.", RecapDayBars.Of(scope, strongPeak: false), "Tocca per andare avanti"),
                PeriodKind.Year => new Cover($"Il tuo\n{span.Period.Start.Year}", YearSubtitle(span),
                    RecapMonthChart.Of(scope).Bars, "Tocca per andare avanti")
            };
        }

        private static string YearSubtitle(RecapSpan span) => span.IsSoFar
            ? $"Fino {ItalianFormat.UntilLongDate(span.Through)}, in poche schede."
            : "Un anno di spese, in poche schede.";
    }
}
