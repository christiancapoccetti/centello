using Centesimo.Domain;

namespace Centesimo.Application;

/// <summary>The row under an Insight total that opens the recap of the period shown.</summary>
public sealed record InsightRecap(Period Period, string Title, string Detail)
{
    internal static InsightRecap? For(Period period, DateOnly today, IEnumerable<Expense> expenses) =>
        throw new NotImplementedException();
}
