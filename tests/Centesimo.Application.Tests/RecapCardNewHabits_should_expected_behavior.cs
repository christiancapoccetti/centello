using Centesimo.Domain;

namespace Centesimo.Application.Tests;

public sealed class RecapCardNewHabits_should_expected_behavior
{
    private const string Nbsp = " ";

    private static RecapWorld World(out Category spesa, out Category svago)
    {
        var world = new RecapWorld(new DateOnly(2027, 1, 5));
        spesa = world.Category("Spesa", "mint");
        svago = world.Category("Svago", "lilac");
        world.SpendInYear(spesa, 2026, 1_200_000, 40);
        world.SpendInYear(spesa, 2025, 1_000_000, 40);
        return world;
    }

    private static void SpendThreeNewTags(RecapWorld world, Category spesa, Category svago)
    {
        world.SpendWithTag(svago, world.Tag(svago, "Piscina"), 850, 2026, [0, 0, 0, 0, 0, 0, 0, 0, 5, 5, 4, 4]);
        world.SpendWithTag(svago, world.Tag(svago, "Libri"), 1_630, 2026, [0, 0, 3, 3, 3]);
        world.SpendWithTag(spesa, world.Tag(spesa, "Veterinario"), 5_250, 2026, [0, 0, 0, 0, 0, 1, 1, 1, 1]);
    }

    [Fact]
    public async Task Should_list_the_tags_the_year_before_did_not_have_most_used_first()
    {
        var world = World(out var spesa, out var svago);
        SpendThreeNewTags(world, spesa, svago);

        var habits = (await world.Deck(Period.Year(2026))).Single<RecapCard.NewHabits>();

        Assert.Equal("Tre abitudini nuove", habits.Headline);
        Assert.Equal("Nel 2025 non c'erano.", habits.Detail);
        Assert.Equal(
            [
                ("Piscina", "Svago, da settembre, 18 volte", $"153,00{Nbsp}€"),
                ("Libri", "Svago, da marzo, 9 volte", $"146,70{Nbsp}€"),
                ("Veterinario", "Spesa, da giugno, 4 volte", $"210,00{Nbsp}€")
            ],
            habits.Habits.Select(habit => (habit.Tag, habit.Detail, habit.Amount)));
        Assert.Equal(["Svago", "Svago", "Spesa"], habits.Habits.Select(habit => habit.Category.Name));
    }

    [Theory]
    [InlineData(1, "Un'abitudine nuova", "Nel 2025 non c'era.")]
    [InlineData(2, "Due abitudini nuove", "Nel 2025 non c'erano.")]
    public async Task Should_word_one_and_two_habits(int count, string headline, string detail)
    {
        var world = World(out var spesa, out var svago);
        world.SpendWithTag(svago, world.Tag(svago, "Piscina"), 850, 2026, [0, 0, 0, 0, 0, 0, 0, 0, 5, 5, 4, 4]);
        if (count == 2)
            world.SpendWithTag(spesa, world.Tag(spesa, "Veterinario"), 5_250, 2026, [0, 0, 0, 0, 0, 1, 1, 1, 1]);

        var habits = (await world.Deck(Period.Year(2026))).Single<RecapCard.NewHabits>();

        Assert.Equal(headline, habits.Headline);
        Assert.Equal(detail, habits.Detail);
    }

    [Fact]
    public async Task Should_show_the_three_most_used_of_four()
    {
        var world = World(out var spesa, out var svago);
        SpendThreeNewTags(world, spesa, svago);
        world.SpendWithTag(spesa, world.Tag(spesa, "Pane"), 300, 2026, [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 5, 5]);

        var habits = (await world.Deck(Period.Year(2026))).Single<RecapCard.NewHabits>();

        Assert.Equal(["Piscina", "Pane", "Libri"], habits.Habits.Select(habit => habit.Tag));
    }

    [Theory]
    [InlineData(3, false)]
    [InlineData(4, true)]
    public async Task Should_need_four_uses_in_a_year(int uses, bool shown)
    {
        var world = World(out _, out var svago);
        world.SpendWithTag(svago, world.Tag(svago, "Piscina"), 850, 2026, [uses]);

        var deck = await world.Deck(Period.Year(2026));

        Assert.Equal(shown, deck.Cards.OfType<RecapCard.NewHabits>().Any());
    }

    [Fact]
    public async Task Should_not_call_a_tag_new_when_the_year_before_used_it()
    {
        var world = World(out _, out var svago);
        var piscina = world.Tag(svago, "Piscina");
        world.SpendWithTag(svago, piscina, 850, 2026, [0, 0, 0, 0, 0, 0, 0, 0, 5, 5, 4, 4]);
        world.Spend(svago, 850, new DateOnly(2025, 3, 3), piscina);

        var deck = await world.Deck(Period.Year(2026));

        Assert.DoesNotContain(deck.Cards, card => card is RecapCard.NewHabits);
    }

    [Fact]
    public async Task Should_read_variable_expenses_only()
    {
        var world = World(out _, out var svago);
        var piscina = world.Tag(svago, "Piscina");
        for (var month = 1; month <= 6; month++)
            world.Spend(svago, 850, new DateOnly(2026, month, 5), piscina, automatic: true);

        var deck = await world.Deck(Period.Year(2026));

        Assert.DoesNotContain(deck.Cards, card => card is RecapCard.NewHabits);
    }

    [Fact]
    public async Task Should_stay_on_the_ground_and_leave_no_summary_row()
    {
        var world = World(out _, out var svago);
        world.SpendWithTag(svago, world.Tag(svago, "Piscina"), 850, 2026, [4]);

        var habits = (await world.Deck(Period.Year(2026))).Single<RecapCard.NewHabits>();

        Assert.True(habits.Surface.IsGround);
        Assert.Null(habits.Row);
    }

    [Fact]
    public async Task Should_compare_a_year_so_far_with_the_year_before()
    {
        var world = new RecapWorld(new DateOnly(2026, 11, 21));
        var svago = world.Category("Svago", "lilac");
        world.SpendInYear(svago, 2026, 1_000_000, 40, lastMonth: 10);
        world.SpendInYear(svago, 2025, 1_000_000, 40);
        world.SpendWithTag(svago, world.Tag(svago, "Piscina"), 850, 2026, [0, 0, 0, 0, 0, 0, 0, 0, 5]);

        var habits = (await world.Deck(Period.Year(2026))).Single<RecapCard.NewHabits>();

        Assert.Equal("Nel 2025 non c'era.", habits.Detail);
    }

    [Fact]
    public async Task Should_not_belong_to_a_month()
    {
        var world = new RecapWorld(new DateOnly(2026, 12, 1));
        var spesa = world.Category("Spesa", "mint");
        world.SpendInMonth(spesa, 2026, 11, 100_000, 12);
        world.SpendInMonth(spesa, 2026, 10, 100_000, 12);
        world.SpendWithTag(spesa, world.Tag(spesa, "Pane"), 300, 2026, [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 5]);

        var deck = await world.Deck(Period.Month(2026, 11));

        Assert.DoesNotContain(deck.Cards, card => card is RecapCard.NewHabits);
    }
}
