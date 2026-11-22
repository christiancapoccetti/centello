namespace Centello.Application.Tests;

public sealed class AppEntryInbox_should_expected_behavior
{
    private static readonly ExpenseDraft Draft = new(10_000, null, null, null, "", DraftSource.Voice, "cento euro");

    private readonly AppEntryInbox _inbox = new();

    [Fact]
    public void Wait_for_nothing_at_first()
    {
        Assert.False(_inbox.HasWaiting);
        Assert.Null(_inbox.Take());
    }

    [Fact]
    public void Hand_over_an_entry_once()
    {
        _inbox.Receive(new AppEntry.Screen.Settings());

        Assert.True(_inbox.HasWaiting);
        Assert.Equal(new WaitingEntry(new AppEntry.Screen.Settings(), null), _inbox.Take());
        Assert.False(_inbox.HasWaiting);
        Assert.Null(_inbox.Take());
    }

    [Fact]
    public void Keep_a_draft_together_with_the_voice_entry()
    {
        _inbox.Receive(Draft);

        Assert.Equal(new WaitingEntry(new AppEntry.Screen.Draft(), Draft), _inbox.Take());
    }

    [Fact]
    public void Keep_the_draft_when_the_voice_link_that_opens_the_app_follows_it()
    {
        _inbox.Receive(Draft);

        _inbox.Receive(new AppEntry.Screen.Draft());

        Assert.Equal(new WaitingEntry(new AppEntry.Screen.Draft(), Draft), _inbox.Take());
    }

    [Fact]
    public void Open_an_empty_voice_entry_when_no_draft_was_handed_over()
    {
        _inbox.Receive(new AppEntry.Screen.Draft());

        Assert.Equal(new WaitingEntry(new AppEntry.Screen.Draft(), null), _inbox.Take());
    }

    [Fact]
    public void Replace_a_waiting_entry_and_its_draft_with_a_newer_entry()
    {
        _inbox.Receive(Draft);

        _inbox.Receive(new AppEntry.Screen.NewExpense(null));

        Assert.Equal(new WaitingEntry(new AppEntry.Screen.NewExpense(null), null), _inbox.Take());
    }

    [Fact]
    public void Replace_a_waiting_draft_with_a_newer_one()
    {
        var newer = Draft with { AmountCents = 2_500 };
        _inbox.Receive(Draft);

        _inbox.Receive(newer);

        Assert.Same(newer, _inbox.Take()!.Draft);
    }

    [Fact]
    public void Not_give_a_taken_draft_to_a_later_voice_link()
    {
        _inbox.Receive(Draft);
        _inbox.Take();

        _inbox.Receive(new AppEntry.Screen.Draft());

        Assert.Null(_inbox.Take()!.Draft);
    }
}
