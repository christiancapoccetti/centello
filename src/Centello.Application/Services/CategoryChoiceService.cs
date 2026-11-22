using Centello.Domain;

namespace Centello.Application;

/// <summary>A tile of the Scorciatoie widget. The keys are resolved: an unknown icon or color reads as the fallback.</summary>
public sealed record CategoryShortcut(Guid CategoryId, string Name, string IconKey, string ColorKey);

/// <summary>The categories an editor offers when it asks where an expense goes.</summary>
public sealed class CategoryChoiceService(ICategoryRepository categories, IExpenseRepository expenses,
    Func<DateOnly>? today = null)
{
    private const int EditorDays = 90;
    private const int ShortcutDays = 30;
    private const int ShortcutCount = 4;
    private readonly Func<DateOnly> _today = today ?? (() => DateOnly.FromDateTime(DateTime.Today));

    /// <summary>Active categories, the ones with more expenses recorded in the last 90 days first and equal counts
    /// by name, so the category used most often is the first to reach. <paramref name="alsoOffer"/> adds one
    /// category at the end even when it is archived: an editor that only shows a stored choice needs it.</summary>
    public async Task<Result<IReadOnlyList<Category>>> Get(Guid? alsoOffer = null,
        CancellationToken cancellationToken = default)
    {
        var ranked = await Ranked(EditorDays, cancellationToken);
        if (ranked.IsFailure)
            return Result<IReadOnlyList<Category>>.Failure(ranked.Error);

        var archivedChoice = ranked.Value.Archived.Where(category => category.CategoryId == alsoOffer);
        return Result<IReadOnlyList<Category>>.Success(ranked.Value.Active.Concat(archivedChoice).ToList());
    }

    /// <summary>The four active categories with the most expenses in the last 30 days, equal counts by name.
    /// Categories without recent expenses fill the free places by name, so the list is shorter than four only with
    /// fewer than four active categories, and empty before the first one exists.</summary>
    public async Task<Result<IReadOnlyList<CategoryShortcut>>> Shortcuts(CancellationToken cancellationToken = default)
    {
        var ranked = await Ranked(ShortcutDays, cancellationToken);
        if (ranked.IsFailure)
            return Result<IReadOnlyList<CategoryShortcut>>.Failure(ranked.Error);

        var shortcuts = ranked.Value.Active
            .Take(ShortcutCount)
            .Select(category => new CategoryShortcut(category.CategoryId, category.Name,
                CategoryIcons.Resolve(category.Icon).Key, CategoryPalette.Resolve(category.Color).Key))
            .ToList();
        return Result<IReadOnlyList<CategoryShortcut>>.Success(shortcuts);
    }

    // The reads are awaited one after the other: the repositories share a scoped DbContext, which never allows two
    // operations at once.
    private async Task<Result<Ranking>> Ranked(int days, CancellationToken cancellationToken)
    {
        var categoryResult = await categories.GetAll(cancellationToken);
        if (categoryResult.IsFailure)
            return Result<Ranking>.Failure(categoryResult.Error);

        var day = _today();
        var expenseResult = await expenses.GetBetween(day.AddDays(1 - days), day, cancellationToken);
        if (expenseResult.IsFailure)
            return Result<Ranking>.Failure(expenseResult.Error);

        var expensesByCategory = expenseResult.Value
            .GroupBy(expense => expense.CategoryId)
            .ToDictionary(group => group.Key, group => group.Count());
        var active = categoryResult.Value
            .Where(category => !category.IsArchived)
            .OrderByDescending(category => expensesByCategory.GetValueOrDefault(category.CategoryId))
            .ThenBy(category => category.Name)
            .ToList();
        return Result<Ranking>.Success(new Ranking(active, categoryResult.Value.Where(category => category.IsArchived).ToList()));
    }

    private sealed record Ranking(IReadOnlyList<Category> Active, IReadOnlyList<Category> Archived);
}
