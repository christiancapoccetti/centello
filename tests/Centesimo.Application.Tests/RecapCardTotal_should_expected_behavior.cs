using Centesimo.Domain;

namespace Centesimo.Application.Tests;

public sealed class RecapCardTotal_should_expected_behavior
{
    private const string Nbsp = " ";

    private static (RecapWorld World, Category Spesa) November(long october = 139_680, long lastYear = 123_610,
        long november = 128_460)
    {
        var world = new RecapWorld(new DateOnly(2026, 12, 1));
        var spesa = world.Category("Spesa", "mint");
        world.SpendInMonth(spesa, 2026, 11, november, 96);
        if (october > 0)
            world.SpendInMonth(spesa, 2026, 10, october, 12);

        if (lastYear > 0)
            world.SpendInMonth(spesa, 2025, 11, lastYear, 12);

        return (world, spesa);
    }

    [Fact]
    public async Task Should_state_the_spending_of_a_month_with_its_cents_and_its_expenses()
    {
        var (world, _) = November();

        var total = (await world.Deck(Period.Month(2026, 11))).Single<RecapCard.Total>();

        Assert.Equal("A novembre hai speso", total.Lead);
        Assert.Equal("1.284,60", total.Amount);
        Assert.Equal(new RecapPhrase("in 96 spese"), total.Detail);
    }

    [Fact]
    public async Task Should_compare_with_the_previous_month_and_the_same_month_a_year_earlier()
    {
        var (world, _) = November();

        var total = (await world.Deck(Period.Month(2026, 11))).Single<RecapCard.Total>();

        Assert.Equal(
            [
                new RecapChip($"112,20{Nbsp}€ meno di ottobre", ComparisonTrend.Lower),
                new RecapChip($"48,50{Nbsp}€ più di novembre 2025", ComparisonTrend.Higher)
            ],
            total.Chips);
    }

    [Fact]
    public async Task Should_draw_the_three_months_against_the_widest()
    {
        var (world, _) = November();

        var total = (await world.Deck(Period.Month(2026, 11))).Single<RecapCard.Total>();

        Assert.Equal(
            [
                new RecapMeasure("Novembre 2025", 123_610 / 139_680.0, $"1.236,10{Nbsp}€", false),
                new RecapMeasure("Ottobre", 1.0, $"1.396,80{Nbsp}€", false),
                new RecapMeasure("Novembre", 128_460 / 139_680.0, $"1.284,60{Nbsp}€", true)
            ],
            total.Measures);
    }

    [Fact]
    public async Task Should_paint_a_chip_green_only_when_the_month_cost_less()
    {
        var (world, _) = November(october: 100_000, lastYear: 150_000);

        var chips = (await world.Deck(Period.Month(2026, 11))).Single<RecapCard.Total>().Chips;

        Assert.Equal(ComparisonTrend.Lower, Assert.Single(chips, chip => chip.Text.Contains("novembre 2025")).Trend);
        Assert.Equal(ComparisonTrend.Higher, Assert.Single(chips, chip => chip.Text.Contains("ottobre")).Trend);
    }

    [Fact]
    public async Task Should_leave_out_a_comparison_with_nothing_spent()
    {
        var (world, _) = November(october: 0, lastYear: 0);

        var total = (await world.Deck(Period.Month(2026, 11))).Single<RecapCard.Total>();

        Assert.Empty(total.Chips);
        Assert.Equal(["Novembre"], total.Measures.Select(measure => measure.Label));
    }

    [Fact]
    public async Task Should_leave_out_a_chip_when_the_spending_is_the_same()
    {
        var (world, _) = November(october: 128_460, lastYear: 0);

        var total = (await world.Deck(Period.Month(2026, 11))).Single<RecapCard.Total>();

        Assert.Empty(total.Chips);
        Assert.Equal(2, total.Measures.Count);
    }

    [Fact]
    public async Task Should_compare_a_january_with_the_december_before_it_by_naming_the_year()
    {
        var world = new RecapWorld(new DateOnly(2026, 2, 1));
        var spesa = world.Category("Spesa", "mint");
        world.SpendInMonth(spesa, 2026, 1, 100_000, 12);
        world.SpendInMonth(spesa, 2025, 12, 150_000, 12);

        var total = (await world.Deck(Period.Month(2026, 1))).Single<RecapCard.Total>();

        Assert.Equal($"500,00{Nbsp}€ meno di dicembre 2025", total.Chips.Single().Text);
    }

