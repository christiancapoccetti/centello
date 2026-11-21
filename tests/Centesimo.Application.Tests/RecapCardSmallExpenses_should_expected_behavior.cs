using Centesimo.Domain;

namespace Centesimo.Application.Tests;

public sealed class RecapCardSmallExpenses_should_expected_behavior
{
    private const string Nbsp = " ";

    private static (RecapWorld World, Category Spesa, Category Bar) November(int coffees, int shopping = 0, long shoppingCents = 450)
    {
        var world = new RecapWorld(new DateOnly(2026, 12, 1));
        var spesa = world.Category("Spesa", "mint");
        var bar = world.Category("Bar", "coral");
        world.SpendInMonth(spesa, 2026, 11, 100_000, 12);
        var caffe = world.Tag(bar, "Caffè");
        for (var index = 0; index < coffees; index++)
            world.Spend(bar, 130, new DateOnly(2026, 11, index % 28 + 1), caffe);

        for (var index = 0; index < shopping; index++)
            world.Spend(spesa, shoppingCents, new DateOnly(2026, 11, index % 28 + 1));

        return (world, spesa, bar);
    }

    [Fact]
    public async Task Should_add_up_the_expenses_under_five_euros()
    {
        var (world, _, _) = November(coffees: 22, shopping: 5);

        var small = (await world.Deck(Period.Month(2026, 11))).Single<RecapCard.SmallExpenses>();

        Assert.Equal($"Le spese sotto i 5{Nbsp}€ fanno", small.Lead);
        Assert.Equal("51,10", small.Amount);
        Assert.Equal(new RecapPhrase("in 27 spese"), small.Detail);
    }

    [Fact]
    public async Task Should_name_the_tag_that_gathers_most_of_them_in_a_month()
    {
        var (world, _, bar) = November(coffees: 22, shopping: 5);

        var small = (await world.Deck(Period.Month(2026, 11))).Single<RecapCard.SmallExpenses>();

        Assert.Equal(
            new RecapTagLine(new RecapCategory(bar.CategoryId, "Bar", "more", "coral"), "Caffè",
                $"22 volte, in media 1,30{Nbsp}€", $"28,60{Nbsp}€"),
            small.TopTag);
    }

    [Fact]
    public async Task Should_draw_a_square_per_expense_in_the_ink_of_its_category_largest_first()
    {
        var (world, _, _) = November(coffees: 22, shopping: 5);

        var units = (await world.Deck(Period.Month(2026, 11))).Single<RecapCard.SmallExpenses>().Units;

        Assert.Equal([.. Enumerable.Repeat("coral", 22), .. Enumerable.Repeat("mint", 5)], units.ColorKeys);
        Assert.Equal($"27 spese sotto i 5{Nbsp}€: 22 in Bar, 5 in Spesa", units.Description);
    }

    [Fact]
    public async Task Should_name_three_categories_and_group_the_others()
    {
        var (world, _, _) = November(coffees: 8, shopping: 3);
        world.Spend(world.Category("Trasporti", "sky"), 300, new DateOnly(2026, 11, 3));
        world.Spend(world.Category("Svago", "lilac"), 300, new DateOnly(2026, 11, 4));
        world.Spend(world.Category("Salute", "peri"), 300, new DateOnly(2026, 11, 5));

        var units = (await world.Deck(Period.Month(2026, 11))).Single<RecapCard.SmallExpenses>().Units;

        Assert.Equal($"14 spese sotto i 5{Nbsp}€: 8 in Bar, 3 in Spesa, 1 in Salute, 2 in altre categorie", units.Description);
    }

    [Theory]
    [InlineData(7, false)]
    [InlineData(8, true)]
    public async Task Should_need_eight_small_expenses_in_a_month(int coffees, bool shown)
    {
        var (world, _, _) = November(coffees);

        var deck = await world.Deck(Period.Month(2026, 11));

        Assert.Equal(shown, deck.Cards.OfType<RecapCard.SmallExpenses>().Any());
    }

    [Theory]
    [InlineData(499, true)]
    [InlineData(500, false)]
    public async Task Should_count_only_the_expenses_below_five_euros(long cents, bool counted)
    {
        var (world, _, _) = November(coffees: 0, shopping: 8, shoppingCents: cents);

        var deck = await world.Deck(Period.Month(2026, 11));

        Assert.Equal(counted, deck.Cards.OfType<RecapCard.SmallExpenses>().Any());
    }

    [Fact]
    public async Task Should_leave_automatic_expenses_out()
    {
        var (world, spesa, _) = November(coffees: 7);
        world.Spend(spesa, 300, new DateOnly(2026, 11, 28), automatic: true);

        var deck = await world.Deck(Period.Month(2026, 11));

        Assert.DoesNotContain(deck.Cards, card => card is RecapCard.SmallExpenses);
    }

    [Fact]
    public async Task Should_say_nothing_about_a_tag_when_no_small_expense_has_one()
    {
        var (world, _, _) = November(coffees: 0, shopping: 8);

        var small = (await world.Deck(Period.Month(2026, 11))).Single<RecapCard.SmallExpenses>();

        Assert.Null(small.TopTag);
    }

    private static async Task<RecapCard.SmallExpenses> YearWith(int coffees)
    {
        var world = new RecapWorld(new DateOnly(2027, 1, 5));
        var spesa = world.Category("Spesa", "mint");
        var bar = world.Category("Bar", "coral");
        world.SpendInYear(spesa, 2026, 1_200_000, 40);
        var caffe = world.Tag(bar, "Caffè");
        for (var index = 0; index < coffees; index++)
            world.Spend(bar, 130, new DateOnly(2026, index % 12 + 1, index % 28 + 1), caffe);

        return (await world.Deck(Period.Year(2026))).Single<RecapCard.SmallExpenses>();
    }

    [Fact]
    public async Task Should_name_the_first_tag_in_the_detail_of_a_year()
    {
        var small = await YearWith(coffees: 262);

        Assert.Equal("340,60", small.Amount);
        Assert.Equal(new RecapPhrase("in 262 spese. Il primo: ", "Caffè, 262 volte", "."), small.Detail);
        Assert.Null(small.TopTag);
    }

    [Theory]
    [InlineData(39, false)]
    [InlineData(40, true)]
    public async Task Should_need_forty_small_expenses_in_a_year(int coffees, bool shown)
    {
        var world = new RecapWorld(new DateOnly(2027, 1, 5));
        var spesa = world.Category("Spesa", "mint");
        world.SpendInYear(spesa, 2026, 1_200_000, 40);
        for (var index = 0; index < coffees; index++)
            world.Spend(spesa, 130, new DateOnly(2026, index % 12 + 1, index % 28 + 1));

        var deck = await world.Deck(Period.Year(2026));

        Assert.Equal(shown, deck.Cards.OfType<RecapCard.SmallExpenses>().Any());
    }
}
