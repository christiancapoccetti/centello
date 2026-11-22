using Centello.Domain;

namespace Centello.Application.Tests;

public sealed class RecapCardYearSoFar_should_expected_behavior
{
    private const string Nbsp = " ";

    private static readonly long[] Months2026 =
        [151_210, 119_800, 142_000, 135_500, 147_600, 139_000, 170_200, 194_800, 138_800, 139_680, 128_460];

    private static readonly long[] Months2025 =
        [147_940, 130_200, 139_500, 141_000, 152_000, 145_500, 181_000, 201_000, 140_200, 140_000, 123_610];

    private static Task<RecapDeck> Deck(long[] thisYear, long[] lastYear) =>
        Deck(thisYear, lastYear, new DateOnly(2026, thisYear.Length + 1, 1));

    private static async Task<RecapDeck> Deck(long[] thisYear, long[] lastYear, DateOnly today)
    {
        var month = thisYear.Length;
        var world = new RecapWorld(today);
        var spesa = world.Category("Spesa", "mint");
        for (var index = 0; index < lastYear.Length; index++)
            world.Spend(spesa, lastYear[index], new DateOnly(2025, index + 1, 10));

        for (var index = 0; index < month - 1; index++)
            world.Spend(spesa, thisYear[index], new DateOnly(2026, index + 1, 10));

        world.SpendInMonth(spesa, 2026, month, thisYear[^1], 12);
        return await world.Deck(Period.Month(2026, month));
    }

    [Fact]
    public async Task Should_state_the_year_so_far_against_the_same_days_a_year_earlier()
    {
        var yearSoFar = (await Deck(Months2026, Months2025)).Single<RecapCard.YearSoFar>();

        Assert.Equal("Nel 2026 hai speso finora", yearSoFar.Lead);
        Assert.Equal("16.071", yearSoFar.Amount);
        Assert.Equal(new RecapChip($"349{Nbsp}€ meno del 2025 a fine novembre", ComparisonTrend.Lower), yearSoFar.Chip);
        Assert.Equal("2026", yearSoFar.ThisYear);
        Assert.Equal(new RecapPhrase("2025, ", $"16.420{Nbsp}€"), yearSoFar.LastYear);
    }

    [Fact]
    public async Task Should_say_where_the_year_stood_when_the_month_is_not_the_latest_closed()
    {
        var yearSoFar = (await Deck([100_000, 100_000, 100_000], [150_000, 150_000, 150_000], new DateOnly(2026, 11, 5)))
            .Single<RecapCard.YearSoFar>();

        Assert.Equal("A fine marzo 2026 avevi speso", yearSoFar.Lead);
        Assert.Equal("3.000", yearSoFar.Amount);
        Assert.Equal(new RecapChip($"1.500{Nbsp}€ meno del 2025 a fine marzo", ComparisonTrend.Lower), yearSoFar.Chip);
    }

    [Fact]
    public async Task Should_keep_the_wording_for_december_opened_later()
    {
        var months = Enumerable.Repeat(100_000L, 12).ToArray();

        var yearSoFar = (await Deck(months, months, new DateOnly(2027, 5, 1))).Single<RecapCard.YearSoFar>();

        Assert.Equal("Nel 2026 hai speso finora", yearSoFar.Lead);
    }

    [Fact]
    public async Task Should_paint_a_chip_neutral_when_the_year_cost_more()
    {
        var yearSoFar = (await Deck(Months2025, Months2026)).Single<RecapCard.YearSoFar>();

        Assert.Equal(new RecapChip($"349{Nbsp}€ più del 2025 a fine novembre", ComparisonTrend.Higher), yearSoFar.Chip);
    }

    [Fact]
    public async Task Should_leave_out_the_chip_when_the_two_years_are_level()
    {
        var yearSoFar = (await Deck(Months2025, Months2025)).Single<RecapCard.YearSoFar>();

        Assert.Null(yearSoFar.Chip);
    }

    [Fact]
    public async Task Should_draw_the_cumulative_year_against_last_years_dashed_line()
    {
        var chart = (await Deck([100_000, 100_000, 100_000, 100_000], [150_000, 150_000, 150_000, 150_000]))
            .Single<RecapCard.YearSoFar>().Chart;

        Assert.Equal(12, chart.Bars.Count);
        Assert.Equal([1 / 6.0, 2 / 6.0, 3 / 6.0, 4 / 6.0], chart.Bars.Take(4).Select(bar => bar.Height));
        Assert.Equal([RecapBarKind.Bar, RecapBarKind.Bar, RecapBarKind.Bar, RecapBarKind.Strong], chart.Bars.Take(4).Select(bar => bar.Kind));
        Assert.All(chart.Bars.Skip(4), bar => Assert.Equal(new RecapBar(0, RecapBarKind.Stub), bar));
        Assert.Equal([0.25, 0.5, 0.75, 1.0], chart.Previous);
        Assert.Equal(
            $"Spesa accumulata nel 2026 contro il 2025: 4.000{Nbsp}€ contro 6.000{Nbsp}€ a fine aprile",
            chart.Description);
    }

    [Fact]
    public async Task Should_scale_the_chart_to_the_higher_of_the_two_years()
    {
        var chart = (await Deck([200_000, 200_000], [50_000, 50_000])).Single<RecapCard.YearSoFar>().Chart;

        Assert.Equal(1.0, chart.Bars[1].Height);
        Assert.Equal([0.125, 0.25], chart.Previous);
    }

    [Fact]
    public async Task Should_say_nothing_in_january()
    {
        var deck = await Deck([150_000], [100_000]);

        Assert.DoesNotContain(deck.Cards, card => card is RecapCard.YearSoFar);
    }

    [Fact]
    public async Task Should_show_from_february_on()
    {
        var deck = await Deck([150_000, 150_000], [100_000, 100_000]);

        Assert.Single(deck.Cards.OfType<RecapCard.YearSoFar>());
    }

    [Fact]
    public async Task Should_say_nothing_when_nothing_was_spent_the_same_days_a_year_earlier()
    {
        var deck = await Deck([150_000, 150_000, 150_000], []);

        Assert.DoesNotContain(deck.Cards, card => card is RecapCard.YearSoFar);
    }

    [Fact]
    public async Task Should_not_belong_to_a_year()
    {
        var world = new RecapWorld(new DateOnly(2027, 1, 5));
        world.SpendInYear(world.Category("Spesa", "mint"), 2026, 1_200_000, 40);

        var deck = await world.Deck(Period.Year(2026));

        Assert.DoesNotContain(deck.Cards, card => card is RecapCard.YearSoFar);
    }
}
