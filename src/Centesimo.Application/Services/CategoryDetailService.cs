using Centesimo.Domain;

namespace Centesimo.Application;

/// <summary>One tag filter chip. <see cref="Filter"/> is what to pass back to <see cref="CategoryDetailService.Get"/>
/// when the chip is tapped.</summary>
public sealed record TagChip(TagFilter Filter, string Label, bool IsSelected);

/// <summary>Everything the page of one category shows for one period, ready to bind. Strings are Italian. The summary
/// (total, budget, bars) covers the whole category; only <see cref="Groups"/> follows the selected tag.</summary>
public sealed record CategoryDetail(
    Guid CategoryId,
    string Name,
    string IconKey,
    string ColorKey,
    bool IsArchived,
    Period Period,
    HomeNavigation Navigation,
    string Title,                       // "Novembre 2026" | "Anno 2026"
    string Total,                       // "128,50" (the page draws "€")
    BudgetSummary? Budget,              // null when the category has no budget
    IReadOnlyList<SpendBar> Bars,       // days of the month, or 12 months
    IReadOnlyList<TagChip> Tags,        // "Tutti", the tags with spending, "Senza tag" when there is untagged spending
    IReadOnlyList<HistoryGroup> Groups,
    string EmptyText)
{
    public bool HasBudget => Budget is not null;
    public bool HasGroups => Groups.Count > 0;
    public bool HasNoGroups => Groups.Count == 0;
}

public sealed class CategoryDetailService(ICategoryRepository categories, ITagRepository tags,
    IExpenseRepository expenses, Func<DateOnly>? today = null)
{
    private readonly Func<DateOnly> _today = today ?? (() => DateOnly.FromDateTime(DateTime.Today));

    public async Task<Result<CategoryDetail>> Get(Guid categoryId, Period period, TagFilter tag,
        CancellationToken cancellationToken = default)
    {
        // The reads are awaited one after the other: the repositories share a scoped DbContext, which never allows
        // two operations at once.
        var categoryResult = await categories.Get(categoryId, cancellationToken);
        if (categoryResult.IsFailure)
            return Result<CategoryDetail>.Failure(categoryResult.Error);

        if (categoryResult.Value is not { } category)
            return Result<CategoryDetail>.Failure(ApplicationErrors.CategoryNotFound);

        var tagResult = await tags.GetByCategory(categoryId, cancellationToken);
        if (tagResult.IsFailure)
            return Result<CategoryDetail>.Failure(tagResult.Error);

        var expenseResult = await expenses.GetByCategoryBetween(categoryId, period.Start, period.End, cancellationToken);
        if (expenseResult.IsFailure)
            return Result<CategoryDetail>.Failure(expenseResult.Error);

        return Result<CategoryDetail>.Success(
            Build(category, period, _today(), tag, tagResult.Value, expenseResult.Value));
    }

    private static CategoryDetail Build(Category category, Period period, DateOnly today, TagFilter selected,
        IReadOnlyList<Tag> tags, IReadOnlyList<Expense> categoryExpenses)
    {
        var spent = categoryExpenses.Sum(expense => expense.Amount.Cents);
        var listed = categoryExpenses.Where(expense => selected.Matches(expense.TagId)).ToList();
        var buckets = SpendBuckets.Of(period, today, categoryExpenses);
        return new CategoryDetail(category.CategoryId, category.Name, CategoryIcons.Resolve(category.Icon).Key,
            CategoryPalette.Resolve(category.Color).Key, category.IsArchived, period, HomeNavigation.For(period, today),
            ItalianFormat.PeriodTitle(period), ItalianFormat.Amount(spent), Summarize(category, period, spent),
            buckets.Bars(buckets.Scale()), Chips(selected, tags, categoryExpenses),
            HistoryGroups.Build(period, today, listed, [category], tags),
            HistoryGroups.EmptyText(period, category.Name, selected, tags));
    }

    // A year budget is twelve monthly budgets. A budget of zero is "no budget": nothing to measure against.
    private static BudgetSummary? Summarize(Category category, Period period, long spent)
    {
        if (category.MonthlyBudget is not { Cents: > 0 } monthly)
            return null;

        var budget = monthly.Cents * (period.Kind == PeriodKind.Year ? 12 : 1);
        return spent > budget
            ? new BudgetSummary("Oltre il budget di ", ItalianFormat.Money(spent - budget), "")
            : new BudgetSummary("Ne restano ", ItalianFormat.Money(budget - spent), $" su {ItalianFormat.Budget(budget)}");
    }

    // Tags with spending in the period, largest first, between "Tutti" and "Senza tag". The selected tag stays even
    // when it has none, or is archived, so the row always shows what filters the list.
    private static IReadOnlyList<TagChip> Chips(TagFilter selected, IReadOnlyList<Tag> tags,
        IReadOnlyList<Expense> categoryExpenses)
    {
        var spentByTag = categoryExpenses
            .Where(expense => expense.TagId.HasValue)
            .GroupBy(expense => expense.TagId!.Value)
            .ToDictionary(group => group.Key, group => group.Sum(expense => expense.Amount.Cents));
        var tagChips = tags
            .Select(tag => (Tag: tag, Filter: TagFilter.Tag(tag.TagId), Spent: spentByTag.GetValueOrDefault(tag.TagId)))
            .Where(item => item.Filter == selected || !item.Tag.IsArchived && item.Spent > 0)
            .OrderByDescending(item => item.Spent)
            .ThenBy(item => item.Tag.Name)
            .Select(item => new TagChip(item.Filter, item.Tag.Name, item.Filter == selected));
        List<TagChip> chips = [new TagChip(TagFilter.All, "Tutti", selected == TagFilter.All), .. tagChips];
        if (selected == TagFilter.Untagged || categoryExpenses.Any(expense => !expense.TagId.HasValue))
            chips.Add(new TagChip(TagFilter.Untagged, "Senza tag", selected == TagFilter.Untagged));

        return chips;
    }
}
