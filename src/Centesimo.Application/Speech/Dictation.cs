namespace Centesimo.Application;

/// <summary>What the voice panel shows. Listening and Preparing carry how much was heard, for the 20 s line.</summary>
public abstract record DictationState
{
    private DictationState()
    {
    }

    public sealed record Starting : DictationState;
    public sealed record NeedsApiKey : DictationState;
    public sealed record Listening(TimeSpan Heard) : DictationState;
    public sealed record Preparing(TimeSpan Heard) : DictationState;
    public sealed record Failed(Error Error) : DictationState;
    /// <summary>The draft waits in IPendingExpenseSpeechDraft: the panel opens the editor and closes.</summary>
    public sealed record DraftReady : DictationState;
    /// <summary>Nothing was sent, or what came back was dropped.</summary>
    public sealed record Closed : DictationState;
}

/// <summary>One dictation of the voice panel, from opening to a draft or to nothing. Every call comes from the panel's
/// main thread and every await returns to it, so State has one writer. A call that does not fit the state is ignored:
/// a second tap while preparing does nothing, and Cancel after DraftReady keeps the draft for the editor.</summary>
public sealed class Dictation(
    SpeechExpenseDraftService speech,
    IMicrophoneAccess microphone,
    IPendingExpenseSpeechDraft drafts,
    TimeProvider clock)
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(SpeechRecordingLimits.MaximumDurationSeconds);
    private CancellationTokenSource? _preparing;
    private long _listeningSince;

    public DictationState State { get; private set; } = new DictationState.Starting();
    public event EventHandler<DictationState>? Changed;

    /// <summary>Starting: no key -> NeedsApiKey; microphone refused -> Failed(MicrophoneDenied); otherwise records at
    /// once -> Listening(0), or Failed when the network or the recorder says no.</summary>
    public async Task Begin()
    {
        if (!await speech.IsAvailable())
        {
            Move(new DictationState.NeedsApiKey());
            return;
        }

        var granted = await microphone.Ensure();
        if (State is not DictationState.Starting)
            return;

        if (!granted)
        {
            Move(new DictationState.Failed(SpeechErrors.MicrophoneDenied));
            return;
        }

        var started = await speech.Start();
        if (State is not DictationState.Starting)
        {
            await speech.Cancel();
            return;
        }

        if (started.IsFailure)
        {
            Move(new DictationState.Failed(started.Error));
            return;
        }

        _listeningSince = clock.GetTimestamp();
        Move(new DictationState.Listening(TimeSpan.Zero));
    }

    /// <summary>From the panel's timer while listening: updates Heard and sends at the limit.</summary>
    public async Task Tick()
    {
        if (State is not DictationState.Listening)
            return;

        var heard = clock.GetElapsedTime(_listeningSince);
        if (heard >= Limit)
        {
            await Send();
            return;
        }

        Move(new DictationState.Listening(heard));
    }

    /// <summary>Listening -> Preparing -> DraftReady or Failed. A full recording is sent (WhenFull.Send).</summary>
    public async Task Send()
    {
        if (State is not DictationState.Listening)
            return;

        Move(new DictationState.Preparing(TimeSpan.FromTicks(Math.Min(clock.GetElapsedTime(_listeningSince).Ticks, Limit.Ticks))));
        using var preparing = new CancellationTokenSource();
        _preparing = preparing;
        var result = await Prepare(preparing.Token);
        _preparing = null;
        if (State is DictationState.Closed)
        {
            drafts.Take();
            return;
        }

        Move(result.IsSuccess ? new DictationState.DraftReady() : new DictationState.Failed(result.Error));
    }

    /// <summary>Any state but DraftReady -> Closed. Stops the recorder and cancels the preparation.</summary>
    public async Task Cancel()
    {
        if (State is DictationState.DraftReady or DictationState.Closed)
            return;

        var wasRecording = State is DictationState.Listening or DictationState.Starting;
        Move(new DictationState.Closed());
        _preparing?.Cancel();
        if (wasRecording)
            await speech.Cancel();
    }

    /// <summary>Failed -> a new recording.</summary>
    public Task Retry()
    {
        if (State is not DictationState.Failed)
            return Task.CompletedTask;

        Move(new DictationState.Starting());
        return Begin();
    }

    private async Task<Result> Prepare(CancellationToken cancellationToken)
    {
        try
        {
            return await speech.StopAndPrepare(WhenFull.Send, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return Result.Failure(SpeechErrors.Canceled);
        }
        catch
        {
            return Result.Failure(SpeechErrors.Failed);
        }
    }

    private void Move(DictationState state)
    {
        State = state;
        Changed?.Invoke(this, state);
    }
}
