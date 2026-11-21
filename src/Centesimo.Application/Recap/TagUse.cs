using Centesimo.Domain;

namespace Centesimo.Application;

/// <summary>The variable expenses carrying one tag in the span.</summary>
internal sealed record TagUse(Tag Tag, RecapCategory Category, IReadOnlyList<Expense> Expenses)
{
    public int Count => Expenses.Count;
    public long Cents => Expenses.Sum(expense => expense.Amount.Cents);
}
