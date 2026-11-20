using Centesimo.Domain;

namespace Centesimo.Application.Tests;

public sealed class PaceChart_should_expected_behavior
{
    private const char Nbsp = (char)0xA0;

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

    [Theory]
    [InlineData(10, 31)]
    [InlineData(11, 30)]
    [InlineData(12, 31)]
    [InlineData(2, 28)]
    public async Task Should_draw_the_previous_month_over_all_its_days(int month, int levels)
    {
        var period = Period.Month(2026, month).Next();
        var ledger = new Ledger().Spend(On(month, 1), 10_000);

        var pace = await ledger.Pace(period, period.Start.AddDays(9));

        Assert.Equal(levels, pace.Previous!.Levels.Count);
    }

    [Fact]
    public async Task Should_draw_the_previous_year_over_its_12_months()
    {
        var ledger = new Ledger().Spend(On(2, 10, 2025), 10_000);

        var pace = await ledger.Pace(Period.Year(2026), Today);

        Assert.Equal(12, pace.Previous!.Levels.Count);
    }

    [Fact]
    public async Task Should_draw_the_previous_period_as_it_really_was()
    {
        var ledger = new Ledger(budgetCents: 100_000)
            .Spend(On(10, 1), 10_000)
            .Spend(On(10, 15), 20_000)
            .Spend(On(10, 31), 5_000);

        var pace = await ledger.Pace(Period.Month(2026, 11), Today);

        AssertClose([0.10, ..Run(0.10, 13), 0.30, ..Run(0.30, 15), 0.35], pace.Previous!.Levels);
    }

    [Fact]
    public async Task Should_keep_the_whole_of_last_years_month_in_the_line()
    {
        var ledger = new Ledger()
            .Spend(On(2, 10, 2025), 10_000)
            .Spend(On(11, 25, 2025), 20_000)
            .Spend(On(12, 5, 2025), 5_000)
            .Spend(On(1, 7, 2026), 7_000);

        var pace = await ledger.Pace(Period.Year(2026), Today);

        AssertClose([0, ..Run(10_000 / 35_000.0, 9), 30_000 / 35_000.0, 1], pace.Previous!.Levels);
    }

    [Fact]
    public async Task Should_leave_out_the_previous_line_when_the_previous_period_has_no_spending()
    {
        var ledger = new Ledger().Spend(On(11, 3), 10_000).Spend(On(9, 20), 5_000);

        var pace = await ledger.Pace(Period.Month(2026, 11), Today);

        Assert.Null(pace.Previous);
    }

    [Fact]
    public async Task Should_label_how_the_previous_period_closed_in_whole_euros()
    {
        var month = await new Ledger().Spend(On(10, 31), 159_040).Pace(Period.Month(2026, 11), Today);
        var year = await new Ledger().Spend(On(12, 31, 2025), 1_840_000).Pace(Period.Year(2026), Today);

        Assert.Equal($"ottobre ha chiuso a 1.590{Nbsp}€", month.Previous!.CloseLabel);
        Assert.Equal($"il 2025 ha chiuso a 18.400{Nbsp}€", year.Previous!.CloseLabel);
    }

    [Fact]
    public async Task Should_scale_to_the_previous_closing_total_when_it_is_the_largest()
    {
        var ledger = new Ledger().Spend(On(10, 31), 100_000).Spend(On(11, 2), 40_000);

        var pace = await ledger.Pace(Period.Month(2026, 11), Today);

        Assert.Equal(1.0, pace.Previous!.Levels[^1], 9);
        Assert.Equal(0.4, pace.Slots[18].Total, 9);
    }

    [Fact]
    public async Task Should_scale_to_the_spent_total_when_it_is_the_largest()
    {
        var ledger = new Ledger().Spend(On(10, 31), 20_000).Spend(On(11, 2), 50_000);

        var pace = await ledger.Pace(Period.Month(2026, 11), Today);

        Assert.Equal(1.0, pace.Slots[18].Total, 9);
        Assert.Equal(0.4, pace.Previous!.Levels[^1], 9);
    }

