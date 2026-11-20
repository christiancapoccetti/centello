using Centesimo.Domain;

namespace Centesimo.Application;

/// <summary>The budget of a period. A year budget is twelve monthly budgets. Zero is "no budget": nothing to measure
/// against. Home, the category detail and Insight read every period budget through here.</summary>
internal static class PeriodBudget
{
    /// <summary>One category's budget for the period, or null when it has none. The archived state is not read: a
    /// category's own page keeps showing the budget it was left with.</summary>
    public static long? Of(Period period, Money? monthly) => monthly is { Cents: > 0 } value
        ? value.Cents * (period.Kind == PeriodKind.Year ? 12 : 1)
        : null;

    /// <summary>The budgets of the categories still in use, summed, or null when none has one. An archived category's
    /// budget no longer counts, while its spending still does.</summary>
    public static long? OfActive(Period period, IEnumerable<Category> categories)
    {
        var total = categories
            .Where(category => !category.IsArchived)
            .Sum(category => Of(period, category.MonthlyBudget) ?? 0);
        return total > 0 ? total : null;
    }
}
