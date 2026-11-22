using System.Diagnostics;
using Centello.Application;

namespace Centello.App.Controls.Recap;

/// <summary>Lets the time pass on a <see cref="RecapStory"/> while nothing stops it: the page is in front, the window
/// is active, no finger is held down and no screen reader is on. The story owns the time spent; this only feeds it.</summary>
public sealed class StoryPlayer
{
    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(33);
    // A long gap, such as a stalled frame, must not eat a card.
    private static readonly TimeSpan LongestStep = TimeSpan.FromMilliseconds(250);

    private readonly IDispatcherTimer _timer;
    private readonly IScreenReader _screenReader;
    private RecapStory? _story;
    private bool _isVisible;
    private bool _isActive = true;
    private bool _isHeld;
    private long _lastTick;

    public StoryPlayer(IDispatcherTimer timer, IScreenReader screenReader)
    {
        _timer = timer;
        _screenReader = screenReader;
        _timer.Interval = Interval;
        _timer.Tick += (_, _) => Step();
    }

    /// <summary>The card in front changed by itself.</summary>
    public event Action? CardChanged;

    /// <summary>The card in front has more of its time behind it, from 0 to 1.</summary>
    public event Action<double>? Progressed;

    /// <summary>False with a screen reader on: the cards then change only by tap.</summary>
    public bool IsAutomatic => !_screenReader.IsOn;

    public bool IsVisible { set => Set(ref _isVisible, value); }
    public bool IsActive { set => Set(ref _isActive, value); }
    public bool IsHeld { set => Set(ref _isHeld, value); }

    public void Play(RecapStory story)
    {
        _story = story;
        Refresh();
    }

    /// <summary>How full the segment of the card in front is: with a screen reader nothing counts, so it is full.</summary>
    public double Fill => IsAutomatic && _story is { } story ? story.Progress : 1;

    private void Set(ref bool field, bool value)
    {
        field = value;
        Refresh();
    }

    /// <summary>Starts or stops the clock to match the state: call it after the story moved without the clock.</summary>
    public void Refresh()
    {
        var shouldRun = _story is { IsDone: false } && _isVisible && _isActive && !_isHeld && IsAutomatic;
        if (shouldRun == _timer.IsRunning)
            return;

        if (!shouldRun)
        {
            _timer.Stop();
            return;
        }

        _lastTick = Stopwatch.GetTimestamp();
        _timer.Start();
    }

    private void Step()
    {
        if (_story is not { } story)
            return;

        var now = Stopwatch.GetTimestamp();
        var step = Stopwatch.GetElapsedTime(_lastTick, now);
        _lastTick = now;
        if (story.Advance(step > LongestStep ? LongestStep : step))
            CardChanged?.Invoke();
        else
            Progressed?.Invoke(story.Progress);

        if (story.IsDone)
            Refresh();
    }
}
