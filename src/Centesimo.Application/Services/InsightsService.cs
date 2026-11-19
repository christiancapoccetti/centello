using Centesimo.Domain;

namespace Centesimo.Application;

public enum InsightPeriod
{
    Month,
    Year
}

public interface IInsightsService
{
    /// <summary>The month or the year that contains today.</summary>
    Task<Result<InsightsOverview>> Get(InsightPeriod period, CancellationToken cancellationToken = default);

    /// <summary>Any month or year. The current period compares to date with the same position in the previous one; a past
    /// period compares whole with whole.</summary>
    Task<Result<InsightsOverview>> Get(Period period, CancellationToken cancellationToken = default);
}

public sealed record InsightCategory(Guid CategoryId, string Name, string Icon, string Color,
    long SpentCents, double Percentage, long? ComparedSpentCents, double? ChangePercentage);

public enum InsightKind { Increased, Decreased, TopCategory, NewHabit, UnusualExpense, Stable }

/// <summary>Where an insight leads: nowhere, to one expense ("Apri la spesa") or to the expenses of a category in a
/// period ("Vedi le spese").</summary>
public abstract record InsightTarget
{
    private InsightTarget()
    {
    }

    public static InsightTarget None { get; } = new NoTarget();
    public static InsightTarget Expense(Guid expenseId) => new ExpenseTarget(expenseId);
    public static InsightTarget Category(Guid categoryId, Period period) => new CategoryTarget(categoryId, period);

    public sealed record NoTarget : InsightTarget;

    public sealed record ExpenseTarget(Guid ExpenseId) : InsightTarget;

    public sealed record CategoryTarget(Guid CategoryId, Period Period) : InsightTarget;
}

public sealed record LocalInsight(InsightKind Kind, string Title, string Description, InsightTarget Target)
{
    public Guid? CategoryId => (Target as InsightTarget.CategoryTarget)?.CategoryId;
    public Guid? ExpenseId => (Target as InsightTarget.ExpenseTarget)?.ExpenseId;
}

public sealed record InsightsOverview(Period Period, DateOnly From, DateOnly To, long SpentCents,
    long? ComparedSpentCents, double? ChangePercentage, IReadOnlyList<InsightCategory> Categories,
    IReadOnlyList<LocalInsight> Insights);

