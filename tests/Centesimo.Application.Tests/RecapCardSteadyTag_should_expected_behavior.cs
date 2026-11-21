using Centesimo.Domain;

namespace Centesimo.Application.Tests;

public sealed class RecapCardSteadyTag_should_expected_behavior
{
    private static readonly DateOnly NewYear = new(2026, 1, 1);

    private static void SpendWeekly(RecapWorld world, Category category, Tag tag, IEnumerable<int> weeks, long cents = 300,
        bool automatic = false)
    {
        foreach (var week in weeks)
            world.Spend(category, cents, NewYear.AddDays(7 * (week - 1) + 2), tag, automatic: automatic);
    }

    /// <summary>The tag of the year and the costliest tag at once: it leaves the steady tag to the ones under test.</summary>
    private static void Dominant(RecapWorld world, Category category, int months = 12) =>
        world.SpendWithTag(category, world.Tag(category, "Dominante"), 1_000, 2026, [.. Enumerable.Repeat(5, months)]);

    private static RecapWorld ClosedYear(out Category spesa)
    {
        var world = new RecapWorld(new DateOnly(2027, 1, 5));
        spesa = world.Category("Spesa", "mint");
        world.SpendInYear(spesa, 2026, 1_200_000, 40);
        return world;
    }

    [Fact]
    public async Task Should_name_the_tag_present_in_the_most_weeks()
    {
        var world = ClosedYear(out var spesa);
        var pane = world.Tag(spesa, "Pane");
        Dominant(world, spesa);
        SpendWeekly(world, spesa, pane, Enumerable.Range(1, 52).Except([2, 32, 33, 52]));

        var steady = (await world.Deck(Period.Year(2026))).Single<RecapCard.SteadyTag>();

        Assert.Equal("La tua abitudine più costante", steady.Lead);
        Assert.Equal("Pane", steady.Tag);
        Assert.Equal("Spesa", steady.Category.Name);
        Assert.Equal(new RecapPhrase("In ", "48 settimane su 52", ", 48 volte."), steady.Detail);
        Assert.Equal(("Gennaio", "Una barra per settimana", "Dicembre"), (steady.Start, steady.Middle, steady.End));
    }

    [Fact]
    public async Task Should_draw_a_bar_per_week_and_a_stub_for_a_week_without_a_use()
    {
        var world = ClosedYear(out var spesa);
        Dominant(world, spesa);
        SpendWeekly(world, spesa, world.Tag(spesa, "Pane"), Enumerable.Range(1, 52).Except([2, 32, 33, 52]));

        var weeks = (await world.Deck(Period.Year(2026))).Single<RecapCard.SteadyTag>().Weeks;

        Assert.Equal(52, weeks.Bars.Count);
        Assert.Equal(new RecapBar(1, RecapBarKind.Strong), weeks.Bars[0]);
        Assert.Equal(
            [2, 32, 33, 52],
            weeks.Bars.Select((bar, index) => (bar, week: index + 1)).Where(pair => pair.bar.Kind == RecapBarKind.Stub).Select(pair => pair.week));
        Assert.Equal("Pane in 48 settimane su 52", weeks.Description);
    }

    [Fact]
    public async Task Should_skip_the_tag_of_the_year_and_the_costliest_tag()
    {
        var world = ClosedYear(out var spesa);
        var ristoranti = world.Category("Ristoranti", "butter");
        var casa = world.Category("Casa", "peri");
        var caffe = world.Tag(spesa, "Caffè");
        SpendWeekly(world, spesa, caffe, Enumerable.Range(1, 52), cents: 130);
        SpendWeekly(world, spesa, caffe, Enumerable.Range(1, 52), cents: 130);
        var cene = world.Tag(ristoranti, "Cene");
        SpendWeekly(world, ristoranti, cene, Enumerable.Range(1, 40), cents: 9_000);
        var pane = world.Tag(casa, "Pane");
        SpendWeekly(world, casa, pane, Enumerable.Range(1, 30));

        var deck = await world.Deck(Period.Year(2026));

        Assert.Equal("Caffè", deck.Single<RecapCard.TagOfYear>().Tag);
        Assert.Equal("Cene", deck.Single<RecapCard.CostliestTag>().Tag);
        Assert.Equal("Pane", deck.Single<RecapCard.SteadyTag>().Tag);
    }

    [Fact]
    public async Task Should_skip_only_the_tag_of_the_year_when_the_costliest_tag_is_the_same()
    {
        var world = ClosedYear(out var spesa);
        var casa = world.Category("Casa", "peri");
        var caffe = world.Tag(spesa, "Caffè");
        SpendWeekly(world, spesa, caffe, Enumerable.Range(1, 52), cents: 900);
        SpendWeekly(world, spesa, caffe, Enumerable.Range(1, 52), cents: 900);
        SpendWeekly(world, casa, world.Tag(casa, "Pane"), Enumerable.Range(1, 30));

        var deck = await world.Deck(Period.Year(2026));

        Assert.DoesNotContain(deck.Cards, card => card is RecapCard.CostliestTag);
        Assert.Equal("Pane", deck.Single<RecapCard.SteadyTag>().Tag);
    }

