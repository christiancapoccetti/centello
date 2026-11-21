using Centesimo.Domain;

namespace Centesimo.Application;

/// <summary>The row under an Insight total that opens the recap of the period shown.</summary>
public sealed record InsightRecap(Period Period, string Title, string Detail)
{
    /// <summary>Null for the month of today, the future and a thin period. The expenses are what Insight already
    /// loaded: <see cref="RecapSpan.HasEnough"/> cuts them at the span, so the row and the deck answer alike.</summary>
    internal static InsightRecap? For(Period period, DateOnly today, IEnumerable<Expense> expenses)
    {
        if (RecapSpan.Of(period, today) is not { } span || !span.HasEnough(expenses))
            return null;

        return span.IsSoFar
            ? new InsightRecap(period, $"Il tuo {period.Start.Year} finora", $"Fino {ItalianFormat.UntilLongDate(span.Through)}, in poche schede")
            : new InsightRecap(period, $"Rivedi il tuo {Name(period)}", $"Il riepilogo {(period.Kind == PeriodKind.Month ? "del mese" : "dell'anno")}, in poche schede");
    }

    private static string Name(Period period) => period.Kind == PeriodKind.Month
        ? ItalianFormat.MonthName(period.Start.Month)
        : $"{period.Start.Year}";
}
