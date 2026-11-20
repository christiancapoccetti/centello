using Centesimo.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace Centesimo.Application.Tests;

public sealed class Dictation_should_expected_behavior
{
    private static readonly RecordedAudio Heard = new(new byte[] { 1, 2, 3, 4 });
    private static readonly RecordedAudio Full = new(new byte[SpeechRecordingLimits.MaximumAudioBytes]);
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(SpeechRecordingLimits.MaximumDurationSeconds);

    private readonly FakeApiKeyStore _keys = new() { Key = "sk-test" };
    private readonly FakeMicrophoneAccess _microphone = new();
    private readonly FakeSpeechRecorder _recorder = new() { Audio = Heard };
    private readonly FakeSpeechTranscriber _transcriber = new();
    private readonly FakeExpenseTextInterpreter _interpreter = new();
    private readonly PendingExpenseSpeechDraft _pending = new();
    private readonly ManualTimeProvider _clock = new();
    private readonly List<DictationState> _changes = [];
    private readonly Dictation _dictation;

    public Dictation_should_expected_behavior()
    {
        var categories = new FakeCategoryRepository();
        var scopes = new CountingScopeFactory(new ServiceMap(
            new CategoryService(categories),
            new TagService(categories, new FakeTagRepository())));
        var speech = new SpeechExpenseDraftService(
            _recorder,
            _transcriber,
            _interpreter,
            new OpenAiSettings(_keys),
            new FakeInternetAccess(),
            scopes,
            _pending);
        _transcriber.Result = Result<string>.Success("cento euro carburante");
        _interpreter.Draft = new ExpenseSpeechDraft(10_000, null, "", null, "", null, "", "cento euro carburante");
        _dictation = new Dictation(speech, _microphone, _pending, _clock);
        _dictation.Changed += (_, state) => _changes.Add(state);
    }

    [Fact]
    public void Start_in_the_starting_state()
    {
        Assert.Equal(new DictationState.Starting(), _dictation.State);
    }

    [Fact]
    public async Task Ask_for_the_api_key_before_asking_for_the_microphone()
    {
        _keys.Key = "";

        await _dictation.Begin();

        Assert.Equal(new DictationState.NeedsApiKey(), _dictation.State);
        Assert.Equal(0, _microphone.AskedCount);
        Assert.Equal(0, _recorder.StartCount);
    }

    [Fact]
    public async Task Fail_with_the_microphone_error_when_the_microphone_is_refused()
    {
        _microphone.Granted = false;

        await _dictation.Begin();

        Assert.Equal(new DictationState.Failed(SpeechErrors.MicrophoneDenied), _dictation.State);
        Assert.Equal(0, _recorder.StartCount);
    }

    [Fact]
    public async Task Fail_when_the_recorder_cannot_start()
    {
        _recorder.StartResult = Result.Failure(SpeechErrors.StartFailed);

        await _dictation.Begin();

        Assert.Equal(new DictationState.Failed(SpeechErrors.StartFailed), _dictation.State);
    }

    [Fact]
    public async Task Listen_from_zero_once_the_recorder_started()
    {
        await _dictation.Begin();

        Assert.Equal(new DictationState.Listening(TimeSpan.Zero), _dictation.State);
        Assert.Equal(
            [new DictationState.Listening(TimeSpan.Zero)],
            _changes);
    }

    [Fact]
    public async Task Update_what_was_heard_on_every_tick()
    {
        await _dictation.Begin();

        _clock.Advance(TimeSpan.FromSeconds(3));
        await _dictation.Tick();

        Assert.Equal(new DictationState.Listening(TimeSpan.FromSeconds(3)), _dictation.State);
    }

    [Fact]
    public async Task Send_by_itself_at_the_limit()
    {
        _recorder.Audio = Full;
        await _dictation.Begin();

        _clock.Advance(Limit);
        await _dictation.Tick();

        Assert.Equal(new DictationState.DraftReady(), _dictation.State);
        Assert.Equal(1, _transcriber.CallCount);
        Assert.Equal(Full, _transcriber.Audio);
        Assert.NotNull(_pending.Take());
    }

