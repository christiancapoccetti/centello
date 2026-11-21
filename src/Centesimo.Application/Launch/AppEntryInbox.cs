namespace Centesimo.Application;

public sealed record WaitingEntry(AppEntry Entry, ExpenseDraft? Draft);

public sealed class AppEntryInbox
{
    private WaitingEntry? _waiting;

    public bool HasWaiting => _waiting is not null;

    public void Receive(AppEntry entry)
    {
        if (entry is AppEntry.VoiceDraft && _waiting is { Entry: AppEntry.VoiceDraft })
            return;

        _waiting = new WaitingEntry(entry, null);
    }

    public void Receive(ExpenseDraft draft) => _waiting = new WaitingEntry(new AppEntry.VoiceDraft(), draft);

    public WaitingEntry? Take()
    {
        var waiting = _waiting;
        _waiting = null;
        return waiting;
    }
}
