using Centesimo.Domain;

namespace Centesimo.Application;

/// <summary>Everything a card reads, cut once. Spending is the very <see cref="SpendComparison"/> Insight builds, so a
/// recap's total and comparison are Insight's for the same days.</summary>
internal sealed class RecapScope
{
    public required RecapSpan Span { get; init; }
    public required RecapRules Rules { get; init; }
    public required SpendComparison Spending { get; init; }

    /// <summary>A month only: 1 January to the month's end against the same days a year earlier.</summary>
    public SpendComparison? YearToDate { get; init; }

    /// <summary>A month only: what the same month a year earlier cost, 0 when nothing was spent.</summary>
    public long SameMonthLastYear { get; init; }

    /// <summary>The span's expenses without the automatic ones: the ones the person chose to make.</summary>
    public required IReadOnlyList<Expense> Variable { get; init; }

    public required RecapCatalog Catalog { get; init; }

    /// <summary>Variable uses per tag in the span, most used first, then the dearest.</summary>
    public required IReadOnlyList<TagUse> TagUses { get; init; }

    public bool HasAutomatic => Spending.Current.Any(expense => expense.IsAutomatic);
    public int DaysCounted => Span.Through.DayNumber - Span.Period.Start.DayNumber + 1;

    /// <summary><paramref name="expenses"/> covers 1 January of the year before the period's to the span's last day.</summary>
    public static RecapScope Of(RecapSpan span, IReadOnlyList<Category> categories, IReadOnlyList<Tag> tags,
        IReadOnlyList<Expense> expenses)
    {
        var period = span.Period;
        var spending = SpendComparison.Of(period, span.Window, categories, expenses);
        var catalog = new RecapCatalog(categories, tags);
        var variable = spending.Current.Where(expense => !expense.IsAutomatic).ToList();
        var isMonth = period.Kind == PeriodKind.Month;
        var year = Period.Year(period.Start.Year);
        return new RecapScope
        {
            Span = span,
            Rules = RecapRules.For(period.Kind),
            Spending = spending,
            YearToDate = isMonth
                ? SpendComparison.Of(year, ComparisonWindow.For(year, period.End), categories, expenses)
                : null,
            SameMonthLastYear = isMonth ? SpentIn(expenses, Period.Month(period.Start.Year - 1, period.Start.Month)) : 0,
            Variable = variable,
            Catalog = catalog,
            TagUses = TagUsesOf(variable, catalog)
        };
    }

    private static long SpentIn(IEnumerable<Expense> expenses, Period period) => expenses
        .Where(expense => period.Contains(expense.OccurredOn))
        .Sum(expense => expense.Amount.Cents);

    private static List<TagUse> TagUsesOf(IEnumerable<Expense> variable, RecapCatalog catalog) => variable
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
