namespace Centello.Application;

/// <summary>One dictation of the voice panel, from opening to a draft or to nothing. Every call comes from the panel's
/// main thread and every await returns to it, so State has one writer. A call that does not fit the state is ignored:
/// a second tap while preparing does nothing, and Cancel after DraftReady keeps the draft for the editor.</summary>
public sealed class Dictation(
    ExpenseDraftService drafts,
    DraftConsent consent,
    IMicrophoneAccess microphone,
    TimeProvider clock) : IPanelCapture
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(SpeechRecordingLimits.MaximumDurationSeconds);
    private CancellationTokenSource? _preparing;
    private long _listeningSince;

    public DraftSource Source => DraftSource.Voice;
    public PanelState State { get; private set; } = new PanelState.Starting();
    public event EventHandler<PanelState>? Changed;

    public Task Restore() => Begin();

    public async Task Begin()
    {
        if (State is not PanelState.Starting)
            return;

        if (!consent.IsEnabled)
        {
            Move(new PanelState.NeedsConsent());
            return;
        }

        var granted = await microphone.Ensure();
        if (State is not PanelState.Starting)
            return;

        if (!granted)
        {
            Move(new PanelState.Failed(SpeechErrors.MicrophoneDenied));
            return;
        }

        var started = await drafts.StartListening();
        if (State is not PanelState.Starting)
        {
            await drafts.CancelListening();
            return;
        }

        if (started.IsFailure)
        {
            Move(new PanelState.Failed(started.Error));
            return;
        }

        _listeningSince = clock.GetTimestamp();
        Move(new PanelState.Listening(TimeSpan.Zero));
    }

    /// <summary>From the panel's timer while listening: updates Heard and sends at the limit.</summary>
    public async Task Tick()
    {
        if (State is not PanelState.Listening)
            return;

        var heard = clock.GetElapsedTime(_listeningSince);
        if (heard >= Limit)
        {
            await Send();
            return;
        }

        Move(new PanelState.Listening(heard));
    }

    /// <summary>Listening -> Preparing -> DraftReady or Failed. A full recording is sent (WhenFull.Send).</summary>
    public async Task Send()
    {
        if (State is not PanelState.Listening)
            return;

        Move(new PanelState.Preparing(TimeSpan.FromTicks(Math.Min(clock.GetElapsedTime(_listeningSince).Ticks, Limit.Ticks))));
        using var preparing = new CancellationTokenSource();
        _preparing = preparing;
        var result = await Prepare(preparing.Token);
        _preparing = null;
        if (State is PanelState.Closed)
            return;

        Move(result.IsSuccess ? new PanelState.DraftReady(result.Value) : new PanelState.Failed(result.Error));
    }

    /// <summary>Any state but DraftReady -> Closed. Stops the recorder and cancels the preparation.</summary>
    public async Task Cancel()
    {
        if (State is PanelState.DraftReady or PanelState.Closed)
            return;

        var wasRecording = State is PanelState.Listening or PanelState.Starting;
        Move(new PanelState.Closed());
        _preparing?.Cancel();
        if (wasRecording)
            await drafts.CancelListening();
    }

    /// <summary>Failed -> a new recording.</summary>
    public Task Retry()
    {
        if (State is not PanelState.Failed)
            return Task.CompletedTask;

        Move(new PanelState.Starting());
        return Begin();
    }

    private async Task<Result<ExpenseDraft>> Prepare(CancellationToken cancellationToken)
    {
        try
        {
            return await drafts.PrepareFromSpeech(WhenFull.Send, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return Result<ExpenseDraft>.Failure(DraftErrors.Canceled);
        }
        catch
        {
            return Result<ExpenseDraft>.Failure(SpeechErrors.ProcessingFailed);
        }
    }

    private void Move(PanelState state)
    {
        State = state;
        Changed?.Invoke(this, state);
    }
}
