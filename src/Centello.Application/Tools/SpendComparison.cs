using Centello.Domain;

namespace Centello.Application;

/// <summary>One active category's spending in the two compared windows, in raw cents. Zero means nothing was spent;
/// there is no null.</summary>
internal sealed record CategorySpend(Category Category, long Spent, long Prior)
{
    public long Delta => Spent - Prior;
}

/// <summary>The loaded expenses cut by the comparison window. Every number Insight shows derives from this one value,
/// so the to-date rule is applied once. Totals count every expense, archived and unknown categories included.
/// <see cref="Categories"/> holds active categories only: the gap between the two is what the waterfall folds.</summary>
internal sealed record SpendComparison(
    Period Period,
    ComparisonWindow Window,
    IReadOnlyList<Expense> Current,
    IReadOnlyList<Expense> Previous,
    IReadOnlyList<Expense> PreviousWhole,
    IReadOnlyList<CategorySpend> Categories)
{
    public bool IsToDate => Window.IsPartial;
    public long Spent => Sum(Current);
    public long Compared => Sum(Previous);
    public long PreviousClosed => Sum(PreviousWhole);

    /// <summary>Nothing spent in the compared days means nothing to compare with, even when the previous period has
    /// spending later on.</summary>
    public bool IsComparable => Compared > 0;

    /// <summary><paramref name="expenses"/> covers Window.PreviousFrom..Window.CurrentTo. PreviousFrom is always the
    /// previous period's first day and its last day is before CurrentFrom, so the whole previous period is inside.
    /// <see cref="Categories"/> comes largest spending first, then by name.</summary>
    public static SpendComparison Of(Period period, ComparisonWindow window, IReadOnlyList<Category> categories,
        IReadOnlyList<Expense> expenses)
    {
        var previousPeriod = period.Previous();
        var current = Between(expenses, window.CurrentFrom, window.CurrentTo);
        var compared = Between(expenses, window.PreviousFrom, window.PreviousTo);
        var whole = Between(expenses, previousPeriod.Start, previousPeriod.End);
        var active = categories
            .Where(category => !category.IsArchived)
            .ToDictionary(category => category.CategoryId);
        var spends = current
            .Concat(compared)
            .Select(expense => expense.CategoryId)
            .Distinct()
            .Where(active.ContainsKey)
            .Select(categoryId => new CategorySpend(active[categoryId], SpentIn(current, categoryId), SpentIn(compared, categoryId)))
            .OrderByDescending(spend => spend.Spent)
            .ThenBy(spend => spend.Category.Name)
            .ToList();
        return new SpendComparison(period, window, current, compared, whole, spends);
    }

    private static List<Expense> Between(IReadOnlyList<Expense> expenses, DateOnly from, DateOnly to) => expenses
        .Where(expense => expense.OccurredOn >= from && expense.OccurredOn <= to)
        .ToList();

    private static long SpentIn(IEnumerable<Expense> expenses, Guid categoryId) => expenses
        .Where(expense => expense.CategoryId == categoryId)
        .Sum(expense => expense.Amount.Cents);

    private static long Sum(IEnumerable<Expense> expenses) => expenses.Sum(expense => expense.Amount.Cents);
}
