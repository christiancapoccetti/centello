using Centello.Domain;

namespace Centello.Application.Tests;

public sealed class RecapCardNewHabit_should_expected_behavior
{
    private const string Nbsp = " ";

    private static RecapWorld WorldWithNovember(out Category spesa, out Category svago)
    {
        var world = new RecapWorld(new DateOnly(2026, 12, 1));
        spesa = world.Category("Spesa", "mint");
        svago = world.Category("Svago", "lilac");
        world.SpendInMonth(spesa, 2026, 11, 100_000, 12);
        world.SpendInMonth(spesa, 2026, 10, 100_000, 12);
        return world;
    }

    private static void SpendOn(RecapWorld world, Category category, Tag tag, long cents, params int[] days)
    {
        foreach (var day in days)
            world.Spend(category, cents, new DateOnly(2026, 11, day), tag);
    }

    [Fact]
    public async Task Should_name_a_tag_used_this_month_and_not_in_the_previous_one()
    {
        var world = WorldWithNovember(out _, out var svago);
        var piscina = world.Tag(svago, "Piscina");
        SpendOn(world, svago, piscina, 850, 4, 11, 18, 22, 25);

        var habit = (await world.Deck(Period.Month(2026, 11))).Single<RecapCard.NewHabit>();

        Assert.Equal("Una nuova abitudine", habit.Lead);
        Assert.Equal("Piscina", habit.Tag);
        Assert.Equal("Svago", habit.Category.Name);
        Assert.Equal(new RecapPhrase("", "5 volte", $", 42,50{Nbsp}€ in tutto, nella categoria Svago. A ottobre nessuna."), habit.Detail);
        Assert.Equal(("1 nov", "30 nov"), (habit.From, habit.To));
    }

    [Fact]
    public async Task Should_mark_the_days_of_use_and_describe_them()
    {
        var world = WorldWithNovember(out _, out var svago);
        SpendOn(world, svago, world.Tag(svago, "Piscina"), 850, 4, 11, 18, 22, 25);

        var days = (await world.Deck(Period.Month(2026, 11))).Single<RecapCard.NewHabit>().Days;

        Assert.Equal(30, days.Bars.Count);
        Assert.Equal(
            [4, 11, 18, 22, 25],
            days.Bars.Select((bar, index) => (bar, day: index + 1)).Where(pair => pair.bar.Kind == RecapBarKind.Strong).Select(pair => pair.day));
        Assert.All(days.Bars.Where(bar => bar.Kind == RecapBarKind.Strong), bar => Assert.Equal(1.0, bar.Height));
        Assert.Equal(25, days.Bars.Count(bar => bar.Kind == RecapBarKind.Stub));
        Assert.Equal("Piscina il 4, l'11, il 18, il 22 e il 25 novembre", days.Description);
    }

    [Theory]
    [InlineData(2, false)]
    [InlineData(3, true)]
    public async Task Should_need_three_uses_in_a_month(int uses, bool shown)
    {
        var world = WorldWithNovember(out _, out var svago);
        SpendOn(world, svago, world.Tag(svago, "Piscina"), 850, Enumerable.Range(3, uses).ToArray());

        var deck = await world.Deck(Period.Month(2026, 11));

        Assert.Equal(shown, deck.Cards.OfType<RecapCard.NewHabit>().Any());
    }

    [Fact]
    public async Task Should_not_call_a_tag_new_when_it_was_used_in_the_previous_month()
    {
        var world = WorldWithNovember(out _, out var svago);
        var piscina = world.Tag(svago, "Piscina");
        SpendOn(world, svago, piscina, 850, 4, 11, 18, 22, 25);
        world.Spend(svago, 850, new DateOnly(2026, 10, 30), piscina);

        var deck = await world.Deck(Period.Month(2026, 11));

        Assert.DoesNotContain(deck.Cards, card => card is RecapCard.NewHabit);
    }