    [Fact]
    public async Task Send_a_recording_that_filled_a_moment_before_the_clock()
    {
        _recorder.Audio = Full;
        await _dictation.Begin();

        _clock.Advance(Limit - TimeSpan.FromMilliseconds(100));
        await _dictation.Send();

        Assert.Equal(new DictationState.DraftReady(), _dictation.State);
    }

    [Fact]
    public async Task Send_once_when_a_tap_and_the_limit_come_together()
    {
        _recorder.Audio = Full;
        await _dictation.Begin();
        var gate = new TaskCompletionSource();
        _transcriber.Gate = gate.Task;
        _clock.Advance(Limit);

        var tap = _dictation.Send();
        await _dictation.Tick();
        gate.SetResult();
        await tap;

        Assert.Equal(1, _transcriber.CallCount);
        Assert.Equal(1, _recorder.StopCount);
        Assert.Equal(new DictationState.DraftReady(), _dictation.State);
    }

    [Fact]
    public async Task Ignore_a_second_send_while_preparing()
    {
        await _dictation.Begin();
        var gate = new TaskCompletionSource();
        _transcriber.Gate = gate.Task;

        var first = _dictation.Send();
        await _dictation.Send();
        gate.SetResult();
        await first;

        Assert.Equal(1, _transcriber.CallCount);
        Assert.Equal(1, _recorder.StopCount);
    }

    [Fact]
    public async Task Ignore_a_send_before_listening()
    {
        await _dictation.Send();

        Assert.Equal(new DictationState.Starting(), _dictation.State);
        Assert.Equal(0, _recorder.StopCount);
    }

    [Fact]
    public async Task Show_how_much_was_heard_while_preparing()
    {
        await _dictation.Begin();
        _clock.Advance(TimeSpan.FromSeconds(4));

        await _dictation.Send();

        Assert.Contains(new DictationState.Preparing(TimeSpan.FromSeconds(4)), _changes);
    }

    [Fact]
    public async Task Fail_with_the_error_when_the_preparation_failed()
    {
        var failure = new Error("Test.Transcription", "Trascrizione non riuscita.");
        _transcriber.Result = Result<string>.Failure(failure);
        await _dictation.Begin();

        await _dictation.Send();

        Assert.Equal(new DictationState.Failed(failure), _dictation.State);
        Assert.Null(_pending.Take());
    }

    [Fact]
    public async Task Fail_with_the_generic_error_when_the_preparation_throws()
    {
        _interpreter.OnInterpret = () => throw new InvalidOperationException();
        await _dictation.Begin();

        await _dictation.Send();

        Assert.Equal(new DictationState.Failed(SpeechErrors.Failed), _dictation.State);
    }

    [Fact]
    public async Task Close_and_stop_the_recorder_when_canceled_while_starting()
    {
        var gate = new TaskCompletionSource();
        _recorder.StartGate = gate.Task;

        var begin = _dictation.Begin();
        await _dictation.Cancel();
        gate.SetResult();
        await begin;

        Assert.Equal(new DictationState.Closed(), _dictation.State);
        Assert.True(_recorder.CancelCount > 0);
        Assert.Equal(0, _recorder.StopCount);
    }

    [Fact]
    public async Task Close_without_recording_when_canceled_during_the_consent_question()
    {
        var gate = new TaskCompletionSource();
        _microphone.Gate = gate.Task;

        var begin = _dictation.Begin();
        await _dictation.Cancel();
        gate.SetResult();
        await begin;

        Assert.Equal(new DictationState.Closed(), _dictation.State);
        Assert.Equal(0, _recorder.StartCount);
    }