    [Theory]
    [InlineData(25, false)]
    [InlineData(26, true)]
    public async Task Should_need_half_the_weeks_of_a_closed_year(int weeks, bool shown)
    {
        var world = ClosedYear(out var spesa);
        Dominant(world, spesa);
        SpendWeekly(world, spesa, world.Tag(spesa, "Pane"), Enumerable.Range(1, weeks));

        var deck = await world.Deck(Period.Year(2026));

        Assert.Equal(shown, deck.Cards.OfType<RecapCard.SteadyTag>().Any());
    }

    [Theory]
    [InlineData(9, false)]
    [InlineData(10, true)]
    public async Task Should_need_ten_weeks_even_in_a_short_year_so_far(int weeks, bool shown)
    {
        var world = new RecapWorld(new DateOnly(2026, 3, 11));
        var spesa = world.Category("Spesa", "mint");
        world.SpendInYear(spesa, 2026, 600_000, 40, lastMonth: 3);
        Dominant(world, spesa);
        SpendWeekly(world, spesa, world.Tag(spesa, "Pane"), Enumerable.Range(1, weeks));

        var deck = await world.Deck(Period.Year(2026));

        Assert.Equal(shown, deck.Cards.OfType<RecapCard.SteadyTag>().Any());
    }

    [Fact]
    public async Task Should_count_the_weeks_so_far_and_end_on_the_month_of_the_last_day()
    {
        var world = new RecapWorld(new DateOnly(2026, 11, 21));
        var spesa = world.Category("Spesa", "mint");
        world.SpendInYear(spesa, 2026, 1_000_000, 40, lastMonth: 10);
        Dominant(world, spesa);
        SpendWeekly(world, spesa, world.Tag(spesa, "Pane"), Enumerable.Range(1, 40));

        var steady = (await world.Deck(Period.Year(2026))).Single<RecapCard.SteadyTag>();

        // 20 November is the 324th day: week 47 of 47 counted.
        Assert.Equal("40 settimane su 47", steady.Detail.Emphasis);
        Assert.Equal(47, steady.Weeks.Bars.Count);
        Assert.Equal("Novembre", steady.End);
    }

    [Fact]
    public async Task Should_give_the_last_week_the_remaining_days_of_the_year()
    {
        var world = ClosedYear(out var spesa);
        var pane = world.Tag(spesa, "Pane");
        Dominant(world, spesa);
        SpendWeekly(world, spesa, pane, Enumerable.Range(1, 30));
        world.Spend(spesa, 300, new DateOnly(2026, 12, 31), pane);

        var steady = (await world.Deck(Period.Year(2026))).Single<RecapCard.SteadyTag>();

        Assert.Equal("31 settimane su 52", steady.Detail.Emphasis);
        Assert.Equal(RecapBarKind.Strong, steady.Weeks.Bars[51].Kind);
    }

    [Fact]
    public async Task Should_read_variable_expenses_only()
    {
        var world = ClosedYear(out var spesa);
        SpendWeekly(world, spesa, world.Tag(spesa, "Abbonamento"), Enumerable.Range(1, 52), automatic: true);

        var deck = await world.Deck(Period.Year(2026));

        Assert.DoesNotContain(deck.Cards, card => card is RecapCard.SteadyTag);
    }

    [Fact]
    public async Task Should_keep_a_coral_category_on_the_ground_and_draw_its_bars_in_its_own_ink()
    {
        var world = ClosedYear(out _);
        var bar = world.Category("Bar", "coral");
        Dominant(world, bar);
        SpendWeekly(world, bar, world.Tag(bar, "Caffè"), Enumerable.Range(1, 40));

        var steady = (await world.Deck(Period.Year(2026))).Single<RecapCard.SteadyTag>();

        Assert.True(steady.Surface.IsGround);
        Assert.Equal(new RecapBar(1, RecapBarKind.Palette, ColorKey: "coral"), steady.Weeks.Bars[0]);
    }

    [Fact]
    public async Task Should_leave_a_summary_row_with_the_tag()
    {
        var world = ClosedYear(out var spesa);
        Dominant(world, spesa);
        SpendWeekly(world, spesa, world.Tag(spesa, "Pane"), Enumerable.Range(1, 40));

        var steady = (await world.Deck(Period.Year(2026))).Single<RecapCard.SteadyTag>();

        Assert.Equal(new RecapSummaryRow(MaterialSymbols.Repeat, "La più costante", "Pane"), steady.Row);
    }

    [Fact]
    public async Task Should_not_belong_to_a_month()
    {
        var world = new RecapWorld(new DateOnly(2026, 12, 1));
        var spesa = world.Category("Spesa", "mint");
        world.SpendInMonth(spesa, 2026, 11, 100_000, 12);
        Dominant(world, spesa);
        SpendWeekly(world, spesa, world.Tag(spesa, "Pane"), Enumerable.Range(1, 40));

        var deck = await world.Deck(Period.Month(2026, 11));

        Assert.DoesNotContain(deck.Cards, card => card is RecapCard.SteadyTag);
    }
}
