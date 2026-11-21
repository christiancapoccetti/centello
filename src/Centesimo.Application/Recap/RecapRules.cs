using Centesimo.Domain;

namespace Centesimo.Application;

/// <summary>How much history a recap needs, per kind of period. The numbers are first guesses to tune against real
/// data.</summary>
internal sealed record RecapRules(int MinExpenses, int MinDays, int MinMonths)
{
    public static RecapRules Month { get; } = new(10, 5, 1);
    public static RecapRules Year { get; } = new(30, 1, 3);

    public static RecapRules For(PeriodKind kind) => kind switch
    {
        PeriodKind.Month => Month,
        PeriodKind.Year => Year
    };

    /// <summary><paramref name="spanExpenses"/> must already be cut to the span.</summary>
    public bool HasEnough(IReadOnlyCollection<Expense> spanExpenses) =>
        spanExpenses.Count >= MinExpenses
        && spanExpenses.Select(expense => expense.OccurredOn).Distinct().Count() >= MinDays
        && spanExpenses.Select(expense => (expense.OccurredOn.Year, expense.OccurredOn.Month)).Distinct().Count() >= MinMonths;
}
