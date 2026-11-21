using Centesimo.Domain;

namespace Centesimo.Application.Tests;

public sealed class RecapCardSummary_should_expected_behavior
{
    [Fact]
    public async Task Should_close_a_month_with_its_expenses_as_the_way_out()
    {
        var world = new RecapWorld(new DateOnly(2026, 12, 1));
        world.SpendInMonth(world.Category("Spesa", "mint"), 2026, 11, 100_000, 12);

        var summary = (await world.Deck(Period.Month(2026, 11))).Single<RecapCard.Summary>();

        Assert.Equal("Novembre in breve", summary.Headline);
        Assert.Equal(new RecapExit("Vedi le spese di novembre", RecapExitTarget.MonthExpenses), summary.Exit);
        Assert.Equal("Rivedi dall'inizio", summary.Restart);
    }

    [Fact]
    public async Task Should_close_a_year_with_insight_as_the_way_out()
    {
        var world = new RecapWorld(new DateOnly(2027, 1, 5));
        world.SpendInYear(world.Category("Spesa", "mint"), 2026, 1_200_000, 40);

        var summary = (await world.Deck(Period.Year(2026))).Single<RecapCard.Summary>();

        Assert.Equal("Il 2026 in breve", summary.Headline);
        Assert.Equal(new RecapExit("Vedi il 2026 in Insight", RecapExitTarget.Insight), summary.Exit);
        Assert.Equal("Rivedi dall'inizio", summary.Restart);
    }

    [Fact]
    public async Task Should_title_the_deck_with_the_span_it_covers()
    {
        var world = new RecapWorld(new DateOnly(2026, 11, 21));
        world.SpendInYear(world.Category("Spesa", "mint"), 2026, 1_200_000, 40, lastMonth: 10);

        var deck = await world.Deck(Period.Year(2026));

        Assert.Equal("Il tuo 2026", deck.Title);
        Assert.Equal("finora", deck.Badge);
    }
}
