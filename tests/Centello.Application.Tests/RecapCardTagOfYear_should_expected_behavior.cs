using Centello.Domain;

namespace Centello.Application.Tests;

public sealed class RecapCardTagOfYear_should_expected_behavior
{
    private const string Nbsp = " ";

    private static readonly int[] Pizzas = [3, 4, 2, 3, 5, 4, 3, 2, 4, 3, 4, 4];

    private static RecapWorld World(out Category spesa, out Category ristoranti)
    {
        var world = new RecapWorld(new DateOnly(2027, 1, 5));
        spesa = world.Category("Spesa", "mint");
        ristoranti = world.Category("Ristoranti", "butter");
        world.SpendInYear(spesa, 2026, 1_200_000, 40);
        return world;
    }

    [Fact]
    public async Task Should_name_the_most_used_tag_with_its_total_and_average()
    {
        var world = World(out _, out var ristoranti);
        var pizza = world.Tag(ristoranti, "Pizza");
        world.SpendWithTag(ristoranti, pizza, 1_250, 2026, Pizzas);

        var tag = (await world.Deck(Period.Year(2026))).Single<RecapCard.TagOfYear>();

        Assert.Equal("Il tag dell'anno", tag.Lead);
        Assert.Equal("Pizza", tag.Tag);
        Assert.Equal("Ristoranti", tag.Category.Name);
        Assert.Equal(new RecapPhrase("", "41 volte", $", 512,50{Nbsp}€ in tutto, 12,50{Nbsp}€ in media."), tag.Detail);
    }

    [Fact]
    public async Task Should_draw_the_uses_of_each_month_in_tile_ink()
    {
        var world = World(out _, out var ristoranti);
        world.SpendWithTag(ristoranti, world.Tag(ristoranti, "Pizza"), 1_250, 2026, Pizzas);

        var chart = (await world.Deck(Period.Year(2026))).Single<RecapCard.TagOfYear>().Chart;

        Assert.Equal(12, chart.Bars.Count);
        Assert.Equal(new RecapBar(3 / 5.0, RecapBarKind.Strong, Label: "G"), chart.Bars[0]);
        Assert.Equal(new RecapBar(1.0, RecapBarKind.Strong, Label: "M"), chart.Bars[4]);
        Assert.Equal(2 / 5.0, chart.Bars[2].Height);
        Assert.Equal("Pizza mese per mese, da 2 a 5 volte", chart.Description);
    }

    [Fact]
    public async Task Should_leave_a_month_without_a_use_as_a_stub()
    {
        var world = World(out _, out var ristoranti);
        world.SpendWithTag(ristoranti, world.Tag(ristoranti, "Pizza"), 1_250, 2026, [4, 0, 4, 4]);

        var chart = (await world.Deck(Period.Year(2026))).Single<RecapCard.TagOfYear>().Chart;

        Assert.Equal(new RecapBar(0, RecapBarKind.Stub, Label: "F"), chart.Bars[1]);
        Assert.Equal("Pizza mese per mese, 4 volte", chart.Description);
    }

    [Theory]
    [InlineData(11, false)]
    [InlineData(12, true)]
    public async Task Should_need_twelve_uses(int uses, bool shown)
    {
        var world = World(out _, out var ristoranti);
        world.SpendWithTag(ristoranti, world.Tag(ristoranti, "Pizza"), 1_250, 2026, [uses]);

        var deck = await world.Deck(Period.Year(2026));

        Assert.Equal(shown, deck.Cards.OfType<RecapCard.TagOfYear>().Any());
    }

    [Fact]
    public async Task Should_pick_the_most_used_tag_by_count_and_not_by_euros()
    {
        var world = World(out var spesa, out var ristoranti);
        world.SpendWithTag(spesa, world.Tag(spesa, "Pane"), 300, 2026, [2, 2, 2, 2, 2, 2, 2, 2, 2, 2]);
        world.SpendWithTag(ristoranti, world.Tag(ristoranti, "Cene"), 9_000, 2026, [1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1]);

        var tag = (await world.Deck(Period.Year(2026))).Single<RecapCard.TagOfYear>();

        Assert.Equal("Pane", tag.Tag);
    }

    [Fact]
    public async Task Should_read_variable_expenses_only()
    {
        var world = World(out _, out var ristoranti);
        var pizza = world.Tag(ristoranti, "Pizza");
        for (var month = 1; month <= 12; month++)
            world.Spend(ristoranti, 1_250, new DateOnly(2026, month, 5), pizza, automatic: true);

        var deck = await world.Deck(Period.Year(2026));

        Assert.DoesNotContain(deck.Cards, card => card is RecapCard.TagOfYear);
    }

    [Fact]
    public async Task Should_paint_the_card_with_the_category_and_the_bars_in_tile_ink()
    {
        var world = World(out _, out var ristoranti);
        world.SpendWithTag(ristoranti, world.Tag(ristoranti, "Pizza"), 1_250, 2026, Pizzas);

        var tag = (await world.Deck(Period.Year(2026))).Single<RecapCard.TagOfYear>();

        Assert.Equal("butter", tag.Surface.PaletteKey);
        Assert.All(tag.Chart.Bars, bar => Assert.Equal(RecapBarKind.Strong, bar.Kind));
    }

    [Fact]
    public async Task Should_keep_a_coral_category_on_the_ground_and_draw_its_bars_in_its_own_ink()
    {
        var world = World(out _, out _);
        var bar = world.Category("Bar", "coral");
        world.SpendWithTag(bar, world.Tag(bar, "Caffè"), 130, 2026, Pizzas);

        var tag = (await world.Deck(Period.Year(2026))).Single<RecapCard.TagOfYear>();

        Assert.True(tag.Surface.IsGround);
        Assert.All(tag.Chart.Bars, chartBar => Assert.Equal(RecapBarKind.Palette, chartBar.Kind));
        Assert.All(tag.Chart.Bars, chartBar => Assert.Equal("coral", chartBar.ColorKey));
    }

    [Fact]
    public async Task Should_leave_a_summary_row_with_the_tag_and_its_uses()
    {
        var world = World(out _, out var ristoranti);
        world.SpendWithTag(ristoranti, world.Tag(ristoranti, "Pizza"), 1_250, 2026, Pizzas);

        var tag = (await world.Deck(Period.Year(2026))).Single<RecapCard.TagOfYear>();

        Assert.Equal(new RecapSummaryRow(MaterialSymbols.Sell, "Il tag dell'anno", "Pizza · 41"), tag.Row);
    }

    [Fact]
    public async Task Should_not_belong_to_a_month()
    {
        var world = new RecapWorld(new DateOnly(2026, 12, 1));
        var spesa = world.Category("Spesa", "mint");
        world.SpendInMonth(spesa, 2026, 11, 100_000, 12);
        world.SpendWithTag(spesa, world.Tag(spesa, "Pane"), 300, 2026, [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 15]);

        var deck = await world.Deck(Period.Month(2026, 11));

        Assert.DoesNotContain(deck.Cards, card => card is RecapCard.TagOfYear);
    }
}