    [Fact]
    public async Task Should_count_archived_category_spending_in_the_bars_and_the_line()
    {
        var ledger = new Ledger()
            .SpendArchived(On(10, 3), 12_000)
            .Spend(On(10, 20), 3_000)
            .SpendArchived(On(11, 2), 6_000)
            .Spend(On(11, 2), 4_000);

        var pace = await ledger.Pace(Period.Month(2026, 11), Today);

        Assert.Equal(10_000 / 15_000.0, pace.Slots[1].Total, 9);
        Assert.Equal(12_000 / 15_000.0, pace.Previous!.Levels[2], 9);
    }

    [Fact]
    public async Task Should_start_the_gap_on_the_previous_line_at_today()
    {
        var ledger = new Ledger(budgetCents: 100_000)
            .Spend(On(10, 5), 20_000)
            .Spend(On(10, 25), 30_000)
            .Spend(On(11, 3), 15_000);

        var pace = await ledger.Pace(Period.Month(2026, 11), Today);

        Assert.Equal(18, pace.Gap!.Slot);
        Assert.Equal(0.20, pace.Gap.From, 9);
        Assert.Equal(0.20, pace.Previous!.Levels[18], 9);
        Assert.Equal(0.15, pace.Gap.To, 9);
        Assert.Equal(pace.Slots[18].Total, pace.Gap.To, 9);
        Assert.Equal($"50{Nbsp}€ in meno", pace.Gap.Amount);
        Assert.Equal("di ottobre a oggi", pace.Gap.Caption);
    }

    [Fact]
    public async Task Should_measure_the_year_gap_against_the_same_days_of_the_previous_year()
    {
        var ledger = new Ledger()
            .Spend(On(1, 10, 2025), 10_000)
            .Spend(On(11, 5, 2025), 20_000)
            .Spend(On(11, 25, 2025), 15_000)
            .Spend(On(12, 3, 2025), 5_000)
            .Spend(On(1, 5), 12_000)
            .Spend(On(11, 3), 8_000);

        var pace = await ledger.Pace(Period.Year(2026), Today);

        Assert.Equal(10, pace.Gap!.Slot);
        Assert.Equal(30_000 / 50_000.0, pace.Gap.From, 9);
        Assert.Equal(20_000 / 50_000.0, pace.Gap.To, 9);
        Assert.Equal($"100{Nbsp}€ in meno", pace.Gap.Amount);
        Assert.Equal("del 2025 a oggi", pace.Gap.Caption);
        Assert.InRange(pace.Gap.From, pace.Previous!.Levels[9], pace.Previous.Levels[10]);
        Assert.Equal(45_000 / 50_000.0, pace.Previous.Levels[10], 9);
    }

    [Fact]
    public async Task Should_compare_a_past_month_at_its_last_day_with_the_whole_previous_month()
    {
        var ledger = new Ledger()
            .Spend(On(10, 10), 10_000)
            .Spend(On(10, 31), 5_000)
            .Spend(On(11, 20), 12_000);

        var pace = await ledger.Pace(Period.Month(2026, 11), On(12, 5));

        Assert.Equal(29, pace.Gap!.Slot);
        Assert.Equal(1.0, pace.Gap.From, 9);
        Assert.Equal(0.8, pace.Gap.To, 9);
        Assert.Equal(10_000 / 15_000.0, pace.Previous!.Levels[29], 9);
        Assert.Equal(1.0, pace.Previous.Levels[30], 9);
        Assert.Equal($"30{Nbsp}€ in meno", pace.Gap.Amount);
    }

