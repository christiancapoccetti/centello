using Centello.Domain;

namespace Centello.Application;

/// <summary>The categories of the Categorie tab, ready to bind. Strings are Italian.</summary>
public sealed record CategoryList(IReadOnlyList<CategoryRow> Active, IReadOnlyList<CategoryRow> Archived);

/// <summary>Detail is "Novembre: 128,50 € · budget 100 €" for a category in use, with "Nessun budget" when it has
/// none, and "Archiviata" for an archived one.</summary>
public sealed record CategoryRow(Guid CategoryId, string Name, string Detail, string IconKey, string ColorKey,
    bool IsOverBudget = false)
{
    public string Description => RowLabels.Description(Name, Detail, IsOverBudget ? "oltre il budget" : "");
}

public sealed class CategoryListService(ICategoryRepository categories, IExpenseRepository expenses,
    Func<DateOnly>? today = null)
{
    private readonly Func<DateOnly> _today = today ?? (() => DateOnly.FromDateTime(DateTime.Today));

    /// <summary>Both lists by name. A category in use says what it spent in the month that contains today.</summary>
    public async Task<Result<CategoryList>> Get(CancellationToken cancellationToken = default)
    {
        // The reads are awaited one after the other: the repositories share a scoped DbContext, which never allows
        // two operations at once.
        var categoryResult = await categories.GetAll(cancellationToken);
        if (categoryResult.IsFailure)
            return Result<CategoryList>.Failure(categoryResult.Error);

        var month = Period.MonthOf(_today());
        var expenseResult = await expenses.GetBetween(month.Start, month.End, cancellationToken);
        if (expenseResult.IsFailure)
            return Result<CategoryList>.Failure(expenseResult.Error);

        var spentByCategory = expenseResult.Value
            .GroupBy(expense => expense.CategoryId)
            .ToDictionary(group => group.Key, group => group.Sum(expense => expense.Amount.Cents));
        var byName = categoryResult.Value
            .OrderBy(category => category.Name)
            .ToList();
        return Result<CategoryList>.Success(new CategoryList(
            byName
                .Where(category => !category.IsArchived)
                .Select(category =>
                {
                    var spent = spentByCategory.GetValueOrDefault(category.CategoryId);
                    return ToRow(category, InUse(category, month, spent), IsOverBudget(category, spent));
                })
                .ToList(),
            byName
                .Where(category => category.IsArchived)
                .Select(category => ToRow(category, "Archiviata"))
                .ToList()));
    }

    private static string InUse(Category category, Period month, long spent)
    {
        var budget = category.MonthlyBudget is { Cents: > 0 } monthly
            ? $"budget {ItalianFormat.Budget(monthly.Cents)} €"
            : "Nessun budget";
        return $"{ItalianFormat.PeriodName(month)}: {ItalianFormat.Money(spent)} · {budget}";
    }

    private static bool IsOverBudget(Category category, long spent) =>
        category.MonthlyBudget is { Cents: > 0 } monthly && spent > monthly.Cents;

    private static CategoryRow ToRow(Category category, string detail, bool isOverBudget = false) => new(category.CategoryId,
        category.Name, detail, CategoryIcons.Resolve(category.Icon).Key, CategoryPalette.Resolve(category.Color).Key,
        isOverBudget);
}
