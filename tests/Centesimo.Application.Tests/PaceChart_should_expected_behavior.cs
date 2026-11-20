using Centesimo.Domain;

namespace Centesimo.Application.Tests;

public sealed class PaceChart_should_expected_behavior
{
    private const char Nbsp = (char)0xA0;

    private static readonly DateOnly Today = new(2026, 11, 19);

    [Fact]
    public async Task Should_title_a_month_and_a_year_the_same()
    {
        var month = await new Ledger().Pace(Period.Month(2026, 11), Today);
        var year = await new Ledger().Pace(Period.Year(2026), Today);

        Assert.Equal("Spesa accumulata", month.Title);
        Assert.Equal("Spesa accumulata", year.Title);
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

    [Theory]
    [InlineData(38_000, 50_000, 38_000, 91_000, SplitVerdict.OnlyVariableMoved,
        "Le automatiche sono uguali a ottobre: tutto il calo viene dalle spese variabili.")]
    [InlineData(38_000, 91_000, 38_000, 50_000, SplitVerdict.OnlyVariableMoved,
        "Le automatiche sono uguali a ottobre: tutto l'aumento viene dalle spese variabili.")]
    [InlineData(38_100, 50_000, 38_000, 91_000, SplitVerdict.OnlyVariableMoved,
        "Le automatiche sono uguali a ottobre: tutto il calo viene dalle spese variabili.")]
    [InlineData(38_700, 50_000, 38_000, 91_000, SplitVerdict.OnlyVariableMoved,
        "Le automatiche sono uguali a ottobre: tutto il calo viene dalle spese variabili.")]
    [InlineData(1_299, 50_000, 1_199, 91_000, SplitVerdict.OnlyVariableMoved,
        "Le automatiche sono uguali a ottobre: tutto il calo viene dalle spese variabili.")]
    [InlineData(40_500, 50_000, 40_000, 50_300, SplitVerdict.BothMoved,
        "Le automatiche sono 5 € più alte di ottobre, le variabili 3 € più basse.")]
    [InlineData(40_000, 50_300, 40_500, 50_000, SplitVerdict.BothMoved,
        "Le automatiche sono 5 € più basse di ottobre, le variabili 3 € più alte.")]
    [InlineData(1_300, 90_000, 1_199, 90_000, SplitVerdict.OnlyAutomaticMoved,
        "Le automatiche sono 1 € più alte di ottobre; le variabili sono uguali.")]
    [InlineData(38_000, 90_000, 38_000, 90_000, SplitVerdict.BothSame,
        "Automatiche e variabili sono in linea con ottobre.")]
    [InlineData(38_100, 90_049, 38_000, 90_000, SplitVerdict.BothSame,
        "Automatiche e variabili sono in linea con ottobre.")]
    [InlineData(40_000, 90_000, 38_000, 90_000, SplitVerdict.OnlyAutomaticMoved,
        "Le automatiche sono 20 € più alte di ottobre; le variabili sono uguali.")]
    [InlineData(36_000, 90_049, 38_000, 90_000, SplitVerdict.OnlyAutomaticMoved,
        "Le automatiche sono 20 € più basse di ottobre; le variabili sono uguali.")]
    [InlineData(39_000, 90_000, 38_000, 90_000, SplitVerdict.OnlyAutomaticMoved,
        "Le automatiche sono 10 € più alte di ottobre; le variabili sono uguali.")]
    [InlineData(40_000, 50_000, 38_000, 75_000, SplitVerdict.BothMoved,
        "Le automatiche sono 20 € più alte di ottobre, le variabili 250 € più basse.")]
    [InlineData(36_000, 80_000, 38_000, 75_000, SplitVerdict.BothMoved,
        "Le automatiche sono 20 € più basse di ottobre, le variabili 50 € più alte.")]
    [InlineData(38_000, 50_000, 0, 100_000, SplitVerdict.OnlyOneSide,
        "Negli stessi giorni di ottobre non risultano spese automatiche.")]
    [InlineData(0, 90_000, 38_000, 50_000, SplitVerdict.OnlyOneSide,
        "Ancora nessuna spesa automatica; negli stessi giorni di ottobre erano 380,00 €.")]
    [InlineData(0, 90_000, 0, 100_000, SplitVerdict.NoAutomatic, "")]
    [InlineData(38_000, 50_000, 0, 0, SplitVerdict.NothingToCompare, "")]
    public async Task Should_decide_how_automatic_and_variable_spending_moved_and_word_it(long automatic, long variable,
        long comparedAutomatic, long comparedVariable, SplitVerdict verdict, string sentence)
    {
        var ledger = LedgerSplit(Period.Month(2026, 11), automatic, variable, comparedAutomatic, comparedVariable);

        var pace = await ledger.Pace(Period.Month(2026, 11), Today);

        Assert.Equal(verdict, pace.Split);
        Assert.Equal(sentence.Replace(" €", $"{Nbsp}€"), pace.Sentence);
        Assert.Equal(sentence.Length > 0, pace.HasSentence);
    }

    [Theory]
    [InlineData(2026, 10, 38_000, 50_000, 0, 100_000, "A settembre non risultano spese automatiche.")]
    [InlineData(2026, 0, 38_000, 50_000, 0, 100_000, "Nello stesso periodo del 2025 non risultano spese automatiche.")]
    [InlineData(2025, 0, 38_000, 50_000, 0, 100_000, "Nel 2024 non risultano spese automatiche.")]
    [InlineData(2026, 10, 0, 90_000, 38_000, 50_000, "Nessuna spesa automatica; a settembre erano 380,00 €.")]
    [InlineData(2026, 0, 0, 90_000, 38_000, 50_000,
        "Ancora nessuna spesa automatica; nello stesso periodo del 2025 erano 380,00 €.")]
    [InlineData(2025, 0, 0, 90_000, 38_000, 50_000, "Nessuna spesa automatica; nel 2024 erano 380,00 €.")]
    [InlineData(2026, 10, 38_000, 50_000, 38_000, 91_000,
        "Le automatiche sono uguali a settembre: tutto il calo viene dalle spese variabili.")]
    [InlineData(2026, 5, 38_000, 50_000, 38_000, 91_000,
        "Le automatiche sono uguali ad aprile: tutto il calo viene dalle spese variabili.")]
    [InlineData(2026, 0, 38_000, 50_000, 38_000, 91_000,
        "Le automatiche sono uguali al 2025: tutto il calo viene dalle spese variabili.")]
    [InlineData(2025, 0, 38_000, 50_000, 38_000, 91_000,
        "Le automatiche sono uguali al 2024: tutto il calo viene dalle spese variabili.")]
    [InlineData(2026, 10, 40_000, 90_000, 38_000, 90_000,
        "Le automatiche sono 20 € più alte di settembre; le variabili sono uguali.")]
    [InlineData(2026, 0, 40_000, 90_000, 38_000, 90_000,
        "Le automatiche sono 20 € più alte del 2025; le variabili sono uguali.")]
    [InlineData(2026, 10, 38_000, 90_000, 38_000, 90_000, "Automatiche e variabili sono in linea con settembre.")]
    [InlineData(2026, 0, 38_000, 90_000, 38_000, 90_000, "Automatiche e variabili sono in linea con il 2025.")]
    public async Task Should_name_the_compared_period_in_the_split_sentence(int year, int month, long automatic,
        long variable, long comparedAutomatic, long comparedVariable, string sentence)
    {
        var period = month == 0 ? Period.Year(year) : Period.Month(year, month);
        var ledger = LedgerSplit(period, automatic, variable, comparedAutomatic, comparedVariable);

        var pace = await ledger.Pace(period, Today);

        Assert.Equal(sentence.Replace(" €", $"{Nbsp}€"), pace.Sentence);
    }

    [Fact]
    public async Task Should_say_nothing_to_compare_when_the_compared_days_had_no_spending()
    {
        var ledger = new Ledger().Automatic(On(11, 1), 38_000).Spend(On(10, 25), 10_000);

        var pace = await ledger.Pace(Period.Month(2026, 11), Today);

        Assert.NotNull(pace.Previous);
        Assert.Equal(SplitVerdict.NothingToCompare, pace.Split);
        Assert.False(pace.HasSentence);
    }

    [Fact]
    public async Task Should_describe_the_pace_for_screen_readers()
    {
        var ledger = new Ledger(160_000)
            .Spend(On(10, 5), 100_000)
            .Spend(On(10, 28), 50_000)
            .Spend(On(11, 3), 90_000);

        var pace = await ledger.Pace(Period.Month(2026, 11), Today);

        Assert.Equal(
            "Spesa accumulata di novembre fino a oggi: 900,00 €. Negli stessi giorni di ottobre: 1.000,00 €. "
            + "Ottobre ha chiuso a 1.500,00 €. Budget: 1.600 €.",
            pace.Description.Replace($"{Nbsp}€", " €"));
    }

    [Fact]
    public async Task Should_describe_the_split_when_there_is_one()
    {
        var ledger = new Ledger().Automatic(On(11, 1), 38_000).Spend(On(11, 3), 5_000).Spend(On(10, 5), 1_000);

        var pace = await ledger.Pace(Period.Month(2026, 11), Today);

        Assert.StartsWith(
            $"Spesa accumulata di novembre fino a oggi: 430,00{Nbsp}€, di cui automatiche 380,00{Nbsp}€. ",
            pace.Description);
    }

    [Fact]
    public async Task Should_describe_a_year_to_date()
    {
        var ledger = new Ledger().Spend(On(3, 3), 10_000).Spend(On(3, 5, 2025), 20_000).Spend(On(12, 5, 2025), 30_000);

        var pace = await ledger.Pace(Period.Year(2026), Today);

        Assert.Equal(
            "Spesa accumulata del 2026 fino a oggi: 100,00 €. Nello stesso periodo del 2025: 200,00 €. "
            + "Il 2025 ha chiuso a 500,00 €.",
            pace.Description.Replace($"{Nbsp}€", " €"));
    }

    [Fact]
    public async Task Should_describe_a_past_period_against_the_whole_previous_one_once()
    {
        var month = await new Ledger().Spend(On(10, 3), 10_000).Spend(On(9, 5), 20_000).Pace(Period.Month(2026, 10), Today);
        var year = await new Ledger().Spend(On(3, 3, 2025), 10_000).Spend(On(3, 5, 2024), 20_000).Pace(Period.Year(2025), Today);

        Assert.Equal(
            "Spesa accumulata di ottobre: 100,00 €. A settembre: 200,00 €.",
            month.Description.Replace($"{Nbsp}€", " €"));
        Assert.Equal(
            "Spesa accumulata del 2025: 100,00 €. Nel 2024: 200,00 €.",
            year.Description.Replace($"{Nbsp}€", " €"));
    }

    [Fact]
    public async Task Should_leave_out_of_the_description_what_the_chart_does_not_draw()
    {
        var alone = await new Ledger().Spend(On(11, 3), 5_000).Pace(Period.Month(2026, 11), Today);
        var later = await new Ledger().Spend(On(11, 3), 5_000).Spend(On(10, 25), 10_000).Pace(Period.Month(2026, 11), Today);

        Assert.Equal("Spesa accumulata di novembre fino a oggi: 50,00 €.", alone.Description.Replace($"{Nbsp}€", " €"));
        Assert.Equal(
            "Spesa accumulata di novembre fino a oggi: 50,00 €. Ottobre ha chiuso a 100,00 €.",
            later.Description.Replace($"{Nbsp}€", " €"));
    }

    [Fact]
    public async Task Should_word_november_like_the_mockup()
    {
        var pace = (await InsightFixtures.November().Overview()).Pace;

        Assert.Equal(SplitVerdict.OnlyVariableMoved, pace.Split);
        Assert.Equal("Le automatiche sono uguali a ottobre: tutto il calo viene dalle spese variabili.", pace.Sentence);
        Assert.Equal(
            [
                new PaceKeyItem(PaceSeries.Automatic, "Automatiche", $"412,99{Nbsp}€"),
                new PaceKeyItem(PaceSeries.Variable, "Variabili", $"871,61{Nbsp}€"),
                new PaceKeyItem(PaceSeries.Previous, "Ottobre", "")
            ],
            pace.Key);
        Assert.Equal(new PaceAxis(new PaceTick(0, "1 nov"), new PaceTick(29, "30 nov"), new PaceTick(18, "oggi")), pace.Axis);
        Assert.Equal(new PaceGap(18, 136_660 / 160_000.0, 128_460 / 160_000.0, $"82{Nbsp}€ in meno", "di ottobre a oggi"),
            pace.Gap);
        Assert.Equal(new PaceBudget(1.0, $"budget 1.600{Nbsp}€"), pace.Budget);
        Assert.Equal($"ottobre ha chiuso a 1.590{Nbsp}€", pace.Previous!.CloseLabel);
        Assert.Equal(
            $"Spesa accumulata di novembre fino a oggi: 1.284,60{Nbsp}€, di cui automatiche 412,99{Nbsp}€. "
            + $"Negli stessi giorni di ottobre: 1.366,60{Nbsp}€. Ottobre ha chiuso a 1.590,00{Nbsp}€. Budget: 1.600{Nbsp}€.",
            pace.Description);
    }

    [Fact]
    public async Task Should_draw_the_mockup_month_day_by_day()
    {
        var pace = (await InsightFixtures.November().Overview()).Pace;

        var automatic = Enumerable.Range(1, 19).Select(day => InsightFixtures.AutomaticDays.GetValueOrDefault(day));
        AssertClose(
            [..Running(InsightFixtures.NovemberDays, InsightFixtures.BudgetCents), ..Run(0.0, 12)],
            pace.Slots.Select(slot => slot.Total));
        AssertClose(
            [..Running(automatic, InsightFixtures.BudgetCents), ..Run(0.0, 12)],
            pace.Slots.Select(slot => slot.Automatic));
        AssertClose(Running(InsightFixtures.OctoberDays, InsightFixtures.BudgetCents).ToList(), pace.Previous!.Levels);
        Assert.Equal(PaceSlotKind.Today, pace.Slots[18].Kind);
    }

    [Fact]
    public async Task Should_say_no_automatic_expenses_were_recorded_to_compare_with()
    {
        var ledger = new Ledger()
            .Automatic(On(11, 1), 38_000)
            .Spend(On(11, 3), 5_000)
            .Spend(On(10, 3), 9_000);

        var pace = await ledger.Pace(Period.Month(2026, 11), Today);

        Assert.Equal(SplitVerdict.OnlyOneSide, pace.Split);
        Assert.Equal("Negli stessi giorni di ottobre non risultano spese automatiche.", pace.Sentence);
        Assert.Equal([PaceSeries.Automatic, PaceSeries.Variable, PaceSeries.Previous], pace.Key.Select(item => item.Series));
    }

    private static Ledger LedgerSplit(Period period, long automatic, long variable, long comparedAutomatic, long comparedVariable)
    {
        var ledger = new Ledger();
        Fill(ledger, period.Start, automatic, variable);
        Fill(ledger, period.Previous().Start, comparedAutomatic, comparedVariable);
        return ledger;
    }

    private static void Fill(Ledger ledger, DateOnly firstDay, long automatic, long variable)
    {
        if (automatic > 0)
            ledger.Automatic(firstDay, automatic);

        if (variable > 0)
            ledger.Spend(firstDay.AddDays(1), variable);
    }

    private static DateOnly On(int month, int day, int year = 2026) => new(year, month, day);

    private static IEnumerable<T> Run<T>(T value, int count) => Enumerable.Repeat(value, count);

    private static IEnumerable<double> Running(IEnumerable<long> cents, long scale)
    {
        var sum = 0L;
        foreach (var amount in cents)
        {
            sum += amount;
            yield return (double)sum / scale;
        }
    }

    private static void AssertClose(IReadOnlyList<double> expected, IEnumerable<double> actual)
    {
        var values = actual.ToList();
        Assert.Equal(expected.Count, values.Count);
        for (var index = 0; index < expected.Count; index++)
            Assert.Equal(expected[index], values[index], 9);
    }

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
