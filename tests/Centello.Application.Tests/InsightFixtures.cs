using Centello.Domain;

namespace Centello.Application.Tests;

/// <summary>November 2026 up to the 19th, the month of the Insight mockup (mockups/2026-11-20-insight-charts.html),
/// as data for the chart tests: seven categories with budgets that add up to 1.600 €, and three automatic payments on
/// the 1st (rent, 380 €, Casa), the 5th (streaming, 12,99 €, Svago) and the 10th (phone, 20 €, Altro) of both months.
/// November holds 1.284,60 € to the 19th, 1–19 October 1.366,60 € and the whole of October closes at 1.590 €.
/// Each instance is its own world: change its repositories freely. The amounts of a day are split among the
/// categories so that every category reaches the total of the mockup (Spesa 412,30 against 398,50, Casa 380 against
/// 380, Trasporti 146,20 against 205,60, Ristoranti 128,40 against 96,20, Svago 96,10 against 142,30, Salute 61,20
/// against 88, Altro 60,40 against 56). The days after the 19th of October go to Spesa.</summary>
internal sealed class InsightFixtures
{
    public const long SpentCents = 128_460;
    public const long ComparedCents = 136_660;
    public const long PreviousClosedCents = 159_000;
    public const long AutomaticCents = 41_299;
    public const long BudgetCents = 160_000;

    public static readonly DateOnly Today = new(2026, 11, 19);

    /// <summary>What was spent on each day, automatic payments included, in cents. Index 0 is the 1st.</summary>
    public static readonly IReadOnlyList<long> NovemberDays =
    [
        40_320, 1_840, 4_210, 1_260, 9_130, 2_890, 12_840, 980, 2_250, 7_800,
        1_510, 11_030, 2_560, 1_390, 14_640, 3_620, 1_980, 5_510, 2_700
    ];

    public static readonly IReadOnlyList<long> OctoberDays =
    [
        39_500, 2_200, 3_500, 4_800, 1_500, 9_000, 3_000, 1_200, 6_400, 4_000,
        1_800, 11_000, 2_700, 6_000, 3_300, 14_000, 2_000, 4_500, 16_260, 1_200,
        2_800, 3_500, 900, 2_200, 1_800, 3_000, 1_400, 2_000, 1_100, 1_500, 940
    ];

    /// <summary>The automatic payments by day of the month, in cents, the same in both months.</summary>
    public static readonly IReadOnlyDictionary<int, long> AutomaticDays = new Dictionary<int, long>
    {
        [1] = 38_000,
        [5] = 1_299,
        [10] = 2_000
    };

    private readonly Guid _rent = Guid.NewGuid();
    private readonly Guid _streaming = Guid.NewGuid();
    private readonly Guid _phone = Guid.NewGuid();

    private InsightFixtures()
    {
        Categories.Items.AddRange([Spesa, Casa, Trasporti, Ristoranti, Svago, Salute, Altro]);
        AddMonth(10, OctoberDays,
        [
            (Spesa, 39_850), (Casa, 0), (Trasporti, 20_560), (Ristoranti, 9_620), (Svago, 12_931), (Salute, 8_800), (Altro, 3_600)
        ]);
        AddMonth(11, NovemberDays,
        [
            (Spesa, 41_230), (Casa, 0), (Trasporti, 14_620), (Ristoranti, 12_840), (Svago, 8_311), (Salute, 6_120), (Altro, 4_040)
        ]);
    }

    public Category Spesa { get; } = NewCategory("Spesa", "cart", "coral", 45_000);
    public Category Casa { get; } = NewCategory("Casa", "home", "peri", 38_000);
    public Category Trasporti { get; } = NewCategory("Trasporti", "bus", "butter", 20_000);
    public Category Ristoranti { get; } = NewCategory("Ristoranti", "food", "mint", 15_000);
    public Category Svago { get; } = NewCategory("Svago", "leisure", "lilac", 15_000);
    public Category Salute { get; } = NewCategory("Salute", "health", "sky", 10_000);
    public Category Altro { get; } = NewCategory("Altro", "more", "grey", 17_000);

    public FakeCategoryRepository Categories { get; } = new();
    public FakeExpenseRepository Expenses { get; } = new();

    public static InsightFixtures November() => new();

    public InsightsService Service(DateOnly? today = null) =>
        new(Categories, Expenses, () => today ?? Today);

    public async Task<InsightsOverview> Overview(Period? period = null, DateOnly? today = null) =>
        (await Service(today).Get(period ?? Period.Month(2026, 11))).Value;

    private static Category NewCategory(string name, string icon, string color, long budgetCents) =>
        new(Guid.NewGuid(), name, icon, color, new Money(budgetCents));

    private void AddMonth(int month, IReadOnlyList<long> days, IReadOnlyList<(Category Category, long Cents)> variable)
    {
        var remaining = variable.Select(item => item.Cents).ToArray();
        var next = 0;
        for (var index = 0; index < days.Count; index++)
        {
            var on = new DateOnly(2026, month, index + 1);
            var left = days[index];
            if (AutomaticDays.TryGetValue(index + 1, out var automatic))
            {
                AddAutomatic(index + 1, automatic, on);
                left -= automatic;
            }

            while (left > 0)
            {
                if (next == variable.Count)
                {
                    Add(Spesa, left, on);
                    break;
                }

                var take = Math.Min(left, remaining[next]);
                if (take > 0)
                    Add(variable[next].Category, take, on);

                remaining[next] -= take;
                left -= take;
                if (remaining[next] == 0)
                    next++;
            }
        }
    }

    private void AddAutomatic(int day, long cents, DateOnly on)
    {
        var (category, payment) = day switch
        {
            1 => (Casa, _rent),
            5 => (Svago, _streaming),
            _ => (Altro, _phone)
        };
        Add(category, cents, on, payment);
    }

    private void Add(Category category, long cents, DateOnly on, Guid? recurringPaymentId = null) =>
        Expenses.Items.Add(new Expense(Guid.NewGuid(), category.CategoryId, new Money(cents), on,
            recurringPaymentId: recurringPaymentId));
}