    [Fact]
    public async Task Should_put_the_gap_of_a_past_year_on_december()
    {
        var ledger = new Ledger().Spend(On(3, 5, 2024), 10_000).Spend(On(3, 3, 2025), 12_000);

        var pace = await ledger.Pace(Period.Year(2025), Today);

        Assert.Equal(11, pace.Gap!.Slot);
    }

    [Fact]
    public async Task Should_compare_31_march_with_the_whole_of_february()
    {
        var ledger = new Ledger()
            .Spend(On(2, 10), 10_000)
            .Spend(On(2, 28), 5_000)
            .Spend(On(3, 5), 9_000);

        var pace = await ledger.Pace(Period.Month(2026, 3), On(3, 31));

        Assert.Equal(30, pace.Gap!.Slot);
        Assert.Equal(PaceSlotKind.Today, pace.Slots[30].Kind);
        Assert.Equal(28, pace.Previous!.Levels.Count);
        Assert.Equal(1.0, pace.Gap.From, 9);
        Assert.Equal(9_000 / 15_000.0, pace.Gap.To, 9);
        Assert.Equal($"60{Nbsp}€ in meno", pace.Gap.Amount);
        Assert.Equal("di febbraio a oggi", pace.Gap.Caption);
    }

    [Fact]
    public async Task Should_draw_the_previous_line_but_no_gap_when_the_compared_days_had_no_spending()
    {
        var ledger = new Ledger().Spend(On(10, 25), 10_000).Spend(On(11, 3), 5_000);

        var pace = await ledger.Pace(Period.Month(2026, 11), Today);

        Assert.NotNull(pace.Previous);
        Assert.Null(pace.Gap);
    }

    [Fact]
    public async Task Should_leave_out_the_gap_when_the_previous_period_has_no_spending()
    {
        var pace = await new Ledger().Spend(On(11, 3), 5_000).Pace(Period.Month(2026, 11), Today);

        Assert.Null(pace.Gap);
    }

    [Theory]
    [InlineData(91_800, "82 € in meno")]
    [InlineData(101_200, "12 € in più")]
    [InlineData(223_456, "1.235 € in più")]
    [InlineData(99_950, "1 € in meno")]
    [InlineData(100_050, "1 € in più")]
    [InlineData(99_951, "In linea")]
    [InlineData(100_000, "In linea")]
    [InlineData(100_049, "In linea")]
    public async Task Should_label_the_gap_in_whole_euros(long spentCents, string amount)
    {
        var ledger = new Ledger().Spend(On(10, 5), 100_000).Spend(On(11, 3), spentCents);

        var pace = await ledger.Pace(Period.Month(2026, 11), Today);

        Assert.Equal(amount.Replace(" €", $"{Nbsp}€"), pace.Gap!.Amount);
    }

    [Theory]
    [InlineData(2026, 11, 90_000, "di ottobre a oggi")]
    [InlineData(2026, 10, 90_000, "di settembre")]
    [InlineData(2026, 0, 90_000, "del 2025 a oggi")]
    [InlineData(2025, 0, 90_000, "del 2024")]
    [InlineData(2026, 11, 100_000, "con ottobre a oggi")]
    [InlineData(2026, 10, 100_000, "con settembre")]
    [InlineData(2026, 0, 100_000, "con il 2025 a oggi")]
    [InlineData(2025, 0, 100_000, "con il 2024")]
    public async Task Should_caption_the_gap_for_each_kind_of_period(int year, int month, long spentCents, string caption)
    {
        var period = month == 0 ? Period.Year(year) : Period.Month(year, month);
        var ledger = new Ledger()
            .Spend(period.Previous().Start.AddDays(4), 100_000)
            .Spend(period.Start.AddDays(2), spentCents);

        var pace = await ledger.Pace(period, Today);

        Assert.Equal(caption, pace.Gap!.Caption);
    }