    [Fact]
    public async Task Should_leave_the_total_row_for_the_summary()
    {
        var (world, _) = November();

        var deck = await world.Deck(Period.Month(2026, 11));

        Assert.Equal(
            new RecapSummaryRow(MaterialSymbols.Payments, "Totale", $"1.284,60{Nbsp}€"),
            deck.Single<RecapCard.Total>().Row);
        Assert.Contains(deck.Single<RecapCard.Total>().Row!, deck.Single<RecapCard.Summary>().Rows);
    }

    [Fact]
    public async Task Should_count_the_automatic_expenses_in_the_total()
    {
        var (world, spesa) = November(november: 100_000);
        world.Spend(spesa, 38_000, new DateOnly(2026, 11, 1), automatic: true);

        var total = (await world.Deck(Period.Month(2026, 11))).Single<RecapCard.Total>();

        Assert.Equal("1.380,00", total.Amount);
        Assert.Equal("in 97 spese", total.Detail.Lead);
    }

    private static RecapWorld Year(DateOnly today, int year = 2026, long spent = 1_795_500, int count = 1_143,
        int lastMonth = 12)
    {
        var world = new RecapWorld(today);
        var spesa = world.Category("Spesa", "mint");
        world.SpendInYear(spesa, year, spent, count, lastMonth);
        world.SpendInYear(spesa, year - 1, 1_834_000, 400, lastMonth: 10);
        return world;
    }

    [Fact]
    public async Task Should_state_the_spending_of_a_year_in_whole_euros_with_the_monthly_average()
    {
        var world = Year(new DateOnly(2027, 1, 5));

        var total = (await world.Deck(Period.Year(2026))).Single<RecapCard.Total>();

        Assert.Equal("Nel 2026 hai speso", total.Lead);
        Assert.Equal("17.955", total.Amount);
        Assert.Equal(new RecapPhrase("in 1.143 spese, ", $"1.496{Nbsp}€", " al mese in media"), total.Detail);
    }

    [Fact]
    public async Task Should_compare_a_year_with_the_year_before_in_whole_euros()
    {
        var world = Year(new DateOnly(2027, 1, 5));

        var total = (await world.Deck(Period.Year(2026))).Single<RecapCard.Total>();

        Assert.Equal([new RecapChip($"385{Nbsp}€ meno del 2025", ComparisonTrend.Lower)], total.Chips);
        Assert.Equal(
            [
                new RecapMeasure("2025", 1.0, $"18.340{Nbsp}€", false),
                new RecapMeasure("2026", 1_795_500 / 1_834_000.0, $"17.955{Nbsp}€", true)
            ],
            total.Measures);
    }

    [Fact]
    public async Task Should_paint_the_year_chip_neutral_when_it_cost_more()
    {
        var world = Year(new DateOnly(2027, 1, 5), spent: 1_900_000);

        var total = (await world.Deck(Period.Year(2026))).Single<RecapCard.Total>();

        Assert.Equal(ComparisonTrend.Higher, total.Chips.Single().Trend);
    }

    [Fact]
    public async Task Should_say_until_when_the_year_so_far_is_counted_and_compare_the_same_days()
    {
        var world = Year(new DateOnly(2026, 11, 21), spent: 1_000_000, count: 500, lastMonth: 10);

        var total = (await world.Deck(Period.Year(2026))).Single<RecapCard.Total>();

        Assert.Equal("Fino al 20 novembre hai speso", total.Lead);
        Assert.Equal("10.000", total.Amount);
        Assert.Equal(
            new RecapChip($"8.340{Nbsp}€ meno del 2025 al 20 novembre", ComparisonTrend.Lower),
            total.Chips.Single());
        Assert.Equal(["2025 al 20 novembre", "2026 al 20 novembre"], total.Measures.Select(measure => measure.Label));
    }

    [Fact]
    public async Task Should_average_a_year_so_far_over_the_months_its_days_make_up()
    {
        var world = Year(new DateOnly(2026, 11, 21), spent: 1_000_000, count: 500, lastMonth: 10);

        var total = (await world.Deck(Period.Year(2026))).Single<RecapCard.Total>();

        // 10.000 € over 324 of 365 days is 12 * 324 / 365 = 10,65 months.
        Assert.Equal($"939{Nbsp}€", total.Detail.Emphasis);
    }
}
