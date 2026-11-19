using Centesimo.Domain;

namespace Centesimo.Application;

/// <summary>What the history lists. A tag filter only makes sense inside a category, so the filter is built through
/// <see cref="All"/> or <see cref="ForCategory"/> and a tag without a category cannot be expressed.</summary>
public sealed record ExpenseFilter
{
    private ExpenseFilter(Guid? categoryId, TagFilter tag)
    {
        CategoryId = categoryId;
        Tag = tag;
    }

    public static ExpenseFilter All { get; } = new(null, TagFilter.All);

    public static ExpenseFilter ForCategory(Guid categoryId, TagFilter? tag = null) => new(categoryId, tag ?? TagFilter.All);

    public Guid? CategoryId { get; }
    public TagFilter Tag { get; }

    public bool Matches(Expense expense) =>
        (!CategoryId.HasValue || expense.CategoryId == CategoryId) && Tag.Matches(expense.TagId);
}

/// <summary>Every expense of one period, grouped and ready to bind. Strings are Italian.</summary>
public sealed record ExpenseHistory(
    Period Period,
    HomeNavigation Navigation,
    string Title,                              // "Novembre 2026" | "Anno 2026"
    string Total,                              // "1.284,60 €", of the listed expenses
    IReadOnlyList<HistoryGroup> Groups,
    IReadOnlyList<HistoryFilterChip> CategoryChips,
    string EmptyText)                          // "Nessuna spesa in Ristoranti a novembre"
{
    public bool HasGroups => Groups.Count > 0;
    public bool HasNoGroups => Groups.Count == 0;
}

public sealed record HistoryFilterChip(Guid CategoryId, string Name, string ColorKey, bool IsSelected);

public sealed class ExpenseHistoryService(ICategoryRepository categories, ITagRepository tags,
    IExpenseRepository expenses, Func<DateOnly>? today = null)
{
    private readonly Func<DateOnly> _today = today ?? (() => DateOnly.FromDateTime(DateTime.Today));

    public async Task<Result<ExpenseHistory>> Get(Period period, ExpenseFilter filter,
        CancellationToken cancellationToken = default)
    {
        // The reads are awaited one after the other: the repositories share a scoped DbContext, which never allows
        // two operations at once.
        var categoryResult = await categories.GetAll(cancellationToken);
        if (categoryResult.IsFailure)
            return Result<ExpenseHistory>.Failure(categoryResult.Error);

        var tagResult = await tags.GetAll(cancellationToken);
        if (tagResult.IsFailure)
            return Result<ExpenseHistory>.Failure(tagResult.Error);

        var expenseResult = await expenses.GetBetween(period.Start, period.End, cancellationToken);
        if (expenseResult.IsFailure)
            return Result<ExpenseHistory>.Failure(expenseResult.Error);

        return Result<ExpenseHistory>.Success(
            Build(period, _today(), filter, categoryResult.Value, tagResult.Value, expenseResult.Value));
    }

    private static ExpenseHistory Build(Period period, DateOnly today, ExpenseFilter filter,
        IReadOnlyList<Category> categories, IReadOnlyList<Tag> tags, IReadOnlyList<Expense> periodExpenses)
    {
        var listed = periodExpenses.Where(filter.Matches).ToList();
        var categoryName = filter.CategoryId.HasValue
            ? RowLabels.NameOf(categories.FirstOrDefault(category => category.CategoryId == filter.CategoryId))
            : "";
        return new ExpenseHistory(period, HomeNavigation.For(period, today), ItalianFormat.PeriodTitle(period),
            ItalianFormat.Money(listed.Sum(expense => expense.Amount.Cents)),
            HistoryGroups.Build(period, today, listed, categories, tags), Chips(filter, categories, periodExpenses),
            HistoryGroups.EmptyText(period, categoryName, filter.Tag, tags));
    }

    // Active categories with spending in the period, largest first. The selected category stays even when it has
    // none, or is archived, so the row always shows what filters the list.
    private static IReadOnlyList<HistoryFilterChip> Chips(ExpenseFilter filter, IReadOnlyList<Category> categories,
        IReadOnlyList<Expense> periodExpenses)
    {
        var spentByCategory = periodExpenses
            .GroupBy(expense => expense.CategoryId)
            .ToDictionary(group => group.Key, group => group.Sum(expense => expense.Amount.Cents));
        return categories
            .Where(category => category.CategoryId == filter.CategoryId
                || !category.IsArchived && spentByCategory.ContainsKey(category.CategoryId))
            .OrderByDescending(category => spentByCategory.GetValueOrDefault(category.CategoryId))
            .ThenBy(category => category.Name)
            .Select(category => new HistoryFilterChip(category.CategoryId, category.Name,
                CategoryPalette.Resolve(category.Color).Key, category.CategoryId == filter.CategoryId))
            .ToList();
    }
}