    [Theory]
    [InlineData(160_000, "budget 1.600 €")]
    [InlineData(160_050, "budget 1.600,50 €")]
    public async Task Should_draw_the_budget_line_with_its_label(long budgetCents, string label)
    {
        var pace = await new Ledger(budgetCents).Spend(On(11, 3), 10_000).Pace(Period.Month(2026, 11), Today);

        Assert.Equal(1.0, pace.Budget!.Fraction, 9);
        Assert.Equal(label.Replace(" €", $"{Nbsp}€"), pace.Budget.Label);
    }

    [Fact]
    public async Task Should_scale_the_budget_line_to_twelve_months_for_a_year()
    {
        var pace = await new Ledger(100_000).Spend(On(3, 3), 10_000).Pace(Period.Year(2026), Today);

        Assert.Equal(1.0, pace.Budget!.Fraction, 9);
        Assert.Equal($"budget 12.000{Nbsp}€", pace.Budget.Label);
    }

    [Fact]
    public async Task Should_scale_to_the_budget_when_it_is_the_largest()
    {
        var ledger = new Ledger(100_000).Spend(On(10, 31), 60_000).Spend(On(11, 3), 40_000);

        var pace = await ledger.Pace(Period.Month(2026, 11), Today);

        Assert.Equal(1.0, pace.Budget!.Fraction, 9);
        Assert.Equal(0.6, pace.Previous!.Levels[^1], 9);
        Assert.Equal(0.4, pace.Slots[18].Total, 9);
    }

    [Fact]
    public async Task Should_draw_the_budget_line_under_the_top_when_spending_passes_it()
    {
        var ledger = new Ledger(20_000).Spend(On(10, 31), 60_000).Spend(On(11, 3), 40_000);

        var pace = await ledger.Pace(Period.Month(2026, 11), Today);

        Assert.Equal(20_000 / 60_000.0, pace.Budget!.Fraction, 9);
    }

    [Fact]
    public async Task Should_draw_no_budget_line_without_a_budget()
    {
        var pace = await new Ledger().Spend(On(11, 3), 10_000).Pace(Period.Month(2026, 11), Today);

        Assert.Null(pace.Budget);
    }

    [Fact]
    public async Task Should_label_the_axis_at_the_first_day_today_and_the_last_day()
    {
        var pace = await new Ledger().Pace(Period.Month(2026, 11), Today);

        Assert.Equal(new PaceAxis(new PaceTick(0, "1 nov"), new PaceTick(29, "30 nov"), new PaceTick(18, "oggi")), pace.Axis);
    }

    [Fact]
    public async Task Should_end_the_axis_of_february_on_its_28th_slot_and_name_no_today_in_a_past_month()
    {
        var pace = await new Ledger().Pace(Period.Month(2026, 2), Today);

        Assert.Equal(new PaceAxis(new PaceTick(0, "1 feb"), new PaceTick(27, "28 feb"), null), pace.Axis);
    }

    [Fact]
    public async Task Should_label_the_months_of_a_year()
    {
        var current = await new Ledger().Pace(Period.Year(2026), Today);
        var past = await new Ledger().Pace(Period.Year(2025), Today);

        Assert.Equal(new PaceAxis(new PaceTick(0, "gen"), new PaceTick(11, "dic"), new PaceTick(10, "oggi")), current.Axis);
        Assert.Equal(new PaceAxis(new PaceTick(0, "gen"), new PaceTick(11, "dic"), null), past.Axis);
    }

    [Fact]
    public async Task Should_key_automatic_and_variable_amounts_and_the_previous_period()
    {
        var ledger = new Ledger()
            .Automatic(On(11, 1), 10_000)
            .Spend(On(11, 2), 5_000)
            .Spend(On(10, 4), 7_000);

        var pace = await ledger.Pace(Period.Month(2026, 11), Today);

        Assert.Equal(
            [
                new PaceKeyItem(PaceSeries.Automatic, "Automatiche", $"100,00{Nbsp}€"),
                new PaceKeyItem(PaceSeries.Variable, "Variabili", $"50,00{Nbsp}€"),
                new PaceKeyItem(PaceSeries.Previous, "Ottobre", "")
            ],
            pace.Key);
    }

