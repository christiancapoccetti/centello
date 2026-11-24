namespace Centello.Application;

/// <summary>One dictation, from opening to a draft or to nothing. Every call comes from the main thread and every
/// await returns to it, so State has one writer. A call that does not fit the state is ignored:
/// a second tap while preparing does nothing, and Cancel after DraftReady keeps the draft for the editor.</summary>
public sealed class Dictation(
    ExpenseDraftService drafts,
    DraftConsent consent,
    IMicrophoneAccess microphone,
    TimeProvider clock) : IPanelCapture
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(SpeechRecordingLimits.MaximumDurationSeconds);
    private CancellationTokenSource? _starting;
    private CancellationTokenSource? _preparing;
    private long _listeningSince;

    public DraftSource Source => DraftSource.Voice;
    public PanelState State { get; private set; } = new PanelState.Starting();
    public event EventHandler<PanelState>? Changed;

    public Task Restore() => Begin();

    public async Task Begin()
    {
        if (State is not PanelState.Starting || _starting is not null)
            return;

        if (!consent.IsEnabled)
        {
            Move(new PanelState.NeedsConsent());
            return;
        }

        using var starting = new CancellationTokenSource();
        _starting = starting;
        var started = await Start(starting.Token);
        _starting = null;
        if (State is not PanelState.Starting)
        {
            if (started.IsSuccess)
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

    /// <summary>Listening -> Preparing -> DraftReady or Failed, including a recording at the limit.</summary>
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

        var wasRecording = State is PanelState.Listening;
        Move(new PanelState.Closed());
        _starting?.Cancel();
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

    private async Task<Result> Start(CancellationToken cancellationToken)
    {
        try
        {
            if (!await microphone.Ensure())
                return Result.Failure(SpeechErrors.MicrophoneDenied);

            cancellationToken.ThrowIfCancellationRequested();
            return await drafts.StartListening(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result.Failure(DraftErrors.Canceled);
        }
        catch
        {
            return Result.Failure(SpeechErrors.StartFailed);
        }
    }

    private async Task<Result<ExpenseDraft>> Prepare(CancellationToken cancellationToken)
    {
        try
        {
            return await drafts.PrepareFromSpeech(cancellationToken);
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
