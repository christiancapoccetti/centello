using Centello.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace Centello.Application.Tests;

public sealed class PhotoReading_should_expected_behavior
{
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x01, 0x02];

    private readonly FakeApiKeyStore _keys = new() { Key = "sk-test" };
    private readonly FakePhotoCapture _camera = new();
    private readonly FakeExpenseInterpreter _interpreter = new();
    private readonly List<PanelState> _changes = [];
    private readonly PhotoReading _reading;

    public PhotoReading_should_expected_behavior()
    {
        _interpreter.Draft = new ExpenseDraft(1_250, null, null, null, "", DraftSource.Photo, "");
        _reading = Create(_interpreter);
    }

    [Fact]
    public void Start_in_the_starting_state_from_the_photo_source()
    {
        Assert.Equal(new PanelState.Starting(), _reading.State);
        Assert.Same(DraftSource.Photo, _reading.Source);
    }

    [Fact]
    public async Task Ask_for_the_api_key_without_touching_the_camera()
    {
        _keys.Key = "";

        await _reading.Begin();

        Assert.Equal(new PanelState.NeedsApiKey(), _reading.State);
        Assert.Equal(0, _camera.AccessCount);
        Assert.Equal(0, _camera.TakeCount);
    }

    [Fact]
    public async Task Fail_with_the_camera_error_when_access_is_refused()
    {
        _camera.Granted = false;

        await _reading.Begin();

        Assert.Equal(new PanelState.Failed(PhotoErrors.CameraDenied), _reading.State);
        Assert.Equal(0, _camera.TakeCount);
    }

    [Fact]
    public async Task Keep_the_panel_while_the_camera_is_in_front()
    {
        var gate = new TaskCompletionSource<Result<byte[]?>>();
        _camera.TakeGate = gate.Task;

        var begin = _reading.Begin();

        Assert.Equal(new PanelState.TakingPhoto(), _reading.State);
        Assert.True(_reading.State.KeepsPanelWhenStopped);
        gate.SetResult(Result<byte[]?>.Success(null));
        await begin;
    }

    [Fact]
    public async Task Close_when_the_camera_came_back_with_nothing()
    {
        _camera.Photo = null;

        await _reading.Begin();

        Assert.Equal(new PanelState.Closed(), _reading.State);
        Assert.Equal(0, _interpreter.CallCount);
    }

    [Fact]
    public async Task Fail_with_the_error_when_the_camera_failed()
    {
        _camera.Failure = PhotoErrors.CameraUnavailable;

        await _reading.Begin();

        Assert.Equal(new PanelState.Failed(PhotoErrors.CameraUnavailable), _reading.State);
    }

    [Fact]
    public async Task Fail_with_the_unavailable_error_when_the_camera_throws()
    {
        _camera.OnTake = () => throw new InvalidOperationException();

        await _reading.Begin();

        Assert.Equal(new PanelState.Failed(PhotoErrors.Unavailable), _reading.State);
    }

    [Fact]
    public async Task Read_the_photo_into_a_draft()
    {
        _camera.Photo = Jpeg;

        await _reading.Begin();

        Assert.Equal(new PanelState.DraftReady(_interpreter.Draft), _reading.State);
        Assert.Equal(
            [new PanelState.TakingPhoto(), new PanelState.Reading(), new PanelState.DraftReady(_interpreter.Draft)],
            _changes);
        Assert.Equal(Jpeg, _interpreter.Document!.Bytes.ToArray());
    }

    [Fact]
    public async Task Fail_with_the_error_when_the_reading_failed()
    {
        _camera.Photo = Jpeg;
        _interpreter.Failure = new Error("Test.Reading", "Lettura non riuscita.");

        await _reading.Begin();

        Assert.Equal(new PanelState.Failed(_interpreter.Failure), _reading.State);
    }

    [Fact]
    public async Task Fail_with_the_unsupported_error_when_the_photo_is_not_an_image()
    {
        _camera.Photo = [1, 2, 3];

        await _reading.Begin();

        Assert.Equal(new PanelState.Failed(PhotoErrors.Unsupported), _reading.State);
    }

    [Fact]
    public async Task Fail_with_the_generic_error_when_the_reading_throws()
    {
        _camera.Photo = Jpeg;
        _interpreter.OnInterpret = () => throw new InvalidOperationException();

        await _reading.Begin();

        Assert.Equal(new PanelState.Failed(PhotoErrors.ProcessingFailed), _reading.State);
    }

    [Fact]
    public async Task Drop_a_photo_that_arrives_after_the_cancel()
    {
        var gate = new TaskCompletionSource<Result<byte[]?>>();
        _camera.TakeGate = gate.Task;

        var begin = _reading.Begin();
        await _reading.Cancel();
        gate.SetResult(Result<byte[]?>.Success(Jpeg));
        await begin;

        Assert.Equal(new PanelState.Closed(), _reading.State);
        Assert.Equal(0, _interpreter.CallCount);
    }

    [Fact]
    public async Task Close_without_the_camera_when_canceled_during_the_permission_question()
    {
        var gate = new TaskCompletionSource();
        _camera.AccessGate = gate.Task;

        var begin = _reading.Begin();
        await _reading.Cancel();
        gate.SetResult();
        await begin;

        Assert.Equal(new PanelState.Closed(), _reading.State);
        Assert.Equal(0, _camera.TakeCount);
    }

    [Fact]
    public async Task Leave_no_draft_behind_when_canceled_while_reading()
    {
        _camera.Photo = Jpeg;
        _interpreter.OnInterpret = () => _ = _reading.Cancel();

        await _reading.Begin();

        Assert.Equal(new PanelState.Closed(), _reading.State);
        Assert.DoesNotContain(_changes, state => state is PanelState.DraftReady);
    }

    [Fact]
    public async Task Cancel_the_token_of_the_reading_when_canceled_while_reading()
    {
        var waiting = new WaitingInterpreter();
        var reading = Create(waiting);
        _camera.Photo = Jpeg;

        var begin = reading.Begin();
        await waiting.Entered.Task;
        await reading.Cancel();
        await begin;

        Assert.True(waiting.Token.IsCancellationRequested);
        Assert.Equal(new PanelState.Closed(), reading.State);
    }

    [Fact]
    public async Task Keep_the_draft_when_canceled_after_it_was_ready()
    {
        _camera.Photo = Jpeg;
        await _reading.Begin();

        await _reading.Cancel();

        Assert.Equal(new PanelState.DraftReady(_interpreter.Draft), _reading.State);
    }

    [Fact]
    public async Task Open_the_camera_again_when_retried_after_a_failure()
    {
        _camera.Granted = false;
        await _reading.Begin();
        _camera.Granted = true;
        _camera.Photo = Jpeg;

        await _reading.Retry();

        Assert.Equal(new PanelState.DraftReady(_interpreter.Draft), _reading.State);
        Assert.Equal(1, _camera.TakeCount);
    }

    [Fact]
    public async Task Ignore_a_retry_unless_failed()
    {
        _camera.Photo = Jpeg;
        await _reading.Begin();

        await _reading.Retry();

        Assert.Equal(1, _camera.TakeCount);
    }

    [Fact]
    public async Task Ignore_a_begin_after_the_first()
    {
        await _reading.Begin();
        await _reading.Begin();

        Assert.Equal(1, _camera.TakeCount);
    }

    [Fact]
    public async Task Fail_with_the_lost_error_when_restored()
    {
        await _reading.Restore();

        Assert.Equal(new PanelState.Failed(PhotoErrors.Lost), _reading.State);
        Assert.Equal(0, _camera.AccessCount);
    }

    [Fact]
    public async Task Ignore_a_restore_unless_starting()
    {
        _camera.Photo = Jpeg;
        await _reading.Begin();

        await _reading.Restore();

        Assert.Equal(new PanelState.DraftReady(_interpreter.Draft), _reading.State);
    }

    private PhotoReading Create(IExpenseInterpreter interpreter)
    {
        var categories = new FakeCategoryRepository();
        var scopes = new CountingScopeFactory(new ServiceMap(
            new CategoryService(categories),
            new TagService(categories, new FakeTagRepository())));
        var drafts = new ExpenseDraftService(
            new FakeSpeechRecorder(),
            new FakeSpeechTranscriber(),
            interpreter,
            new FakeInternetAccess(),
            scopes);
        var reading = new PhotoReading(drafts, new OpenAiSettings(_keys), _camera);
        reading.Changed += (_, state) => _changes.Add(state);
        return reading;
    }

    private sealed class FakePhotoCapture : IPhotoCapture
    {
        public bool Granted { get; set; } = true;
        public byte[]? Photo { get; set; }
        public Error? Failure { get; set; }
        public Task? AccessGate { get; set; }
        public Task<Result<byte[]?>>? TakeGate { get; set; }
        public Action? OnTake { get; set; }
        public int AccessCount { get; private set; }
        public int TakeCount { get; private set; }

        public async Task<bool> EnsureAccess()
        {
            AccessCount++;
            if (AccessGate is not null)
                await AccessGate;

            return Granted;
        }

        public async Task<Result<byte[]?>> Take()
        {
            TakeCount++;
            OnTake?.Invoke();
            if (TakeGate is not null)
                return await TakeGate;

            return Failure is null ? Result<byte[]?>.Success(Photo) : Result<byte[]?>.Failure(Failure);
        }
    }

    private sealed class WaitingInterpreter : IExpenseInterpreter
    {
        public TaskCompletionSource Entered { get; } = new();
        public CancellationToken Token { get; private set; }

        public Task<Result<ExpenseDraft>> Interpret(
            string phrase,
            IReadOnlyCollection<DraftCategory> categories,
            DateOnly today,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public async Task<Result<ExpenseDraft>> Interpret(
            ExpenseDocument document,
            IReadOnlyCollection<DraftCategory> categories,
            DateOnly today,
            CancellationToken cancellationToken = default)
        {
            Token = cancellationToken;
            Entered.SetResult();
            await Task.Delay(Timeout.Infinite, cancellationToken);
            throw new InvalidOperationException();
        }
    }
}
