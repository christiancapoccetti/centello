using Centesimo.Domain;

namespace Centesimo.Application.Tests;

public sealed class RecapCardMonths_should_expected_behavior
{
    private const string Nbsp = " ";

    private static readonly long[] Months2026 =
        [151_200, 119_800, 142_000, 135_500, 147_600, 139_000, 170_200, 194_800, 138_800, 139_700, 128_500, 188_400];

    private static async Task<RecapDeck> Year(long[] months, DateOnly today)
    {
        var world = new RecapWorld(today);
        var spesa = world.Category("Spesa", "mint");
        for (var index = 0; index < months.Length; index++)
            world.SpendInMonth(spesa, 2026, index + 1, months[index], 6);

        return await world.Deck(Period.Year(2026));
    }

    [Fact]
    public async Task Should_name_the_dearest_and_the_lightest_month()
    {
        var months = (await Year(Months2026, new DateOnly(2027, 1, 5))).Single<RecapCard.Months>();

        Assert.Equal("Agosto il più caro, febbraio il più leggero", months.Headline);
        Assert.Equal($"1.948{Nbsp}€ contro 1.198{Nbsp}€.", months.Detail);
    }

    [Fact]
    public async Task Should_draw_the_dearest_month_strong_and_the_lightest_outlined()
    {
        var chart = (await Year(Months2026, new DateOnly(2027, 1, 5))).Single<RecapCard.Months>().Chart;

        Assert.Equal(new RecapBar(1.0, RecapBarKind.Strong, "1.948", "A"), chart.Bars[7]);
        Assert.Equal(new RecapBar(119_800 / 194_800.0, RecapBarKind.Outlined, "1.198", "F"), chart.Bars[1]);
        Assert.Equal(
            $"Spesa per mese nel 2026: agosto il più caro con 1.948{Nbsp}€, febbraio il più leggero con 1.198{Nbsp}€",
            chart.Description);
    }

    [Theory]
    [InlineData(5, false)]
    [InlineData(6, true)]
    public async Task Should_need_six_months_with_spending(int months, bool shown)
    {
        var deck = await Year(Months2026[..months], new DateOnly(2027, 1, 5));

        Assert.Equal(shown, deck.Cards.OfType<RecapCard.Months>().Any());
    }

    [Fact]
    public async Task Should_draw_but_never_rank_the_month_of_the_last_day_of_a_year_so_far()
    {
        long[] months = [100_000, 110_000, 120_000, 130_000, 140_000, 150_000, 160_000, 170_000, 180_000, 190_000, 900_000];

        var deck = await Year(months, new DateOnly(2026, 11, 21));

        var chart = deck.Single<RecapCard.Months>().Chart;
        Assert.Equal(RecapBarKind.Bar, chart.Bars[10].Kind);
        Assert.Equal(1.0, chart.Bars[10].Height);
        Assert.Equal(RecapBarKind.Strong, chart.Bars[9].Kind);
        Assert.Equal("Ottobre il più caro, gennaio il più leggero", deck.Single<RecapCard.Months>().Headline);
    }

    [Fact]
    public async Task Should_leave_a_summary_row_with_the_dearest_month()
    {
        var months = (await Year(Months2026, new DateOnly(2027, 1, 5))).Single<RecapCard.Months>();

        Assert.Equal(new RecapSummaryRow(MaterialSymbols.CalendarMonth, "Il mese più caro", "Agosto"), months.Row);
    }

    [Fact]
    public async Task Should_not_belong_to_a_month()
    {
        var world = new RecapWorld(new DateOnly(2026, 12, 1));
        world.SpendInMonth(world.Category("Spesa", "mint"), 2026, 11, 100_000, 12);

        var deck = await world.Deck(Period.Month(2026, 11));

        Assert.DoesNotContain(deck.Cards, card => card is RecapCard.Months);
    }
}
