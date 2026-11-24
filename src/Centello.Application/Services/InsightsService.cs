using Centello.Domain;

namespace Centello.Application;

public interface IInsightsService
{
    /// <summary>Any month or year, or the month that contains today when no period is given. The current period
    /// compares to date with the same position in the previous one; a past period compares whole with whole.</summary>
    Task<Result<InsightsOverview>> Get(Period? period = null, CancellationToken cancellationToken = default);
}

public sealed record InsightCategory(Guid? CategoryId, string Name, string Icon, string Color,
    long SpentCents, long? ComparedSpentCents, double? Share, double? ComparedShare)
{
    public long? DeltaCents => SpentCents - ComparedSpentCents;
}

public sealed record InsightComposition(BudgetSummary Statement, string Context);

public sealed record InsightsOverview(Period Period, HomeNavigation Navigation, DateOnly From, DateOnly To,
    DateOnly ComparedFrom, DateOnly ComparedTo, bool IsToDate, long SpentCents, long? ComparedSpentCents,
    BudgetSummary Summary, PaceChart Pace, IReadOnlyList<InsightCategory> Categories, InsightComposition Composition,
    InsightRecap? Recap);

public sealed class InsightsService(ICategoryRepository categories, IExpenseRepository expenses,
    Func<DateOnly>? today = null) : IInsightsService
{
    private readonly Func<DateOnly> _today = today ?? (() => DateOnly.FromDateTime(DateTime.Today));

    public async Task<Result<InsightsOverview>> Get(Period? period = null, CancellationToken cancellationToken = default)
    {
        var day = _today();
        var selected = period ?? Period.MonthOf(day);
        var window = ComparisonWindow.For(selected, day);
        var categoryResult = await categories.GetAll(cancellationToken);
        if (categoryResult.IsFailure)
            return Result<InsightsOverview>.Failure(categoryResult.Error);

        // One range covers both windows. The reads are awaited one after the other: the repositories share a scoped
        // DbContext, which never allows two operations at once.
        var expenseResult = await expenses.GetBetween(window.PreviousFrom, window.CurrentTo, cancellationToken);
        if (expenseResult.IsFailure)
            return Result<InsightsOverview>.Failure(expenseResult.Error);

        return Result<InsightsOverview>.Success(Build(selected, day, window, categoryResult.Value, expenseResult.Value));
    }

    private static InsightsOverview Build(Period period, DateOnly today, ComparisonWindow window,
        IReadOnlyList<Category> categories, IReadOnlyList<Expense> expenses)
    {
        var spending = SpendComparison.Of(period, window, categories, expenses);
        long? compared = spending.IsComparable ? spending.Compared : null;
        var rows = InsightCategories.For(spending.Spent, compared, spending.Categories);
        return new InsightsOverview(period, HomeNavigation.For(period, today), window.CurrentFrom, window.CurrentTo,
            window.PreviousFrom, window.PreviousTo, spending.IsToDate, spending.Spent, compared,
            InsightTexts.Summary(period, spending.IsToDate, spending.Spent, compared),
            PaceChart.For(spending, PeriodBudget.OfActive(period, categories)), rows,
            InsightCategories.Composition(period, spending.IsToDate, rows), InsightRecap.For(period, today, spending.Current));
    }
}
