namespace Centesimo.Application;

/// <summary>No amount, ever: the notification is read on the lock screen. Entry is what a tap opens.</summary>
public sealed record RecapNotice(string Key, Period Period, string Title, string Text)
{
    public AppEntry.Screen Entry => new AppEntry.Screen.Recap(Period);
}
