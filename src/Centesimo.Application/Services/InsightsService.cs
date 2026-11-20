using Centesimo.Domain;

namespace Centesimo.Application;

public interface IInsightsService
{
    /// <summary>Any month or year, or the month that contains today when no period is given. The current period
    /// compares to date with the same position in the previous one; a past period compares whole with whole.</summary>
    Task<Result<InsightsOverview>> Get(Period? period = null, CancellationToken cancellationToken = default);
}

public sealed record InsightCategory(Guid CategoryId, string Name, string Icon, string Color,
    long SpentCents, double Percentage, long? ComparedSpentCents, double? ChangePercentage);

/// <summary>Only what the charts do not show: the pace and the waterfall say the rest with numbers.</summary>
public enum InsightKind { NewHabit, UnusualExpense }

/// <summary>Where an insight leads: to one expense ("Apri la spesa") or to the expenses of a category in a period
/// ("Vedi le spese").</summary>
public abstract record InsightTarget
{
    private InsightTarget()
    {
    }

    public static InsightTarget Expense(Guid expenseId) => new ExpenseTarget(expenseId);
    public static InsightTarget Category(Guid categoryId, Period period) => new CategoryTarget(categoryId, period);

    public sealed record ExpenseTarget(Guid ExpenseId) : InsightTarget;

    public sealed record CategoryTarget(Guid CategoryId, Period Period) : InsightTarget;
}

public sealed record LocalInsight(InsightKind Kind, string Title, string Description, InsightTarget Target);

/// <summary>From and To bound what was spent. IsToDate is true for the period that contains today, which is compared
/// up to today with the same days of the previous one.</summary>
public sealed record InsightsOverview(Period Period, HomeNavigation Navigation, DateOnly From, DateOnly To,
    bool IsToDate, long SpentCents, long? ComparedSpentCents, double? ChangePercentage,
    IReadOnlyList<InsightCategory> Categories, IReadOnlyList<LocalInsight> Insights);

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
        return new InsightsOverview(period, HomeNavigation.For(period, today), window.CurrentFrom, window.CurrentTo,
            spending.IsToDate, spending.Spent, compared,
            compared is null ? null : (double)(spending.Spent - compared.Value) / compared.Value,
            Rows(spending), BuildInsights(spending));
    }

    // A category that only spent in the compared days is a step of the waterfall, never a row.
    private static IReadOnlyList<InsightCategory> Rows(SpendComparison spending) => spending.Categories
        .Where(spend => spend.Spent > 0)
        .Select(spend => new InsightCategory(spend.Category.CategoryId, spend.Category.Name, spend.Category.Icon,
            spend.Category.Color, spend.Spent, (double)spend.Spent / spending.Spent,
            spend.Prior == 0 ? null : spend.Prior, spend.Prior == 0 ? null : (double)spend.Delta / spend.Prior))
        .ToList();

    // Automatic expenses take part in neither: rent is not unusual among groceries, and a subscription is not a new
    // habit. The pace chart shows what they weigh.
    private static IReadOnlyList<LocalInsight> BuildInsights(SpendComparison spending)
    {
        if (spending.Spent == 0)
            return [];

        var current = spending.Current.Where(expense => !expense.IsAutomatic).ToList();
        var previous = spending.Previous.Where(expense => !expense.IsAutomatic).ToList();
        return new[] { UnusualExpense(spending, current), NewHabit(spending, current, previous) }
            .OfType<LocalInsight>()
            .ToList();
    }

    // Every sentence names what it measures against ("di ottobre", "dello stesso periodo del 2025"), so it still
    // reads right on a past period.
    private static LocalInsight? NewHabit(SpendComparison spending, IReadOnlyList<Expense> current,
        IReadOnlyList<Expense> previous)
    {
        var habit = current
            .GroupBy(expense => expense.CategoryId)
            .FirstOrDefault(group => group.Count() >= 3 && !previous.Any(expense => expense.CategoryId == group.Key));
        if (habit is null || CategoryOf(spending, habit.Key) is not { } category)
            return null;

        var period = spending.Period;
        return new LocalInsight(InsightKind.NewHabit, "Una nuova abitudine",
            $"Hai registrato {habit.Count()} spese in {category.Name} {ItalianFormat.InPeriod(period)}; {InsightTexts.InComparedPeriod(period, spending.IsToDate)} nessuna.",
            InsightTarget.Category(category.CategoryId, period));
    }

    private static LocalInsight? UnusualExpense(SpendComparison spending, IReadOnlyList<Expense> current)
    {
        var unusual = current
            .GroupBy(expense => expense.CategoryId)
            .Where(group => group.Count() >= 2
                && group.Max(expense => expense.Amount.Cents) >= group.Average(expense => expense.Amount.Cents) * 1.8)
            .Select(group => group.MaxBy(expense => expense.Amount.Cents))
            .FirstOrDefault();
        if (unusual is null || CategoryOf(spending, unusual.CategoryId) is not { } category)
            return null;

        return new LocalInsight(InsightKind.UnusualExpense, "Una spesa da rivedere",
            $"{Name(unusual)} è molto più alta delle altre spese di {category.Name}.",
            InsightTarget.Expense(unusual.ExpenseId));
    }

    private static Category? CategoryOf(SpendComparison spending, Guid categoryId) => spending.Categories
        .FirstOrDefault(spend => spend.Category.CategoryId == categoryId)
        ?.Category;

    // "Idraulico, 180,00 € il 9 novembre," or, without a note, "180,00 € il 9 novembre".
    private static string Name(Expense expense)
    {
        var what = $"{ItalianFormat.Money(expense.Amount.Cents)} il {ItalianFormat.LongDate(expense.OccurredOn)}";
        return expense.Note.HasValue() ? $"{expense.Note}, {what}," : what;
    }
}
