namespace Centello.Application;

/// <summary>What the platform does for the recap notification: show it and keep exactly one alarm.</summary>
public interface IRecapNoticeBoard
{
    /// <summary>The system will show a notification: the permission is granted and the channel is not blocked.</summary>
    bool CanPost { get; }

    /// <summary>False when the notification could not be shown.</summary>
    Task<bool> Post(RecapNotice notice);

    /// <summary>Exactly one alarm: arming replaces the previous one.</summary>
    Task ArmAt(DateTime local);

    Task Disarm();
}

public sealed class NoOpRecapNoticeBoard : IRecapNoticeBoard
{
    public bool CanPost => false;

    public Task<bool> Post(RecapNotice notice) => Task.FromResult(false);

    public Task ArmAt(DateTime local) => Task.CompletedTask;

    public Task Disarm() => Task.CompletedTask;
}