    [Fact]
    public async Task Stay_closed_when_the_microphone_is_refused_after_the_cancel()
    {
        var gate = new TaskCompletionSource();
        _microphone.Gate = gate.Task;
        _microphone.Granted = false;

        var begin = _dictation.Begin();
        await _dictation.Cancel();
        gate.SetResult();
        await begin;

        Assert.Equal(new DictationState.Closed(), _dictation.State);
    }

    [Fact]
    public async Task Close_and_stop_the_recorder_when_canceled_while_listening()
    {
        await _dictation.Begin();

        await _dictation.Cancel();

        Assert.Equal(new DictationState.Closed(), _dictation.State);
        Assert.Equal(1, _recorder.CancelCount);
        Assert.Equal(0, _transcriber.CallCount);
    }

    [Fact]
    public async Task Ignore_a_tick_after_the_cancel()
    {
        await _dictation.Begin();
        await _dictation.Cancel();

        _clock.Advance(Limit);
        await _dictation.Tick();

        Assert.Equal(new DictationState.Closed(), _dictation.State);
        Assert.Equal(0, _transcriber.CallCount);
    }

    [Fact]
    public async Task Leave_no_draft_behind_when_canceled_while_preparing()
    {
        await _dictation.Begin();
        var gate = new TaskCompletionSource();
        _transcriber.Gate = gate.Task;

        var send = _dictation.Send();
        await _dictation.Cancel();
        gate.SetResult();
        await send;

        Assert.Equal(new DictationState.Closed(), _dictation.State);
        Assert.Null(_pending.Take());
    }

    [Fact]
    public async Task Take_back_a_draft_that_arrived_after_the_cancel()
    {
        await _dictation.Begin();
        _interpreter.OnInterpret = () => _pending.Set(_interpreter.Draft);
        var gate = new TaskCompletionSource();
        _transcriber.Gate = gate.Task;

        var send = _dictation.Send();
        await _dictation.Cancel();
        gate.SetResult();
        await send;

        Assert.Null(_pending.Take());
    }

    [Fact]
    public async Task Keep_the_draft_when_canceled_after_it_was_ready()
    {
        await _dictation.Begin();
        await _dictation.Send();

        await _dictation.Cancel();

        Assert.Equal(new DictationState.DraftReady(), _dictation.State);
        Assert.NotNull(_pending.Take());
    }

    [Fact]
    public async Task Record_again_when_retried_after_a_failure()
    {
        _microphone.Granted = false;
        await _dictation.Begin();
        _microphone.Granted = true;

        await _dictation.Retry();

        Assert.Equal(new DictationState.Listening(TimeSpan.Zero), _dictation.State);
        Assert.Equal(1, _recorder.StartCount);
    }

    [Fact]
    public async Task Ignore_a_retry_unless_failed()
    {
        await _dictation.Begin();

        await _dictation.Retry();

        Assert.Equal(new DictationState.Listening(TimeSpan.Zero), _dictation.State);
        Assert.Equal(1, _recorder.StartCount);
    }

    [Fact]
    public async Task Start_the_clock_again_on_a_retry()
    {
        _recorder.StartResult = Result.Failure(SpeechErrors.StartFailed);
        await _dictation.Begin();
        _clock.Advance(TimeSpan.FromSeconds(10));
        _recorder.StartResult = null;

        await _dictation.Retry();
        _clock.Advance(TimeSpan.FromSeconds(1));
        await _dictation.Tick();

        Assert.Equal(new DictationState.Listening(TimeSpan.FromSeconds(1)), _dictation.State);
    }

    private sealed class FakeMicrophoneAccess : IMicrophoneAccess
    {
        public bool Granted { get; set; } = true;
        public Task? Gate { get; set; }
        public int AskedCount { get; private set; }

        public async Task<bool> Ensure()
        {
            AskedCount++;
            if (Gate is not null)
                await Gate;

            return Granted;
        }
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private long _ticks;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => _ticks;

        public void Advance(TimeSpan by) => _ticks += by.Ticks;
    }
}