    [Fact]
    public async Task Should_read_variable_expenses_only()
    {
        var world = WorldWithNovember(out _, out var svago);
        var piscina = world.Tag(svago, "Piscina");
        SpendOn(world, svago, piscina, 850, 4, 11);
        world.Spend(svago, 850, new DateOnly(2026, 11, 18), piscina, automatic: true);
        world.Spend(svago, 850, new DateOnly(2026, 10, 18), piscina, automatic: true);

        var deck = await world.Deck(Period.Month(2026, 11));

        Assert.DoesNotContain(deck.Cards, card => card is RecapCard.NewHabit);
    }

    [Fact]
    public async Task Should_still_call_a_tag_new_when_only_an_automatic_expense_carried_it_before()
    {
        var world = WorldWithNovember(out _, out var svago);
        var piscina = world.Tag(svago, "Piscina");
        SpendOn(world, svago, piscina, 850, 4, 11, 18);
        world.Spend(svago, 850, new DateOnly(2026, 10, 18), piscina, automatic: true);

        var deck = await world.Deck(Period.Month(2026, 11));

        Assert.Equal("Piscina", deck.Single<RecapCard.NewHabit>().Tag);
    }

    [Fact]
    public async Task Should_show_only_the_most_used_new_tag()
    {
        var world = WorldWithNovember(out _, out var svago);
        SpendOn(world, svago, world.Tag(svago, "Piscina"), 850, 4, 11, 18);
        SpendOn(world, svago, world.Tag(svago, "Libri"), 1_500, 5, 12, 19, 26);

        var habit = (await world.Deck(Period.Month(2026, 11))).Single<RecapCard.NewHabit>();

        Assert.Equal("Libri", habit.Tag);
    }

    [Fact]
    public async Task Should_keep_a_coral_category_on_the_ground_and_in_its_own_ink()
    {
        var world = WorldWithNovember(out _, out _);
        var bar = world.Category("Bar", "coral");
        SpendOn(world, bar, world.Tag(bar, "Caffè"), 130, 4, 11, 18);

        var habit = (await world.Deck(Period.Month(2026, 11))).Single<RecapCard.NewHabit>();

        Assert.True(habit.Surface.IsGround);
        var used = habit.Days.Bars.Where(day => day.Kind != RecapBarKind.Stub).ToList();
        Assert.All(used, day => Assert.Equal(new RecapBar(1, RecapBarKind.Palette, ColorKey: "coral"), day));
    }

    [Fact]
    public async Task Should_paint_the_card_with_the_category_and_draw_the_bars_in_tile_ink()
    {
        var world = WorldWithNovember(out _, out var svago);
        SpendOn(world, svago, world.Tag(svago, "Piscina"), 850, 4, 11, 18);

        var habit = (await world.Deck(Period.Month(2026, 11))).Single<RecapCard.NewHabit>();

        Assert.Equal("lilac", habit.Surface.PaletteKey);
        Assert.Contains(habit.Days.Bars, bar => bar.Kind == RecapBarKind.Strong);
    }

    [Fact]
    public async Task Should_elide_the_preposition_before_a_month_starting_with_a()
    {
        var world = new RecapWorld(new DateOnly(2026, 5, 1));
        var spesa = world.Category("Spesa", "mint");
        world.SpendInMonth(spesa, 2026, 4, 100_000, 12);
        world.SpendInMonth(spesa, 2026, 3, 100_000, 12);
        var tag = world.Tag(spesa, "Pane");
        foreach (var day in new[] { 3, 10, 17 })
            world.Spend(spesa, 300, new DateOnly(2026, 4, day), tag);

        var habit = (await world.Deck(Period.Month(2026, 4))).Single<RecapCard.NewHabit>();

        Assert.EndsWith("A marzo nessuna.", habit.Detail.Tail);
    }

    [Fact]
    public async Task Should_leave_a_summary_row_with_the_tag()
    {
        var world = WorldWithNovember(out _, out var svago);
        SpendOn(world, svago, world.Tag(svago, "Piscina"), 850, 4, 11, 18);

        var habit = (await world.Deck(Period.Month(2026, 11))).Single<RecapCard.NewHabit>();

        Assert.Equal(new RecapSummaryRow(MaterialSymbols.FiberNew, "Nuova abitudine", "Piscina"), habit.Row);
    }
}
