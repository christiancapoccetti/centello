using Centesimo.Domain;

namespace Centesimo.Application;

public abstract partial record RecapCard
{
    /// <summary>How much of a year was already decided: the automatic expenses against the variable ones, and the
    /// regular payments one by one. From two payments and 5% of the total on. Money card: every expense counts.
    /// There is no payments repository: a payment is named by its latest note, else by its category.</summary>
    public sealed record Automatic(string Headline, RecapPhrase Detail, RecapSplit Split, IReadOnlyList<RecapLine> Payments,
        RecapPhrase Note) : RecapCard
    {
        private const string UnknownPayment = "Pagamento regolare";

        public override T Accept<T>(IRecapCardVisitor<T> visitor) => visitor.Automatic(this);

        internal static RecapCard? Of(RecapScope scope)
        {
            var span = scope.Span;
            var total = scope.Spending.Spent;
            var payments = scope.Spending.Current
                .Where(expense => expense.IsAutomatic)
                .GroupBy(expense => expense.RecurringPaymentId!.Value)
                .Select(payment => (Name: NameOf(scope, payment), Cents: payment.Sum(expense => expense.Amount.Cents)))
                .OrderByDescending(payment => payment.Cents)
                .ThenBy(payment => payment.Name, StringComparer.Ordinal)
                .ToList();
            var automatic = payments.Sum(payment => payment.Cents);
            if (payments.Count < RecapRules.MinAutomaticPayments || automatic < total * RecapRules.MinAutomaticShare)
                return null;

            var fraction = (double)automatic / total;
            var percent = (int)Math.Round(fraction * 100, MidpointRounding.AwayFromZero);
            var withoutLargest = payments.Skip(1).Sum(payment => payment.Cents);
            return new Automatic($"{RecapTexts.ThePercent(percent)}{ItalianFormat.Percent(percent)} era già deciso",
                new RecapPhrase("", RecapTexts.TotalMoney(span, automatic),
                    $" in spese automatiche, {RecapTexts.TotalMoney(span, total - automatic)} variabili."),
                new RecapSplit(fraction, "Automatiche", "Variabili"),
                [.. payments.Take(RecapRules.MaxPaymentLines).Select(payment => new RecapLine(payment.Name, ItalianFormat.Money(payment.Cents)))],
                new RecapPhrase($"Abbonamenti senza {payments[0].Name}: ", ItalianFormat.Money(withoutLargest),
                    span.IsSoFar ? " finora." : " l'anno."))
            {
                Row = new RecapSummaryRow(MaterialSymbols.Autorenew, "Automatico", ItalianFormat.Percent(percent))
            };
        }

        private static string NameOf(RecapScope scope, IGrouping<Guid, Expense> payment)
        {
            var note = payment
                .Where(expense => expense.Note.HasValue())
                .OrderByDescending(expense => expense.OccurredOn)
                .FirstOrDefault()
                ?.Note;
            if (note is not null)
                return note;

            var categoryId = payment.First().CategoryId;
            return scope.Catalog.Knows(categoryId) ? scope.Catalog.Category(categoryId).Name : UnknownPayment;
        }
    }
}
