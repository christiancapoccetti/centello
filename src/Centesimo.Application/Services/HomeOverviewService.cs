using Centesimo.Domain;

namespace Centesimo.Application;

/// <summary>Everything the home shows for one period, ready to bind. Strings are Italian.</summary>
public sealed record HomeOverview(
    Period Period,
    HomeNavigation Navigation,
    string Title,                    // "Novembre" | "Anno"
    string Subtitle,                 // "2026"
    string Total,                    // "1.284,60" (the page draws "€")
    BudgetSummary Budget);

/// <summary>Navigation targets computed with the clock, so the view model never reads the date.</summary>
public sealed record HomeNavigation(Period Previous, Period? Next, Period Toggle)
{
    public bool HasNext => Next is not null;
}

/// <summary>Three runs so the amount can be bold: Lead + Emphasis + Tail.</summary>
public sealed record BudgetSummary(string Lead, string Emphasis, string Tail);

public sealed class HomeOverviewService(ICategoryRepository categories, IExpenseRepository expenses,
    Func<DateOnly>? today = null)
{
    private readonly Func<DateOnly> _today = today ?? (() => DateOnly.FromDateTime(DateTime.Today));

    /// <summary>Builds the overview for <paramref name="period"/>, or for the month containing today when null.</summary>
    public async Task<Result<HomeOverview>> Get(Period? period = null, CancellationToken cancellationToken = default)
    {
        var day = _today();
        var selected = period ?? Period.MonthOf(day);
        var categoryResult = await categories.GetAll(cancellationToken);
        if (categoryResult.IsFailure)
            return Result<HomeOverview>.Failure(categoryResult.Error);

        // Awaited one after the other: both repositories share a scoped DbContext, which never allows two
        // operations at once, and RepositoryBase would turn that violation into a silent "Unexpected".
        var expenseResult = await expenses.GetBetween(selected.Start, selected.End, cancellationToken);
        if (expenseResult.IsFailure)
            return Result<HomeOverview>.Failure(expenseResult.Error);

        return Result<HomeOverview>.Success(Build(selected, day, categoryResult.Value, expenseResult.Value));
    }

    private static HomeOverview Build(Period period, DateOnly today, IReadOnlyList<Category> categories,
        IReadOnlyList<Expense> periodExpenses)
    {
        var spent = periodExpenses.Sum(expense => expense.Amount.Cents);
        var year = period.Start.Year.ToString();
        var (title, subtitle) = period.Kind == PeriodKind.Month
            ? (ItalianFormat.MonthTitle(period.Start.Month), year)
            : ("Anno", year);
        return new HomeOverview(period, Navigate(period, today), title, subtitle, ItalianFormat.Amount(spent),
            Summarize(spent, BudgetOf(period, categories)));
    }

    // A year budget is twelve monthly budgets. A total of zero is "no budget": nothing to measure against.
    private static long? BudgetOf(Period period, IReadOnlyList<Category> categories)
    {
        var monthly = categories
            .Where(category => !category.IsArchived)
            .Sum(category => category.MonthlyBudget?.Cents ?? 0);
        var total = period.Kind == PeriodKind.Year ? monthly * 12 : monthly;
        return total > 0 ? total : null;
    }

    private static HomeNavigation Navigate(Period period, DateOnly today) =>
        new(period.Previous(), period.End >= today ? null : period.Next(), Toggle(period, today));

    private static Period Toggle(Period period, DateOnly today)
    {
        if (period.Kind == PeriodKind.Month)
            return Period.Year(period.Start.Year);

        return period.Contains(today) ? Period.MonthOf(today) : Period.Month(period.Start.Year, 12);
    }

    private static BudgetSummary Summarize(long spent, long? budget)
    {
        if (budget is not { } total)
            return new BudgetSummary("Nessun budget impostato", "", "");

        if (spent > total)
            return new BudgetSummary("Hai superato il budget di ", ItalianFormat.Money(spent - total),
                $" su {ItalianFormat.Budget(total)}");

        return new BudgetSummary("Ne restano ", ItalianFormat.Money(total - spent),
            $" su {ItalianFormat.Budget(total)} di budget");
    }
}
