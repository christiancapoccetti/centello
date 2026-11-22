using Centello.Domain;

namespace Centello.Application;

/// <summary>How much history a recap needs, and how much a card needs to have something to say, per kind of period.
/// The numbers are first guesses to tune against real data.</summary>
internal sealed record RecapRules(int MinExpenses, int MinDays, int MinMonths, long MinChangeCents, int MinSmall,
    int MinHabitUses, int MaxHabits)
{
    /// <summary>"sotto i 5 €".</summary>
    public const long SmallLimitCents = 500;

    public const int MinRecordDays = 3;
    public const int MinCategories = 2;

    /// <summary>The most categories named on the stack; past them the rest fold into one block.</summary>
    public const int MaxNamedCategories = 6;

    /// <summary>Of the days counted: a sparse period is not reproached.</summary>
    public const double MinConsistency = 0.5;

    public const int MinRankMonths = 3;

    /// <summary>A month ranks 1st to 3rd from either end, or says nothing.</summary>
    public const int RankReach = 3;

    public const int MinWholeMonths = 6;
    public const int MinWeekdayExpenses = 100;

    /// <summary>A uniform week gives 14.3%.</summary>
    public const double MinWeekdayShare = 0.18;

    public const int MinTagOfYearUses = 12;

    /// <summary>Weeks with a use, and at least half of the weeks counted.</summary>
    public const int MinSteadyWeeks = 10;

    public const double MinAutomaticShare = 0.05;
    public const int MinAutomaticPayments = 2;
    public const int MaxPaymentLines = 5;

    public static RecapRules Month { get; } = new(10, 5, 1, 1_000, 8, 3, 1);
    public static RecapRules Year { get; } = new(30, 1, 3, 5_000, 40, 4, 3);

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
