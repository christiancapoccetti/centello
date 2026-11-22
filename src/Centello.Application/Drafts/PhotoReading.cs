namespace Centello.Application;

/// <summary>One photo of the photo panel, from opening to a draft or to nothing. Every call comes from the panel's main
/// thread and every await returns to it, so State has one writer. A call that does not fit the state is ignored, and a
/// photo or a result that arrives after Cancel is dropped.</summary>
public sealed class PhotoReading(
    ExpenseDraftService drafts,
    OpenAiSettings settings,
    IPhotoCapture photo) : IPanelCapture
{
    private CancellationTokenSource? _reading;

    public DraftSource Source => DraftSource.Photo;
    public PanelState State { get; private set; } = new PanelState.Starting();
    public event EventHandler<PanelState>? Changed;

    /// <summary>Starting: no key -> NeedsApiKey, without touching the camera; camera refused -> Failed(CameraDenied);
    /// otherwise TakingPhoto, then nothing -> Closed, an error -> Failed, a photo -> Reading -> DraftReady or Failed.</summary>
    public async Task Begin()
    {
        if (State is not PanelState.Starting)
            return;

        var hasKey = await settings.HasApiKey();
        if (State is not PanelState.Starting)
            return;

        if (!hasKey)
        {
            Move(new PanelState.NeedsApiKey());
            return;
        }

        var granted = await photo.EnsureAccess();
        if (State is not PanelState.Starting)
            return;

        if (!granted)
        {
            Move(new PanelState.Failed(PhotoErrors.CameraDenied));
            return;
        }

        Move(new PanelState.TakingPhoto());
        var taken = await Take();
        if (State is not PanelState.TakingPhoto)
            return;

        if (taken.IsFailure)
        {
            Move(new PanelState.Failed(taken.Error));
            return;
        }

        if (taken.Value is not { } bytes)
        {
            Move(new PanelState.Closed());
            return;
        }

        await Read(bytes);
    }

    /// <summary>Android rebuilt the panel while the camera was in front: the photo never came back.</summary>
    public Task Restore()
    {
        if (State is PanelState.Starting)
            Move(new PanelState.Failed(PhotoErrors.Lost));

        return Task.CompletedTask;
    }

    /// <summary>Failed -> the camera again.</summary>
    public Task Retry()
    {
        if (State is not PanelState.Failed)
            return Task.CompletedTask;

        Move(new PanelState.Starting());
        return Begin();
    }

    /// <summary>Any state but DraftReady -> Closed. Cancels the reading.</summary>
    public Task Cancel()
    {
        if (State is PanelState.DraftReady or PanelState.Closed)
            return Task.CompletedTask;

        Move(new PanelState.Closed());
        _reading?.Cancel();
        return Task.CompletedTask;
    }

    private async Task<Result<byte[]?>> Take()
    {
        try
        {
            return await photo.Take();
        }
        catch
        {
            return Result<byte[]?>.Failure(PhotoErrors.Unavailable);
        }
    }

    private async Task Read(byte[] bytes)
    {
        Move(new PanelState.Reading());
        using var reading = new CancellationTokenSource();
        _reading = reading;
        var result = await Prepare(bytes, reading.Token);
        _reading = null;
        if (State is not PanelState.Reading)
            return;

        Move(result.IsSuccess ? new PanelState.DraftReady(result.Value) : new PanelState.Failed(result.Error));
    }

    private async Task<Result<ExpenseDraft>> Prepare(byte[] bytes, CancellationToken cancellationToken)
    {
        try
        {
            return await drafts.PrepareFromDocument(bytes, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return Result<ExpenseDraft>.Failure(DraftErrors.Canceled);
        }
        catch
        {
            return Result<ExpenseDraft>.Failure(PhotoErrors.ProcessingFailed);
        }
    }

    private void Move(PanelState state)
    {
        State = state;
        Changed?.Invoke(this, state);
    }
}
