using Centello.Domain;

namespace Centello.Application.Tests;

public sealed class RecapCardWeekday_should_expected_behavior
{
    // Mon..Sun, as a count of expenses: 100 in all.
    private static readonly int[] Mockup = [11, 12, 12, 13, 16, 24, 12];

    private static async Task<RecapDeck> Year(int[] perWeekday, int automaticSaturdays = 0)
    {
        var world = new RecapWorld(new DateOnly(2027, 1, 5));
        var spesa = world.Category("Spesa", "mint");
        var firstMonday = new DateOnly(2026, 1, 5);
        for (var weekday = 0; weekday < 7; weekday++)
            for (var week = 0; week < perWeekday[weekday]; week++)
                world.Spend(spesa, 1_000, firstMonday.AddDays(weekday + 7 * week));

        for (var week = 0; week < automaticSaturdays; week++)
            world.Spend(spesa, 1_000, firstMonday.AddDays(5 + 7 * week), automatic: true);

        return await world.Deck(Period.Year(2026));
    }

    [Fact]
    public async Task Should_name_the_weekday_with_the_most_variable_expenses_by_count()
    {
        var weekday = (await Year(Mockup)).Single<RecapCard.Weekday>();

        Assert.Equal("Il sabato è il tuo giorno", weekday.Headline);
        Assert.Equal(new RecapPhrase("Il ", "24%", " delle spese variabili cade di sabato."), weekday.Detail);
    }

    [Fact]
    public async Task Should_put_the_article_la_before_sunday()
    {
        var weekday = (await Year([11, 12, 12, 13, 16, 12, 24])).Single<RecapCard.Weekday>();

        Assert.Equal("La domenica è il tuo giorno", weekday.Headline);
        Assert.Equal(" delle spese variabili cade di domenica.", weekday.Detail.Tail);
    }

    [Fact]
    public async Task Should_draw_the_share_of_each_weekday_starting_on_monday()
    {
        var days = (await Year(Mockup)).Single<RecapCard.Weekday>().Days;

        Assert.Equal(["Lun", "Mar", "Mer", "Gio", "Ven", "Sab", "Dom"], days.Bars.Select(bar => bar.Label));
        Assert.Equal(["11%", "12%", "12%", "13%", "16%", "24%", "12%"], days.Bars.Select(bar => bar.Value));
        Assert.Equal(RecapBarKind.Strong, days.Bars[5].Kind);
        Assert.Equal(1.0, days.Bars[5].Height);
        Assert.Equal(11 / 24.0, days.Bars[0].Height);
        Assert.Equal(RecapBarKind.Bar, days.Bars[0].Kind);
        Assert.Equal("Quota delle spese variabili per giorno della settimana: sabato 24%", days.Description);
    }

    [Fact]
    public async Task Should_not_count_automatic_expenses()
    {
        var weekday = (await Year(Mockup, automaticSaturdays: 40)).Single<RecapCard.Weekday>();

        Assert.Equal("24%", weekday.Detail.Emphasis);
    }

    [Theory]
    [InlineData(99, false)]
    [InlineData(100, true)]
    public async Task Should_need_a_hundred_variable_expenses(int total, bool shown)
    {
        var deck = await Year([10, 10, 10, 10, 10, total - 50, 0]);

        Assert.Equal(shown, deck.Cards.OfType<RecapCard.Weekday>().Any());
    }

    [Theory]
    [InlineData(17, false)]
    [InlineData(18, true)]
    public async Task Should_need_the_top_weekday_to_hold_eighteen_percent(int top, bool shown)
    {
        int[] week = top == 17 ? [17, 17, 17, 17, 16, 8, 8] : [18, 17, 17, 16, 16, 8, 8];

        var deck = await Year(week);

        Assert.Equal(shown, deck.Cards.OfType<RecapCard.Weekday>().Any());
    }

    [Fact]
    public async Task Should_pick_the_first_weekday_on_a_tie()
    {
        var weekday = (await Year([20, 20, 15, 15, 15, 15, 0])).Single<RecapCard.Weekday>();

        Assert.Equal("Il lunedì è il tuo giorno", weekday.Headline);
    }

    [Fact]
    public async Task Should_leave_a_summary_row_with_the_weekday()
    {
        var weekday = (await Year(Mockup)).Single<RecapCard.Weekday>();

        Assert.Equal(new RecapSummaryRow(MaterialSymbols.Weekend, "Il tuo giorno", "Sabato"), weekday.Row);
    }

    [Fact]
    public async Task Should_not_belong_to_a_month()
    {
        var world = new RecapWorld(new DateOnly(2026, 12, 1));
        world.SpendInMonth(world.Category("Spesa", "mint"), 2026, 11, 100_000, 12);

        var deck = await world.Deck(Period.Month(2026, 11));

        Assert.DoesNotContain(deck.Cards, card => card is RecapCard.Weekday);
    }
}
