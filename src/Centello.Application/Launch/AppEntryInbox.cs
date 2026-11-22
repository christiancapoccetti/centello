namespace Centello.Application;

public sealed record WaitingEntry(AppEntry.Screen Entry, ExpenseDraft? Draft);

public sealed class AppEntryInbox
{
    private WaitingEntry? _waiting;

    public bool HasWaiting => _waiting is not null;

    public void Receive(AppEntry.Screen entry)
    {
        if (entry is AppEntry.Screen.Draft && _waiting is { Entry: AppEntry.Screen.Draft })
            return;

        _waiting = new WaitingEntry(entry, null);
    }

    public void Receive(ExpenseDraft draft) => _waiting = new WaitingEntry(new AppEntry.Screen.Draft(), draft);

    public WaitingEntry? Take()
    {
        var waiting = _waiting;
        _waiting = null;
        return waiting;
    }
}