    [Fact]
    public async Task Should_key_the_split_when_only_the_compared_days_had_automatic_spending()
    {
        var ledger = new Ledger().Automatic(On(10, 1), 10_000).Spend(On(11, 2), 5_000);

        var pace = await ledger.Pace(Period.Month(2026, 11), Today);

        Assert.Equal(
            [
                new PaceKeyItem(PaceSeries.Automatic, "Automatiche", $"0,00{Nbsp}€"),
                new PaceKeyItem(PaceSeries.Variable, "Variabili", $"50,00{Nbsp}€"),
                new PaceKeyItem(PaceSeries.Previous, "Ottobre", "")
            ],
            pace.Key);
    }

    [Fact]
    public async Task Should_key_the_period_alone_without_automatic_spending()
    {
        var ledger = new Ledger().Spend(On(11, 2), 5_000).Spend(On(10, 4), 7_000).Spend(On(11, 3, 2025), 1_000);

        var month = await ledger.Pace(Period.Month(2026, 11), Today);
        var year = await ledger.Pace(Period.Year(2026), Today);

        Assert.Equal(
            [new PaceKeyItem(PaceSeries.Spent, "Novembre", ""), new PaceKeyItem(PaceSeries.Previous, "Ottobre", "")],
            month.Key);
        Assert.Equal(
            [new PaceKeyItem(PaceSeries.Spent, "2026", ""), new PaceKeyItem(PaceSeries.Previous, "2025", "")],
            year.Key);
    }

    [Fact]
    public async Task Should_leave_the_previous_period_out_of_the_key_without_a_line()
    {
        var plain = await new Ledger().Spend(On(11, 2), 5_000).Pace(Period.Month(2026, 11), Today);
        var split = await new Ledger().Automatic(On(11, 1), 10_000).Pace(Period.Month(2026, 11), Today);

        Assert.Equal([new PaceKeyItem(PaceSeries.Spent, "Novembre", "")], plain.Key);
        Assert.Equal([PaceSeries.Automatic, PaceSeries.Variable], split.Key.Select(item => item.Series));
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

    // One category, optionally with a budget, one recurring payment for the automatic spending and, on demand, an
    // archived category.
    private sealed class Ledger
    {
        private readonly FakeCategoryRepository _categories = new();
        private readonly FakeExpenseRepository _expenses = new();
        private readonly Category _category;
        private readonly Guid _payment = Guid.NewGuid();
        private Category? _archived;

        public Ledger(long? budgetCents = null)
        {
            _category = new Category(Guid.NewGuid(), "Spesa", "cart", "coral",
                budgetCents is { } cents ? new Money(cents) : null);
            _categories.Items.Add(_category);
        }

        public Ledger Spend(DateOnly on, long cents) => Add(_category, on, cents);

        public Ledger Automatic(DateOnly on, long cents) => Add(_category, on, cents, _payment);

        public Ledger SpendArchived(DateOnly on, long cents) => Add(Archived(), on, cents);

        public async Task<PaceChart> Pace(Period period, DateOnly today)
        {
            var service = new InsightsService(_categories, _expenses, () => today);
            return (await service.Get(period)).Value.Pace;
        }

        private Category Archived()
        {
            if (_archived is not null)
                return _archived;

            _archived = new Category(Guid.NewGuid(), "Vecchia", "more", "grey");
            _archived.Archive();
            _categories.Items.Add(_archived);
            return _archived;
        }

        private Ledger Add(Category category, DateOnly on, long cents, Guid? recurringPaymentId = null)
        {
            _expenses.Items.Add(new Expense(Guid.NewGuid(), category.CategoryId, new Money(cents), on,
                recurringPaymentId: recurringPaymentId));
            return this;
        }
    }
}
