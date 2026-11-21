using Centesimo.Domain;

namespace Centesimo.Application;

/// <summary>The variable expenses carrying one tag in the span.</summary>
internal sealed record TagUse(Tag Tag, RecapCategory Category, IReadOnlyList<Expense> Expenses)
{
    public int Count => Expenses.Count;
    public long Cents => Expenses.Sum(expense => expense.Amount.Cents);

    /// <summary>The tags <paramref name="expenses"/> carry, most used first, then the dearest. A tag that is gone is
    /// left out.</summary>
    public static List<TagUse> Of(IEnumerable<Expense> expenses, RecapCatalog catalog) => expenses
        .Where(expense => expense.TagId.HasValue)
        .GroupBy(expense => expense.TagId!.Value)
        .Select(group => (Tag: catalog.Tag(group.Key), Expenses: group.ToList()))
        .Where(use => use.Tag is not null)
        .Select(use => new TagUse(use.Tag!, catalog.Category(use.Tag!.CategoryId), use.Expenses))
        .OrderByDescending(use => use.Count)
        .ThenByDescending(use => use.Cents)
        .ThenBy(use => use.Tag.Name, StringComparer.Ordinal)
        .ToList();
}
