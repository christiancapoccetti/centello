using Centello.Domain;

namespace Centello.Application.Tests;

public sealed class RecapCardChanges_should_expected_behavior
{
    private const string Nbsp = " ";
    private const string Minus = "−";

    private static async Task<RecapDeck> NovemberDeck(
        params (string Name, string Color, long October, long November)[] categories)
    {
        var world = new RecapWorld(new DateOnly(2026, 12, 1));
        foreach (var (name, color, october, november) in categories)
        {
            var category = world.Category(name, color);
            world.SpendInMonth(category, 2026, 10, october, 6);
            world.SpendInMonth(category, 2026, 11, november, 6);
        }

        return await world.Deck(Period.Month(2026, 11));
    }

    [Fact]
    public async Task Should_name_the_category_that_grew_and_the_one_that_fell_in_euros()
    {
        var deck = await NovemberDeck(("Ristoranti", "butter", 11_830, 18_250), ("Trasporti", "sky", 18_980, 14_120),
            ("Casa", "peri", 32_000, 32_000));

        var changes = deck.Single<RecapCard.Changes>();

        Assert.Equal("Ristoranti", changes.Rise!.Category.Name);
        Assert.Equal("+64,20", changes.Rise.Amount);
        Assert.Equal($"Da 118,30 a 182,50{Nbsp}€ rispetto a ottobre.", changes.Rise.Detail);
        Assert.Equal("Trasporti", changes.Fall!.Category.Name);
        Assert.Equal($"{Minus}48,60", changes.Fall.Amount);
        Assert.Equal($"Da 189,80 a 141,20{Nbsp}€.", changes.Fall.Detail);
    }

    [Fact]
    public async Task Should_pick_the_largest_change_in_each_direction()
    {
        var deck = await NovemberDeck(("A", "butter", 10_000, 15_000), ("B", "sky", 10_000, 25_000),
            ("C", "peri", 30_000, 20_000), ("D", "mint", 40_000, 10_000));

        var changes = deck.Single<RecapCard.Changes>();

        Assert.Equal("B", changes.Rise!.Category.Name);
        Assert.Equal("D", changes.Fall!.Category.Name);
    }

    [Theory]
    [InlineData(10_999, false)]
    [InlineData(11_000, true)]
    public async Task Should_need_a_rise_of_ten_euros_in_a_month(long november, bool shown)
    {
        var deck = await NovemberDeck(("Ristoranti", "butter", 10_000, november), ("Casa", "peri", 30_000, 30_000));

        Assert.Equal(shown, deck.Cards.OfType<RecapCard.Changes>().SingleOrDefault()?.Rise is not null);
    }

    [Theory]
    [InlineData(9_001, false)]
    [InlineData(9_000, true)]
    public async Task Should_need_a_fall_of_ten_euros_in_a_month(long november, bool shown)
    {
        var deck = await NovemberDeck(("Trasporti", "sky", 10_000, november), ("Casa", "peri", 30_000, 30_000));

        Assert.Equal(shown, deck.Cards.OfType<RecapCard.Changes>().SingleOrDefault()?.Fall is not null);
    }

    [Fact]
    public async Task Should_say_nothing_when_no_change_is_worth_it()
    {
        var deck = await NovemberDeck(("Ristoranti", "butter", 10_000, 10_500), ("Trasporti", "sky", 10_000, 9_500));

        Assert.DoesNotContain(deck.Cards, card => card is RecapCard.Changes);
    }

    [Fact]
    public async Task Should_show_a_card_with_only_a_fall()
    {
        var deck = await NovemberDeck(("Trasporti", "sky", 30_000, 10_000), ("Casa", "peri", 30_000, 30_000));

        var changes = deck.Single<RecapCard.Changes>();

        Assert.Null(changes.Rise);
        Assert.Equal(changes.Fall!.Surface, changes.Top);
        Assert.Equal(changes.Fall.Surface, changes.Bottom);
    }

    [Fact]
    public async Task Should_not_call_a_category_with_nothing_before_it_a_rise()
    {
        var world = new RecapWorld(new DateOnly(2026, 12, 1));
        var casa = world.Category("Casa", "peri");
        world.SpendInMonth(casa, 2026, 10, 30_000, 6);
        world.SpendInMonth(casa, 2026, 11, 30_000, 6);
        world.SpendInMonth(world.Category("Nuova", "butter"), 2026, 11, 30_000, 6);

        var deck = await world.Deck(Period.Month(2026, 11));

        Assert.DoesNotContain(deck.Cards, card => card is RecapCard.Changes);
    }

