using Centello.Domain;

namespace Centello.Application.Tests;

public sealed class RecapCardSpendingMap_should_expected_behavior
{
    private static readonly Period November = Period.Month(2026, 11);
    private static readonly DateOnly Day = November.Start;

    [Fact]
    public async Task Should_conserve_both_sources_by_category_and_tag_id_and_keep_unresolved_tags()
    {
        var world = new RecapWorld();
        var home = world.Category("Casa", "peri", archived: true);
        var food = world.Category("Spesa", "mint");
        var homeTag = world.Tag(home, "Acquisti");
        var foodTag = world.Tag(food, "Acquisti");
        var lost = new Tag(Guid.NewGuid(), home.CategoryId, "Scomparso");
        world.SpendInMonth(food, 2026, 11, 100, 10);
        world.Spend(home, 1_223, Day, homeTag, automatic: true);
        world.Spend(home, 235, Day, homeTag);
        world.Spend(home, 100, Day, foodTag);
        world.Spend(home, 63, Day, lost, automatic: true);
        world.Spend(home, 5, Day);
        world.Spend(food, 999, Day, foodTag);
        world.Spend(home, 500_000, Day.AddDays(-1), homeTag);

        var deck = await world.Deck(November);
        var map = Assert.IsType<RecapCard.SpendingMap>(deck.Cards[^2]);

        Assert.Equal(2_725, map.TotalCents);
        Assert.Equal(new[] { home.CategoryId, food.CategoryId }, map.Categories.Select(row => row.Category.CategoryId));
        Assert.Equal(new RecapFlowTag(homeTag.TagId, "Acquisti", 1_223, 235), map.FocusTag);
        Assert.Equal(home.CategoryId, map.FocusCategory.Category.CategoryId);
        Assert.Equal(new RecapFlowTag(null, "Senza tag", 63, 105), map.Categories[0].Tags[1]);
        Assert.Equal(foodTag.TagId, map.Categories[1].Tags[0].TagId);
        Assert.Equal("Novembre 2026", map.PeriodLabel);
        Assert.Contains("54%", map.Headline);
        Assert.DoesNotContain("€", map.Description);
        Assert.Equal("<1%", map.Share(1));
        Assert.DoesNotContain(deck.Cards, card => card is RecapCard.Categories);
        Assert.Contains(deck.Single<RecapCard.Summary>().Rows, row => row.Label == "Prima categoria" && row.Value == "Casa · 60%");
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task Should_keep_the_category_card_when_origins_or_named_tags_cannot_be_compared(bool automatic, bool named)
    {
        var world = new RecapWorld();
        var home = world.Category("Casa", "peri");
        var food = world.Category("Spesa", "mint");
        world.SpendInMonth(food, 2026, 11, 1_000, 10);
        world.Spend(home, 500, Day, named ? world.Tag(home, "Affitto") : null, automatic: automatic);

        var deck = await world.Deck(November);

        Assert.Single(deck.Cards.OfType<RecapCard.Categories>());
        Assert.DoesNotContain(deck.Cards, card => card is RecapCard.SpendingMap);
    }

    [Fact]
    public async Task Should_omit_the_map_when_all_expenses_are_automatic_or_in_one_category()
    {
        var world = new RecapWorld();
        var home = world.Category("Casa", "peri");
        var rent = world.Tag(home, "Affitto");
        for (var day = 0; day < 10; day++)
            world.Spend(home, 500, Day.AddDays(day), rent, automatic: true);

        world.Spend(home, 25, Day);
        var oneCategory = await world.Deck(November);
        Assert.DoesNotContain(oneCategory.Cards, card => card is RecapCard.SpendingMap or RecapCard.Categories);

        world.Expenses.Items.RemoveAll(expense => !expense.IsAutomatic);
        world.Spend(world.Category("Spesa", "mint"), 200, Day, automatic: true);
        var onlyAutomatic = await world.Deck(November);
        Assert.Single(onlyAutomatic.Cards.OfType<RecapCard.Categories>());
        Assert.DoesNotContain(onlyAutomatic.Cards, card => card is RecapCard.SpendingMap);
    }

    [Fact]
    public async Task Should_break_equal_amounts_consistently_when_repository_order_changes()
    {
        var world = new RecapWorld();
        var food = world.Category("Spesa", "mint");
        var home = world.Category("Casa", "peri");
        var gas = world.Tag(home, "Gas");
        var electricity = world.Tag(home, "Luce");
        var bread = world.Tag(food, "Pane");
        for (var day = 0; day < 5; day++)
        {
            world.Spend(food, 10, Day.AddDays(day), bread);
            world.Spend(food, 10, Day.AddDays(day));
            world.Spend(home, 10, Day.AddDays(day), electricity, automatic: true);
            world.Spend(home, 10, Day.AddDays(day), gas);
        }

        var first = (await world.Deck(November)).Single<RecapCard.SpendingMap>();
        world.Expenses.Items.Reverse();
        var reversed = (await world.Deck(November)).Single<RecapCard.SpendingMap>();

        Assert.Equal(gas.TagId, first.FocusTag.TagId);
        Assert.Equal(first.FocusTag, reversed.FocusTag);
        Assert.Equal(["Casa", "Spesa"], reversed.Categories.Select(row => row.Category.Name));
        Assert.Equal(["Gas", "Luce"], reversed.Categories[0].Tags.Select(tag => tag.Name));
    }

    [Fact]
    public async Task Should_require_one_percent_of_actual_period_spending_before_highlighting_a_tag()
    {
        var world = new RecapWorld();
        var home = world.Category("Casa", "peri");
        var food = world.Category("Spesa", "mint");
        var rent = world.Tag(home, "Affitto");
        world.SpendInMonth(food, 2026, 11, 9_900, 10);
        world.Spend(home, 99, Day, rent, automatic: true);
        var untaggedCent = world.Spend(home, 1, Day);

        var below = await world.Deck(November);
        Assert.DoesNotContain(below.Cards, card => card is RecapCard.SpendingMap);
        Assert.Single(below.Cards.OfType<RecapCard.Categories>());

        world.Expenses.Items.Remove(untaggedCent);
        world.Spend(home, 1, Day, rent);
        var exact = await world.Deck(November);
        var map = Assert.IsType<RecapCard.SpendingMap>(exact.Cards[^2]);

        Assert.Equal(10_000, map.TotalCents);
        Assert.Equal(100, map.FocusTag.TotalCents);
        Assert.Equal("1%", map.Share(map.FocusTag.TotalCents));
    }

    [Fact]
    public async Task Should_label_and_cut_the_current_year_at_the_last_complete_day()
    {
        var world = new RecapWorld(new DateOnly(2026, 12, 30));
        var home = world.Category("Casa", "peri");
        var food = world.Category("Spesa", "mint");
        var rent = world.Tag(home, "Affitto");
        world.SpendInYear(food, 2026, 10_000, 45);
        world.Spend(home, 500, new DateOnly(2026, 12, 29), rent, automatic: true);
        world.Spend(home, 800_000, new DateOnly(2026, 12, 30), rent);

        var deck = await world.Deck(Period.Year(2026));
        var map = Assert.IsType<RecapCard.SpendingMap>(deck.Cards[^2]);

        Assert.Equal(10_500, map.TotalCents);
        Assert.Contains("29 dicembre", map.PeriodLabel);
        Assert.Contains("2026", map.PeriodLabel);
        Assert.Contains("5%", map.Headline);
    }
}
