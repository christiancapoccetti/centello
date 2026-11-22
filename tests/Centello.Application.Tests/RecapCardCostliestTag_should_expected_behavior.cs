using Centello.Domain;

namespace Centello.Application.Tests;

public sealed class RecapCardCostliestTag_should_expected_behavior
{
    private const string Nbsp = " ";

    private static readonly int[] Monthly = [2, 2, 2, 2, 2, 2, 2, 2, 2, 2];

    private static RecapWorld World(out Category spesa, out Category ristoranti)
    {
        var world = new RecapWorld(new DateOnly(2027, 1, 5));
        spesa = world.Category("Spesa", "mint");
        ristoranti = world.Category("Ristoranti", "butter");
        world.SpendInYear(spesa, 2026, 1_200_000, 40);
        return world;
    }

    [Fact]
    public async Task Should_name_the_tag_with_the_most_euros_when_it_is_not_the_most_used()
    {
        var world = World(out var spesa, out var ristoranti);
        world.SpendWithTag(spesa, world.Tag(spesa, "Pane"), 300, 2026, Monthly);
        world.SpendWithTag(ristoranti, world.Tag(ristoranti, "Cene"), 5_000, 2026, [1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1]);

        var tag = (await world.Deck(Period.Year(2026))).Single<RecapCard.CostliestTag>();

        Assert.Equal("Il tag che ti è costato di più", tag.Lead);
        Assert.Equal("Cene", tag.Tag);
        Assert.Equal("Ristoranti", tag.Category.Name);
        Assert.Equal(new RecapPhrase("", $"600,00{Nbsp}€ in tutto", ", 12 volte"), tag.Detail);
    }

    [Fact]
    public async Task Should_draw_the_same_chart_as_the_tag_of_the_year()
    {
        var world = World(out var spesa, out var ristoranti);
        world.SpendWithTag(spesa, world.Tag(spesa, "Pane"), 300, 2026, Monthly);
        world.SpendWithTag(ristoranti, world.Tag(ristoranti, "Cene"), 5_000, 2026, [1, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1]);

        var tag = (await world.Deck(Period.Year(2026))).Single<RecapCard.CostliestTag>();

        Assert.Equal("Cene mese per mese, da 1 a 2 volte", tag.Chart.Description);
        Assert.Equal(12, tag.Chart.Bars.Count);
        Assert.Equal(new RecapBar(1.0, RecapBarKind.Strong, Label: "F"), tag.Chart.Bars[1]);
        Assert.Equal("butter", tag.Surface.PaletteKey);
    }

    [Fact]
    public async Task Should_say_nothing_when_the_costliest_tag_is_the_tag_of_the_year()
    {
        var world = World(out _, out var ristoranti);
        world.SpendWithTag(ristoranti, world.Tag(ristoranti, "Pizza"), 1_250, 2026, [2, 2, 2, 2, 2, 2, 2]);

        var deck = await world.Deck(Period.Year(2026));

        Assert.Equal("Pizza", deck.Single<RecapCard.TagOfYear>().Tag);
        Assert.DoesNotContain(deck.Cards, card => card is RecapCard.CostliestTag);
    }

    [Theory]
    [InlineData(11, false)]
    [InlineData(12, true)]
    public async Task Should_need_twelve_uses(int uses, bool shown)
    {
        var world = World(out var spesa, out var ristoranti);
        world.SpendWithTag(spesa, world.Tag(spesa, "Pane"), 300, 2026, [3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3]);
        world.SpendWithTag(ristoranti, world.Tag(ristoranti, "Cene"), 9_000, 2026, [uses]);

        var deck = await world.Deck(Period.Year(2026));

        Assert.Equal(shown, deck.Cards.OfType<RecapCard.CostliestTag>().Any());
    }

    [Fact]
    public async Task Should_say_nothing_when_no_tag_is_used_often_enough()
    {
        var world = World(out var spesa, out var ristoranti);
        world.SpendWithTag(spesa, world.Tag(spesa, "Pane"), 300, 2026, [5]);
        world.SpendWithTag(ristoranti, world.Tag(ristoranti, "Cene"), 9_000, 2026, [4]);

        var deck = await world.Deck(Period.Year(2026));

        Assert.DoesNotContain(deck.Cards, card => card is RecapCard.TagOfYear or RecapCard.CostliestTag);
    }

    [Fact]
    public async Task Should_keep_a_coral_category_on_the_ground()
    {
        var world = World(out var spesa, out _);
        var bar = world.Category("Bar", "coral");
        world.SpendWithTag(spesa, world.Tag(spesa, "Pane"), 300, 2026, Monthly);
        world.SpendWithTag(bar, world.Tag(bar, "Aperitivi"), 5_000, 2026, [1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1]);

        var tag = (await world.Deck(Period.Year(2026))).Single<RecapCard.CostliestTag>();

        Assert.True(tag.Surface.IsGround);
        Assert.All(tag.Chart.Bars, chartBar => Assert.Equal(RecapBarKind.Palette, chartBar.Kind));
    }

    [Fact]
    public async Task Should_not_leave_a_summary_row()
    {
        var world = World(out var spesa, out var ristoranti);
        world.SpendWithTag(spesa, world.Tag(spesa, "Pane"), 300, 2026, Monthly);
        world.SpendWithTag(ristoranti, world.Tag(ristoranti, "Cene"), 5_000, 2026, [1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1]);

        var tag = (await world.Deck(Period.Year(2026))).Single<RecapCard.CostliestTag>();

        Assert.Null(tag.Row);
    }

    [Fact]
    public async Task Should_not_belong_to_a_month()
    {
        var world = new RecapWorld(new DateOnly(2026, 12, 1));
        var spesa = world.Category("Spesa", "mint");
        world.SpendInMonth(spesa, 2026, 11, 100_000, 12);
        world.SpendWithTag(spesa, world.Tag(spesa, "Pane"), 300, 2026, [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 14]);
        world.SpendWithTag(spesa, world.Tag(spesa, "Cene"), 9_000, 2026, [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 13]);

        var deck = await world.Deck(Period.Month(2026, 11));

        Assert.DoesNotContain(deck.Cards, card => card is RecapCard.CostliestTag);
    }
}