    [Fact]
    public async Task Should_not_compare_with_a_month_with_nothing_spent()
    {
        var world = new RecapWorld(new DateOnly(2026, 12, 1));
        world.SpendInMonth(world.Category("Ristoranti", "butter"), 2026, 11, 30_000, 6);
        world.SpendInMonth(world.Category("Casa", "peri"), 2026, 11, 30_000, 6);

        var deck = await world.Deck(Period.Month(2026, 11));

        Assert.DoesNotContain(deck.Cards, card => card is RecapCard.Changes);
    }

    [Fact]
    public async Task Should_keep_a_coral_half_on_the_ground()
    {
        var deck = await NovemberDeck(("Bar", "coral", 10_000, 20_000), ("Trasporti", "sky", 30_000, 10_000));

        var changes = deck.Single<RecapCard.Changes>();

        Assert.True(changes.Rise!.Surface.IsGround);
        Assert.True(changes.Top.IsGround);
        Assert.Equal("sky", changes.Bottom.PaletteKey);
    }

    [Fact]
    public async Task Should_leave_the_rise_for_the_summary_and_the_fall_when_there_is_no_rise()
    {
        var both = (await NovemberDeck(("Ristoranti", "butter", 11_830, 18_250), ("Trasporti", "sky", 18_980, 14_120)))
            .Single<RecapCard.Changes>();
        var fallOnly = (await NovemberDeck(("Trasporti", "sky", 30_000, 10_000), ("Casa", "peri", 30_000, 30_000)))
            .Single<RecapCard.Changes>();

        Assert.Equal(new RecapSummaryRow(MaterialSymbols.NorthEast, "Cresciuta di più", $"Ristoranti +64{Nbsp}€"), both.Row);
        Assert.Equal(new RecapSummaryRow(MaterialSymbols.SouthEast, "Calata di più", $"Trasporti {Minus}200{Nbsp}€"), fallOnly.Row);
    }

    [Fact]
    public async Task Should_compare_a_year_in_whole_euros_with_the_year_before()
    {
        var world = new RecapWorld(new DateOnly(2027, 1, 5));
        var ristoranti = world.Category("Ristoranti", "butter");
        var trasporti = world.Category("Trasporti", "sky");
        var casa = world.Category("Casa", "peri");
        world.SpendInYear(ristoranti, 2025, 156_000, 12);
        world.SpendInYear(ristoranti, 2026, 218_000, 12);
        world.SpendInYear(trasporti, 2025, 250_000, 12);
        world.SpendInYear(trasporti, 2026, 196_000, 12);
        world.SpendInYear(casa, 2025, 384_000, 12);
        world.SpendInYear(casa, 2026, 384_000, 12);

        var changes = (await world.Deck(Period.Year(2026))).Single<RecapCard.Changes>();

        Assert.Equal("+620", changes.Rise!.Amount);
        Assert.Equal($"Da 1.560 a 2.180{Nbsp}€ rispetto al 2025.", changes.Rise.Detail);
        Assert.Equal($"{Minus}540", changes.Fall!.Amount);
        Assert.Equal($"Da 2.500 a 1.960{Nbsp}€.", changes.Fall.Detail);
    }

    [Theory]
    [InlineData(4_999, false)]
    [InlineData(5_000, true)]
    public async Task Should_need_a_change_of_fifty_euros_in_a_year(long rise, bool shown)
    {
        var world = new RecapWorld(new DateOnly(2027, 1, 5));
        var ristoranti = world.Category("Ristoranti", "butter");
        var casa = world.Category("Casa", "peri");
        world.SpendInYear(ristoranti, 2025, 100_000, 20);
        world.SpendInYear(ristoranti, 2026, 100_000 + rise, 20);
        world.SpendInYear(casa, 2025, 300_000, 20);
        world.SpendInYear(casa, 2026, 300_000, 20);

        var deck = await world.Deck(Period.Year(2026));

        Assert.Equal(shown, deck.Cards.OfType<RecapCard.Changes>().Any());
    }
}
