using Centesimo.Domain;

namespace Centesimo.Application;

/// <summary>Everything the home shows for one period, ready to bind. Strings are Italian.</summary>
public sealed record HomeOverview(
    Period Period,
    HomeNavigation Navigation,
    string Title,                    // "Novembre" | "Anno"
    string Subtitle,                 // "2026"
    string Total,                    // "1.284,60" (the page draws "€")
    BudgetSummary Budget,
    PeriodComparison? Comparison)
{
    public bool HasComparison => Comparison is not null;
}

/// <summary>Navigation targets computed with the clock, so the view model never reads the date.</summary>
public sealed record HomeNavigation(Period Previous, Period? Next, Period Toggle)
{
    public bool HasNext => Next is not null;
}

/// <summary>Three runs so the amount can be bold: Lead + Emphasis + Tail.</summary>
public sealed record BudgetSummary(string Lead, string Emphasis, string Tail);

public enum ComparisonTrend { Lower, Higher, Same }

public sealed record PeriodComparison(string Text, ComparisonTrend Trend, string Description);

public sealed class HomeOverviewService(ICategoryRepository categories, IExpenseRepository expenses,
    Func<DateOnly>? today = null)
{
    private readonly Func<DateOnly> _today = today ?? (() => DateOnly.FromDateTime(DateTime.Today));

    /// <summary>Builds the overview for <paramref name="period"/>, or for the month containing today when null.</summary>
    public async Task<Result<HomeOverview>> Get(Period? period = null, CancellationToken cancellationToken = default)
    {
        var day = _today();
        var selected = period ?? Period.MonthOf(day);
        var window = ComparisonWindow.For(selected, day);
        var categoryResult = await categories.GetAll(cancellationToken);
        if (categoryResult.IsFailure)
            return Result<HomeOverview>.Failure(categoryResult.Error);

        // One range covers the comparison window and the period. The two reads are awaited one after the other:
        // both repositories share a scoped DbContext, which never allows two operations at once, and
        // RepositoryBase would turn that violation into a silent "Unexpected".
        var expenseResult = await expenses.GetBetween(window.PreviousFrom, selected.End, cancellationToken);
        if (expenseResult.IsFailure)
            return Result<HomeOverview>.Failure(expenseResult.Error);

        return Result<HomeOverview>.Success(Build(selected, day, window, categoryResult.Value, expenseResult.Value));
    }

    private static HomeOverview Build(Period period, DateOnly today, ComparisonWindow window,
        IReadOnlyList<Category> categories, IReadOnlyList<Expense> expenses)
    {
        var spent = SpentBetween(expenses, period.Start, period.End);
        var year = period.Start.Year.ToString();
        var (title, subtitle) = period.Kind == PeriodKind.Month
            ? (ItalianFormat.MonthTitle(period.Start.Month), year)
            : ("Anno", year);
        return new HomeOverview(period, Navigate(period, today), title, subtitle, ItalianFormat.Amount(spent),
            Summarize(spent, BudgetOf(period, categories)), Compare(period, window, expenses));
    }

    private static long SpentBetween(IReadOnlyList<Expense> expenses, DateOnly from, DateOnly to) => expenses
        .Where(expense => expense.OccurredOn >= from && expense.OccurredOn <= to)
        .Sum(expense => expense.Amount.Cents);

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

    // The percentage is rounded first, so a change that rounds to zero reads as "in linea".
    private static PeriodComparison? Compare(Period period, ComparisonWindow window, IReadOnlyList<Expense> expenses)
    {
        var previous = SpentBetween(expenses, window.PreviousFrom, window.PreviousTo);
        if (previous == 0)
            return null;

        var current = SpentBetween(expenses, window.CurrentFrom, window.CurrentTo);
        var percent = (int)Math.Round(Math.Abs(current - previous) * 100m / previous, MidpointRounding.AwayFromZero);
        var trend = percent == 0
            ? ComparisonTrend.Same
            : current < previous ? ComparisonTrend.Lower : ComparisonTrend.Higher;
        var change = trend switch
        {
            ComparisonTrend.Lower => $"{ItalianFormat.Percent(percent)} in meno",
            ComparisonTrend.Higher => $"{ItalianFormat.Percent(percent)} in più",
            _ => "in linea"
        };

        var isMonth = period.Kind == PeriodKind.Month;
        var reference = isMonth
            ? ItalianFormat.MonthName(period.Previous().Start.Month)
            : period.Previous().Start.Year.ToString();
        var (preposition, sameAs) = isMonth ? ("di", "con") : ("del", "con il");
        var text = trend == ComparisonTrend.Same ? $"In linea {sameAs} {reference}" : $"{change} {preposition} {reference}";
        var basis = window.IsPartial
            ? $"agli stessi giorni {preposition} {reference}"
            : isMonth ? $"al mese di {reference}" : $"al {reference}";
        return new PeriodComparison(text, trend, $"Rispetto {basis}: {change}.");
    }

    /// <summary>The current period compares to date with the same position in the previous one, clamped to its
    /// last valid day (Mar 31 -> Feb 28; Feb 29 -> Feb 28 of the previous year). A past period compares whole with whole.</summary>
    private sealed record ComparisonWindow(DateOnly CurrentFrom, DateOnly CurrentTo, DateOnly PreviousFrom,
        DateOnly PreviousTo, bool IsPartial)
    {
        public static ComparisonWindow For(Period period, DateOnly today)
        {
            var previous = period.Previous();
            if (!period.Contains(today))
                return new ComparisonWindow(period.Start, period.End, previous.Start, previous.End, false);

            var monthStart = previous.Start.AddMonths(period.Kind == PeriodKind.Year ? today.Month - 1 : 0);
            var lastDay = Math.Min(today.Day, DateTime.DaysInMonth(monthStart.Year, monthStart.Month));
            return new ComparisonWindow(period.Start, today, previous.Start, monthStart.AddDays(lastDay - 1), true);
        }
    }
}
