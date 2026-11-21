namespace Centesimo.Application.Tests;

public sealed class RecapStory_should_expected_behavior
{
    private static readonly RecapBars NoBars = new([], [], "");

    private static RecapStory StoryOf(int middle)
    {
        var span = RecapSpan.Of(Period.Month(2026, 10), new DateOnly(2026, 11, 21))!;
        var cover = new RecapCard.Cover("Il tuo\nottobre", "", NoBars, "");
        var summary = new RecapCard.Summary("Ottobre in breve", [], new RecapExit("", RecapExitTarget.MonthExpenses), "");
        var cards = Enumerable.Range(0, middle)
            .Select(_ => (RecapCard)new RecapCard.Months("", "", NoBars))
            .ToList();
        return new RecapStory(new RecapDeck(span, cover, cards, summary));
    }

    [Fact]
    public void Should_start_on_the_cover()
    {
        var story = StoryOf(2);

        Assert.Equal(0, story.Index);
        Assert.Equal(4, story.Count);
        Assert.IsType<RecapCard.Cover>(story.Current);
    }

    [Fact]
    public void Should_move_forward_to_the_summary()
    {
        var story = StoryOf(1);

        Assert.True(story.Next());
        Assert.IsType<RecapCard.Months>(story.Current);
        Assert.True(story.Next());
        Assert.IsType<RecapCard.Summary>(story.Current);
        Assert.Equal(2, story.Index);
    }

    [Fact]
    public void Should_stay_on_the_summary_when_asked_for_more()
    {
        var story = StoryOf(0);
        story.Next();

        Assert.False(story.Next());
        Assert.Equal(1, story.Index);
    }

    [Fact]
    public void Should_move_back()
    {
        var story = StoryOf(1);
        story.Next();
        story.Next();

        Assert.True(story.Previous());
        Assert.Equal(1, story.Index);
    }

    [Fact]
    public void Should_stay_on_the_cover_when_asked_for_less()
    {
        var story = StoryOf(1);

        Assert.False(story.Previous());
        Assert.Equal(0, story.Index);
    }

    [Fact]
    public void Should_restart_from_the_cover()
    {
        var story = StoryOf(2);
        story.Next();
        story.Next();
        story.Next();

        story.Restart();

        Assert.Equal(0, story.Index);
        Assert.IsType<RecapCard.Cover>(story.Current);
    }

    [Fact]
    public void Should_hold_the_deck_it_was_given()
    {
        var story = StoryOf(0);

        Assert.Equal("Il tuo ottobre", story.Deck.Title);
        Assert.Equal("2026", story.Deck.Badge);
    }

    private static TimeSpan Seconds(double value) => TimeSpan.FromSeconds(value);

    [Fact]
    public void Should_give_each_card_seven_seconds() =>
        Assert.Equal(Seconds(7), RecapStory.CardDuration);

    [Fact]
    public void Should_fill_the_card_with_the_time_spent_on_it()
    {
        var story = StoryOf(1);

        Assert.Equal(0, story.Progress);
        Assert.False(story.Advance(Seconds(3.5)));
        Assert.Equal(0.5, story.Progress, 10);
        Assert.Equal(Seconds(3.5), story.Elapsed);
    }

    [Fact]
    public void Should_go_on_when_the_card_has_had_its_time()
    {
        var story = StoryOf(1);

        Assert.True(story.Advance(Seconds(7)));

        Assert.Equal(1, story.Index);
        Assert.Equal(0, story.Progress);
    }

    [Fact]
    public void Should_go_on_one_card_even_when_the_time_overshoots()
    {
        var story = StoryOf(2);

        Assert.True(story.Advance(Seconds(30)));

        Assert.Equal(1, story.Index);
        Assert.Equal(TimeSpan.Zero, story.Elapsed);
    }

    [Fact]
    public void Should_fill_the_last_card_and_stay_on_it()
    {
        var story = StoryOf(0);
        story.Next();

        Assert.False(story.Advance(Seconds(10)));

        Assert.Equal(1, story.Index);
        Assert.Equal(1.0, story.Progress);
        Assert.True(story.IsDone);
        Assert.False(story.Advance(Seconds(1)));
        Assert.Equal(1.0, story.Progress);
    }

    [Fact]
    public void Should_not_be_done_before_the_last_card_is_full()
    {
        var story = StoryOf(0);

        story.Advance(Seconds(7));
        Assert.False(story.IsDone);

        story.Advance(Seconds(6.9));
        Assert.False(story.IsDone);
    }

    [Fact]
    public void Should_start_the_time_over_on_the_card_a_tap_lands_on()
    {
        var story = StoryOf(2);
        story.Advance(Seconds(5));

        Assert.True(story.Next());
        Assert.Equal(TimeSpan.Zero, story.Elapsed);

        story.Advance(Seconds(5));
        Assert.True(story.Previous());
        Assert.Equal(TimeSpan.Zero, story.Elapsed);
    }

    [Fact]
    public void Should_start_the_cover_over_when_asked_for_less()
    {
        var story = StoryOf(1);
        story.Advance(Seconds(5));

        Assert.False(story.Previous());

        Assert.Equal(TimeSpan.Zero, story.Elapsed);
    }

    [Fact]
    public void Should_keep_the_summary_full_when_asked_for_more()
    {
        var story = StoryOf(0);
        story.Next();
        story.Advance(Seconds(10));

        Assert.False(story.Next());

        Assert.True(story.IsDone);
    }

    [Fact]
    public void Should_restart_with_the_time_running_from_zero()
    {
        var story = StoryOf(1);
        story.Next();
        story.Next();
        story.Advance(Seconds(10));

        story.Restart();

        Assert.Equal(0, story.Index);
        Assert.Equal(TimeSpan.Zero, story.Elapsed);
        Assert.False(story.IsDone);
    }
}
