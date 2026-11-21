namespace Centesimo.Application;

public abstract partial record RecapCard
{
    /// <summary>The dearest and the lightest whole month of a year, from six months with spending on. Money card: every
    /// expense counts.</summary>
    public sealed record Months(string Headline, string Detail, RecapBars Chart) : RecapCard
    {
        public override T Accept<T>(IRecapCardVisitor<T> visitor) => visitor.Months(this);

        internal static RecapCard? Of(RecapScope scope)
        {
            var chart = RecapMonthChart.Of(scope);
            if (chart.WholeMonths < RecapRules.MinWholeMonths || chart.Dearest == chart.Lightest)
                return null;

            var dearest = ItalianFormat.MonthName(chart.Dearest);
            var lightest = ItalianFormat.MonthName(chart.Lightest);
            return new Months(
                RecapTexts.Capitalized($"{dearest} il più caro, {lightest} il più leggero"),
                $"{ItalianFormat.WholeMoney(chart.Cents[chart.Dearest])} contro {ItalianFormat.WholeMoney(chart.Cents[chart.Lightest])}.",
                chart.Bars)
            {
                Row = new RecapSummaryRow(MaterialSymbols.CalendarMonth, "Il mese più caro",
                    ItalianFormat.MonthTitle(chart.Dearest))
            };
        }
    }
}
