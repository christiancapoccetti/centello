namespace Centello.Application;

/// <summary>What the quick-entry panel is doing. Listening and Preparing carry how much was heard, for the 20 s line.</summary>
public abstract record PanelState
{
    private PanelState()
    {
    }

    /// <summary>The panel stays open when the activity stops, because the camera app is in front of it.</summary>
    public bool KeepsPanelWhenStopped => this is TakingPhoto;

    public sealed record Starting : PanelState;
    public sealed record NeedsConsent : PanelState;
    public sealed record Listening(TimeSpan Heard) : PanelState;
    public sealed record Preparing(TimeSpan Heard) : PanelState;
    public sealed record TakingPhoto : PanelState;
    public sealed record Reading : PanelState;
    public sealed record Failed(Error Error) : PanelState;
    /// <summary>The panel hands the draft to the app, opens the editor and closes.</summary>
    public sealed record DraftReady(ExpenseDraft Draft) : PanelState;
    /// <summary>Nothing was sent, or what came back was dropped.</summary>
    public sealed record Closed : PanelState;
}

/// <summary>One capture of the quick-entry panel, from opening to a draft or to nothing. Every call comes from the
/// panel's main thread and every await returns to it, so State has one writer.</summary>
public interface IPanelCapture
{
    DraftSource Source { get; }
    PanelState State { get; }
    event EventHandler<PanelState>? Changed;

    Task Begin();

    /// <summary>The panel was rebuilt by Android: the capture it had is gone.</summary>
    Task Restore();

    Task Retry();

    Task Cancel();
}
