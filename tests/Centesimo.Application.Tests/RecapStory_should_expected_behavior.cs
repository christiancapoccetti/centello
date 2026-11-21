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
}
