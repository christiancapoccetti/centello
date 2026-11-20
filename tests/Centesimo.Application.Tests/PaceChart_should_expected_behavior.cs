using Centesimo.Domain;

namespace Centesimo.Application.Tests;

public sealed class PaceChart_should_expected_behavior
{
    private static readonly DateOnly Today = new(2026, 11, 19);

    [Fact]
    public async Task Should_title_a_month_and_a_year()
    {
        var month = await new Ledger().Pace(Period.Month(2026, 11), Today);
        var year = await new Ledger().Pace(Period.Year(2026), Today);

        Assert.Equal("Il passo del mese", month.Title);
        Assert.Equal("Il passo dell'anno", year.Title);
    }

    [Theory]
    [InlineData(2026, 11, 31)]
    [InlineData(2026, 2, 31)]
    [InlineData(2026, 0, 12)]
    public async Task Should_lay_a_month_on_31_slots_and_a_year_on_12(int year, int month, int slots)
    {
        var period = month == 0 ? Period.Year(year) : Period.Month(year, month);

        var pace = await new Ledger().Pace(period, Today);

        Assert.Equal(slots, pace.Slots.Count);
    }

    [Fact]
    public async Task Should_mark_passed_days_today_days_to_come_and_days_outside_the_month()
    {
        var pace = await new Ledger().Pace(Period.Month(2026, 11), Today);

        Assert.Equal(
            [..Run(PaceSlotKind.Spent, 18), PaceSlotKind.Today, ..Run(PaceSlotKind.Ahead, 11), PaceSlotKind.Outside],
            pace.Slots.Select(slot => slot.Kind));
    }

    [Fact]
    public async Task Should_mark_every_day_of_a_past_month_as_spent()
    {
        var october = await new Ledger().Pace(Period.Month(2026, 10), Today);
        var november = await new Ledger().Pace(Period.Month(2026, 11), new DateOnly(2026, 12, 3));

        Assert.Equal(Run(PaceSlotKind.Spent, 31), october.Slots.Select(slot => slot.Kind));
        Assert.Equal([..Run(PaceSlotKind.Spent, 30), PaceSlotKind.Outside], november.Slots.Select(slot => slot.Kind));
    }

    [Fact]
    public async Task Should_mark_months_of_the_year_of_today()
    {
        var pace = await new Ledger().Pace(Period.Year(2026), Today);

        Assert.Equal(
            [..Run(PaceSlotKind.Spent, 10), PaceSlotKind.Today, PaceSlotKind.Ahead],
            pace.Slots.Select(slot => slot.Kind));
    }

    [Fact]
    public async Task Should_mark_every_month_of_a_past_year_as_spent()
    {
        var pace = await new Ledger().Pace(Period.Year(2025), Today);

        Assert.Equal(Run(PaceSlotKind.Spent, 12), pace.Slots.Select(slot => slot.Kind));
    }

    [Fact]
    public async Task Should_accumulate_spending_slot_by_slot_and_carry_nothing_for_days_to_come()
    {
        var ledger = new Ledger(budgetCents: 100_000)
            .Spend(On(11, 1), 10_000)
            .Spend(On(11, 3), 20_000)
            .Spend(On(11, 3), 5_000);

        var pace = await ledger.Pace(Period.Month(2026, 11), On(11, 5));

        AssertClose([0.10, 0.10, 0.35, 0.35, 0.35, ..Run(0.0, 26)], pace.Slots.Select(slot => slot.Total));
    }

    [Fact]
    public async Task Should_split_each_bar_into_automatic_and_variable_spending()
    {
        var ledger = new Ledger(budgetCents: 100_000)
            .Automatic(On(11, 1), 10_000)
            .Spend(On(11, 2), 5_000)
            .Automatic(On(11, 4), 5_000);

        var pace = await ledger.Pace(Period.Month(2026, 11), On(11, 4));

        AssertClose([0.10, 0.10, 0.10, 0.15, ..Run(0.0, 27)], pace.Slots.Select(slot => slot.Automatic));
        AssertClose([0.10, 0.15, 0.15, 0.20, ..Run(0.0, 27)], pace.Slots.Select(slot => slot.Total));
        Assert.All(pace.Slots, slot => Assert.True(slot.Automatic <= slot.Total));
    }

    [Fact]
    public async Task Should_leave_the_automatic_part_at_zero_without_automatic_spending()
    {
        var ledger = new Ledger(budgetCents: 100_000).Spend(On(11, 1), 10_000);

        var pace = await ledger.Pace(Period.Month(2026, 11), On(11, 2));

        AssertClose([..Run(0.0, 31)], pace.Slots.Select(slot => slot.Automatic));
    }

    private static DateOnly On(int month, int day, int year = 2026) => new(year, month, day);

    private static IEnumerable<T> Run<T>(T value, int count) => Enumerable.Repeat(value, count);

    private static void AssertClose(IReadOnlyList<double> expected, IEnumerable<double> actual)
    {
        var values = actual.ToList();
        Assert.Equal(expected.Count, values.Count);
        for (var index = 0; index < expected.Count; index++)
            Assert.Equal(expected[index], values[index], 9);
    }

    // One category, optionally with a budget, and one recurring payment for the automatic spending.
    private sealed class Ledger
    {
        private readonly FakeCategoryRepository _categories = new();
        private readonly FakeExpenseRepository _expenses = new();
        private readonly Category _category;
        private readonly Guid _payment = Guid.NewGuid();

        public Ledger(long? budgetCents = null)
        {
            _category = new Category(Guid.NewGuid(), "Spesa", "cart", "coral",
                budgetCents is { } cents ? new Money(cents) : null);
            _categories.Items.Add(_category);
        }

        public Ledger Spend(DateOnly on, long cents) => Add(on, cents, null);

        public Ledger Automatic(DateOnly on, long cents) => Add(on, cents, _payment);

        public async Task<PaceChart> Pace(Period period, DateOnly today)
        {
            var service = new InsightsService(_categories, _expenses, () => today);
            return (await service.Get(period)).Value.Pace;
        }

        private Ledger Add(DateOnly on, long cents, Guid? recurringPaymentId)
        {
            _expenses.Items.Add(new Expense(Guid.NewGuid(), _category.CategoryId, new Money(cents), on,
                recurringPaymentId: recurringPaymentId));
            return this;
        }
    }
}
