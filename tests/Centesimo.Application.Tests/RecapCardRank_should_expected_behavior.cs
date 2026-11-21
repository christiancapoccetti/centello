using Centesimo.Domain;

namespace Centesimo.Application.Tests;

public sealed class RecapCardRank_should_expected_behavior
{
    private const string Nbsp = " ";

    private static readonly long[] FirstSixMonths = [100_000, 110_000, 120_000, 130_000, 140_000, 150_000];

    private static Task<RecapDeck> JulyAmong(long[] earlier, long july) => JulyAmong(earlier, july, new DateOnly(2026, 8, 1));

    private static async Task<RecapDeck> JulyAmong(long[] earlier, long july, DateOnly today)
    {
        var world = new RecapWorld(today);
        var spesa = world.Category("Spesa", "mint");
        for (var index = 0; index < earlier.Length; index++)
            world.Spend(spesa, earlier[index], new DateOnly(2026, index + 1, 10));

        world.SpendInMonth(spesa, 2026, earlier.Length + 1, july, 10);
        return await world.Deck(Period.Month(2026, earlier.Length + 1));
    }

    [Theory]
    [InlineData(90_000, "Il mese più leggero del 2026", "Il più caro è stato giugno: 1.500 €.")]
    [InlineData(105_000, "Il secondo mese più leggero del 2026", "Solo gennaio è costato meno: 1.000 €.")]
    [InlineData(115_000, "Il terzo mese più leggero del 2026", "Solo gennaio e febbraio sono costati meno.")]
    [InlineData(135_000, "Il terzo mese più caro del 2026", "Solo giugno e maggio sono costati di più.")]
    [InlineData(145_000, "Il secondo mese più caro del 2026", "Solo giugno è costato di più: 1.500 €.")]
    [InlineData(160_000, "Il mese più caro del 2026", "Il più leggero è stato gennaio: 1.000 €.")]
    public async Task Should_word_the_first_three_ranks_from_both_ends(long july, string headline, string detail)
    {
        var rank = (await JulyAmong(FirstSixMonths, july)).Single<RecapCard.Rank>();

        Assert.Equal(headline, rank.Headline);
        Assert.Equal(detail, rank.Detail);
    }

    [Theory]
    [InlineData(90_000, "Il mese più leggero del 2026 fino a luglio", "2026-11-05")]
    [InlineData(105_000, "Il secondo mese più leggero del 2026 fino a luglio", "2026-11-05")]
    [InlineData(160_000, "Il mese più caro del 2026 fino a luglio", "2026-09-01")]
    public async Task Should_name_the_cut_when_the_month_is_not_the_latest_closed(long july, string headline, string today)
    {
        var rank = (await JulyAmong(FirstSixMonths, july, DateOnly.Parse(today))).Single<RecapCard.Rank>();

        Assert.Equal(headline, rank.Headline);
        Assert.Equal("Fino a luglio", rank.Row!.Label);
    }

    [Fact]
    public async Task Should_keep_the_wording_of_the_latest_closed_month_through_the_month_after()
    {
        var rank = (await JulyAmong(FirstSixMonths, 90_000, new DateOnly(2026, 8, 31))).Single<RecapCard.Rank>();

        Assert.Equal("Il mese più leggero del 2026", rank.Headline);
    }

    [Fact]
    public async Task Should_keep_the_wording_for_december_opened_later()
    {
        var world = new RecapWorld(new DateOnly(2027, 5, 1));
        var spesa = world.Category("Spesa", "mint");
        for (var month = 1; month <= 11; month++)
            world.Spend(spesa, 100_000 + month * 1_000, new DateOnly(2026, month, 10));

        world.SpendInMonth(spesa, 2026, 12, 50_000, 10);
        var rank = (await world.Deck(Period.Month(2026, 12))).Single<RecapCard.Rank>();

        Assert.Equal("Il mese più leggero del 2026", rank.Headline);
        Assert.Equal("Nell'anno", rank.Row!.Label);
    }

    [Fact]
    public async Task Should_say_nothing_in_the_middle_of_the_year()
    {
        var deck = await JulyAmong(FirstSixMonths, 125_000);

        Assert.DoesNotContain(deck.Cards, card => card is RecapCard.Rank);
    }

    [Theory]
    [InlineData(2, false)]
    [InlineData(3, true)]
    public async Task Should_need_three_months_with_spending_so_far(int months, bool shown)
    {
        var deck = await JulyAmong(FirstSixMonths[..(months - 1)], 50_000);

        Assert.Equal(shown, deck.Cards.OfType<RecapCard.Rank>().Any());
    }

    [Fact]
    public async Task Should_not_count_an_equal_month_as_cheaper()
    {
        var deck = await JulyAmong([100_000, 100_000, 100_000], 100_000);

        Assert.DoesNotContain(deck.Cards, card => card is RecapCard.Rank);
    }

    [Fact]
    public async Task Should_draw_the_months_so_far_with_the_month_strong_and_the_cited_ones_valued()
    {
        var rank = (await JulyAmong(FirstSixMonths, 105_000)).Single<RecapCard.Rank>();

        Assert.Equal(7, rank.Chart.Bars.Count);
        Assert.Equal(new RecapBar(105_000 / 150_000.0, RecapBarKind.Strong, "1.050", "L"), rank.Chart.Bars[6]);
        Assert.Equal(new RecapBar(100_000 / 150_000.0, RecapBarKind.Bar, "1.000", "G"), rank.Chart.Bars[0]);
        Assert.Equal(new RecapBar(110_000 / 150_000.0, RecapBarKind.Bar, Label: "F"), rank.Chart.Bars[1]);
        Assert.Empty(rank.Chart.Previous);
        Assert.Equal($"Spesa per mese nel 2026: luglio 1.050{Nbsp}€, il secondo più leggero", rank.Chart.Description);
    }

    [Fact]
    public async Task Should_leave_a_summary_row_with_the_rank()
    {
        var second = (await JulyAmong(FirstSixMonths, 105_000)).Single<RecapCard.Rank>();
        var first = (await JulyAmong(FirstSixMonths, 160_000)).Single<RecapCard.Rank>();

        Assert.Equal(new RecapSummaryRow(MaterialSymbols.CalendarMonth, "Nell'anno", "2° più leggero"), second.Row);
        Assert.Equal(new RecapSummaryRow(MaterialSymbols.CalendarMonth, "Nell'anno", "Più caro"), first.Row);
    }

    [Fact]
    public async Task Should_not_rank_a_year()
    {
        var world = new RecapWorld(new DateOnly(2027, 1, 5));
        world.SpendInYear(world.Category("Spesa", "mint"), 2026, 1_200_000, 40);

        var deck = await world.Deck(Period.Year(2026));

        Assert.DoesNotContain(deck.Cards, card => card is RecapCard.Rank);
    }
}