public sealed class InsightsService(ICategoryRepository categories, IExpenseRepository expenses,
    Func<DateOnly>? today = null) : IInsightsService
{
    private readonly Func<DateOnly> _today = today ?? (() => DateOnly.FromDateTime(DateTime.Today));

    public Task<Result<InsightsOverview>> Get(InsightPeriod period, CancellationToken cancellationToken = default)
    {
        var day = _today();
        return Get(period == InsightPeriod.Month ? Period.MonthOf(day) : Period.Year(day.Year), cancellationToken);
    }

    public async Task<Result<InsightsOverview>> Get(Period period, CancellationToken cancellationToken = default)
    {
        var window = ComparisonWindow.For(period, _today());
        var categoryResult = await categories.GetAll(cancellationToken);
        if (categoryResult.IsFailure)
            return Result<InsightsOverview>.Failure(categoryResult.Error);

        // One range covers both windows. The reads are awaited one after the other: the repositories share a scoped
        // DbContext, which never allows two operations at once.
        var expenseResult = await expenses.GetBetween(window.PreviousFrom, window.CurrentTo, cancellationToken);
        if (expenseResult.IsFailure)
            return Result<InsightsOverview>.Failure(expenseResult.Error);

        var activeCategories = categoryResult.Value.Where(x => !x.IsArchived).ToDictionary(x => x.CategoryId);
        var currentExpenses = Between(expenseResult.Value, window.CurrentFrom, window.CurrentTo);
        var previousExpenses = Between(expenseResult.Value, window.PreviousFrom, window.PreviousTo);
        var total = currentExpenses.Sum(x => x.Amount.Cents);
        var previousTotal = previousExpenses.Sum(x => x.Amount.Cents);
        long? comparable = previousTotal > 0 ? previousTotal : null;
        var comparisonCategories = currentExpenses.Concat(previousExpenses)
            .GroupBy(x => x.CategoryId)
            .Where(x => activeCategories.ContainsKey(x.Key))
            .Select(group =>
            {
                var category = activeCategories[group.Key];
                var spent = currentExpenses.Where(x => x.CategoryId == group.Key).Sum(x => x.Amount.Cents);
                var prior = previousExpenses.Where(x => x.CategoryId == group.Key).Sum(x => x.Amount.Cents);
                return new InsightCategory(category.CategoryId, category.Name, category.Icon, category.Color, spent,
                    total == 0 ? 0 : (double)spent / total, prior == 0 ? null : prior,
                    prior == 0 ? null : ((double)(spent - prior) / prior));
            })
            .OrderByDescending(x => x.SpentCents).ThenBy(x => x.Name).ToList();
        var categoryItems = comparisonCategories.Where(x => x.SpentCents > 0).ToList();
        var localInsights = BuildInsights(period, comparisonCategories, currentExpenses, previousExpenses, total, comparable);
        return Result<InsightsOverview>.Success(new InsightsOverview(period, window.CurrentFrom, window.CurrentTo, total,
            comparable, comparable is null ? null : (double)(total - comparable.Value) / comparable.Value,
            categoryItems, localInsights));
    }

    private static List<Expense> Between(IReadOnlyList<Expense> expenses, DateOnly from, DateOnly to) => expenses
        .Where(expense => expense.OccurredOn >= from && expense.OccurredOn <= to)
        .ToList();

    private static IReadOnlyList<LocalInsight> BuildInsights(Period period, IReadOnlyList<InsightCategory> categories,
        IReadOnlyList<Expense> currentExpenses, IReadOnlyList<Expense> previousExpenses, long total, long? previousTotal)
    {
        if (total == 0)
            return [];

        var insights = new List<LocalInsight>();
        var increased = categories.FirstOrDefault(x => x.ChangePercentage >= .1);
        if (increased is not null)
            insights.Add(new LocalInsight(InsightKind.Increased, "Spese in aumento", $"Hai speso di più per {increased.Name} rispetto al periodo precedente.", InsightTarget.Category(increased.CategoryId, period)));
        var decreased = categories.FirstOrDefault(x => x.ChangePercentage <= -.1);
        if (decreased is not null)
            insights.Add(new LocalInsight(InsightKind.Decreased, "Spese in diminuzione", $"Hai speso di meno per {decreased.Name} rispetto al periodo precedente.", InsightTarget.Category(decreased.CategoryId, period)));
        if (categories.Count > 0)
        {
            var top = categories[0];
            var share = ItalianFormat.Percent((int)Math.Round(top.Percentage * 100, MidpointRounding.AwayFromZero));
            insights.Add(new LocalInsight(InsightKind.TopCategory, "La categoria principale", $"{top.Name} rappresenta il {share} delle spese del periodo.", InsightTarget.Category(top.CategoryId, period)));
        }
        var newHabit = currentExpenses
            .GroupBy(x => x.CategoryId)
            .FirstOrDefault(group => group.Count() >= 3 && !previousExpenses.Any(x => x.CategoryId == group.Key));
        if (newHabit is not null && categories.FirstOrDefault(x => x.CategoryId == newHabit.Key) is { } newCategory)
            insights.Add(new LocalInsight(InsightKind.NewHabit, "Una nuova abitudine", $"Hai registrato {newHabit.Count()} spese in {newCategory.Name} in questo periodo.", InsightTarget.Category(newCategory.CategoryId, period)));
        var unusual = currentExpenses
            .GroupBy(x => x.CategoryId)
            .Select(group => new { Expenses = group.ToList(), Maximum = group.MaxBy(x => x.Amount.Cents) })
            .FirstOrDefault(group => group.Expenses.Count >= 2 && group.Maximum!.Amount.Cents >= group.Expenses.Average(x => x.Amount.Cents) * 1.8);
        if (unusual?.Maximum is not null && categories.FirstOrDefault(x => x.CategoryId == unusual.Maximum.CategoryId) is { } unusualCategory)
            insights.Add(new LocalInsight(InsightKind.UnusualExpense, "Una spesa da rivedere", $"{ItalianFormat.Money(unusual.Maximum.Amount.Cents)} per {unusualCategory.Name} è sensibilmente più alta delle altre spese della categoria.", InsightTarget.Expense(unusual.Maximum.ExpenseId)));
        if (previousTotal > 0 && Math.Abs((double)(total - previousTotal.Value) / previousTotal.Value) < .1)
            insights.Add(new LocalInsight(InsightKind.Stable, "Spese stabili", "Il totale è simile al periodo precedente.", InsightTarget.None));
        return insights
            .OrderBy(x => x.Kind is InsightKind.UnusualExpense or InsightKind.NewHabit ? 0 : 1)
            .ToList();
    }
}
