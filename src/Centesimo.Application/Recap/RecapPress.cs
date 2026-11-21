namespace Centesimo.Application;

/// <summary>Tells a tap from a hold on the story: a press that lasts <see cref="HoldAfter"/> or more pauses the story
/// and must not turn the card when the finger lifts.</summary>
public sealed class RecapPress
{
    public static readonly TimeSpan HoldAfter = TimeSpan.FromMilliseconds(300);

    private TimeSpan? _downAt;

    /// <summary>True while a finger is down.</summary>
    public bool IsDown => _downAt.HasValue;

    /// <summary>True when the last press that ended was a hold.</summary>
    public bool WasHold { get; private set; }

    public void Down(TimeSpan now) => _downAt = now;

    public void Up(TimeSpan now)
    {
        if (_downAt is not { } downAt)
            return;

        WasHold = now - downAt >= HoldAfter;
        _downAt = null;
    }
}
