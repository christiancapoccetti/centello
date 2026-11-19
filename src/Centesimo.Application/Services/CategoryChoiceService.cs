using Centesimo.Domain;

namespace Centesimo.Application;

/// <summary>The categories an editor offers when it asks where an expense goes.</summary>
public sealed class CategoryChoiceService(ICategoryRepository categories, IExpenseRepository expenses,
    Func<DateOnly>? today = null)
{
    private readonly Func<DateOnly> _today = today ?? (() => DateOnly.FromDateTime(DateTime.Today));

    /// <summary>Active categories, the ones with more spending this month first and the others by name, so the
    /// category used most is the first to reach. <paramref name="alsoOffer"/> adds one category at the end even when
    /// it is archived: an editor that only shows a stored choice needs it.</summary>
    public async Task<Result<IReadOnlyList<Category>>> Get(Guid? alsoOffer = null,
        CancellationToken cancellationToken = default)
    {
        // The reads are awaited one after the other: the repositories share a scoped DbContext, which never allows
        // two operations at once.
        var categoryResult = await categories.GetAll(cancellationToken);
        if (categoryResult.IsFailure)
            return Result<IReadOnlyList<Category>>.Failure(categoryResult.Error);

        var month = Period.MonthOf(_today());
        var expenseResult = await expenses.GetBetween(month.Start, month.End, cancellationToken);
        if (expenseResult.IsFailure)
            return Result<IReadOnlyList<Category>>.Failure(expenseResult.Error);

        var spentByCategory = expenseResult.Value
            .GroupBy(expense => expense.CategoryId)
            .ToDictionary(group => group.Key, group => group.Sum(expense => expense.Amount.Cents));
        var offered = categoryResult.Value
            .Where(category => !category.IsArchived)
            .OrderByDescending(category => spentByCategory.GetValueOrDefault(category.CategoryId))
            .ThenBy(category => category.Name)
            .Concat(categoryResult.Value.Where(category => category.IsArchived && category.CategoryId == alsoOffer))
            .ToList();
        return Result<IReadOnlyList<Category>>.Success(offered);
    }
}
