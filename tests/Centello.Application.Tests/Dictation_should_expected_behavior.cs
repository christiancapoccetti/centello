using Centello.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace Centello.Application.Tests;

public sealed class Dictation_should_expected_behavior
{
    private static readonly RecordedAudio Heard = new(new byte[] { 1, 2, 3, 4 });
    private static readonly RecordedAudio Full = new(new byte[SpeechRecordingLimits.MaximumAudioBytes]);
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(SpeechRecordingLimits.MaximumDurationSeconds);

    private readonly FakeDraftConsentStore _consentStore = new();
    private readonly FakeMicrophoneAccess _microphone = new();
    private readonly FakeSpeechRecorder _recorder = new() { Audio = Heard };
    private readonly FakeExpenseInterpreter _interpreter = new();
    private readonly ManualTimeProvider _clock = new();
    private readonly List<PanelState> _changes = [];
    private readonly Dictation _dictation;

    public Dictation_should_expected_behavior()
    {
        var categories = new FakeCategoryRepository();
        var scopes = new CountingScopeFactory(new ServiceMap(
            new CategoryService(categories),
            new TagService(categories, new FakeTagRepository())));
        var drafts = new ExpenseDraftService(
            _recorder,
            _interpreter,
            new FakeInternetAccess(),
            new DraftConsent(_consentStore),
            new FakePdfPageCounter(),
            scopes);
        _interpreter.Draft = new ExpenseDraft(10_000, null, null, null, "", DraftSource.Voice, "cento euro carburante");
        _dictation = new Dictation(drafts, new DraftConsent(_consentStore), _microphone, _clock);
        _dictation.Changed += (_, state) => _changes.Add(state);
    }

    [Fact]
    public void Read_from_the_voice_source()
    {
        Assert.Equal(DraftSource.Voice, _dictation.Source);
    }

    [Fact]
    public async Task Begin_again_when_restored()
    {
        await _dictation.Restore();

        Assert.Equal(new PanelState.Listening(TimeSpan.Zero), _dictation.State);
    }

    [Fact]
    public void Keep_the_panel_when_stopped_only_while_taking_a_photo()
    {
        Assert.True(new PanelState.TakingPhoto().KeepsPanelWhenStopped);
        Assert.False(new PanelState.Reading().KeepsPanelWhenStopped);
        Assert.False(new PanelState.Listening(TimeSpan.Zero).KeepsPanelWhenStopped);
    }

    [Fact]
    public void Start_in_the_starting_state()
    {
        Assert.Equal(new PanelState.Starting(), _dictation.State);
    }

    [Fact]
    public async Task Ask_for_the_consent_before_asking_for_the_microphone()
    {
        _consentStore.AcceptedVersion = 0;

        await _dictation.Begin();

        Assert.Equal(new PanelState.NeedsConsent(), _dictation.State);
        Assert.Equal(0, _microphone.AskedCount);
        Assert.Equal(0, _recorder.StartCount);
    }

    [Fact]
    public async Task Fail_with_the_microphone_error_when_the_microphone_is_refused()
    {
        _microphone.Granted = false;

        await _dictation.Begin();

        Assert.Equal(new PanelState.Failed(SpeechErrors.MicrophoneDenied), _dictation.State);
        Assert.Equal(0, _recorder.StartCount);
    }

    [Fact]
    public async Task Wait_for_permission_before_listening_and_ignore_taps_while_waiting()
    {
        var permission = new TaskCompletionSource();
        _microphone.Gate = permission.Task;

        var begin = _dictation.Begin();
        await _dictation.Begin();
        await _dictation.Send();

        Assert.Equal(1, _microphone.AskedCount);
        Assert.Equal(0, _recorder.StartCount);
        Assert.Equal(0, _recorder.StopCount);

        permission.SetResult();
        await begin;

        Assert.Equal(new PanelState.Listening(TimeSpan.Zero), _dictation.State);
        Assert.Equal(1, _recorder.StartCount);
    }

    [Fact]
    public async Task Fail_when_the_permission_request_throws()
    {
        _microphone.Gate = Task.FromException(new InvalidOperationException());

        await _dictation.Begin();

        Assert.Equal(new PanelState.Failed(SpeechErrors.StartFailed), _dictation.State);
        Assert.Equal(0, _recorder.StartCount);
    }

    [Fact]
    public async Task Fail_when_the_recorder_cannot_start()
    {
        _recorder.StartResult = Result.Failure(SpeechErrors.StartFailed);

        await _dictation.Begin();

        Assert.Equal(new PanelState.Failed(SpeechErrors.StartFailed), _dictation.State);
    }

    [Fact]
    public async Task Listen_from_zero_once_the_recorder_started()
    {
        await _dictation.Begin();

        Assert.Equal(new PanelState.Listening(TimeSpan.Zero), _dictation.State);
        Assert.Equal(
            [new PanelState.Listening(TimeSpan.Zero)],
            _changes);
    }

    [Fact]
    public async Task Update_what_was_heard_on_every_tick()
    {
        await _dictation.Begin();

        _clock.Advance(TimeSpan.FromSeconds(3));
        await _dictation.Tick();

        Assert.Equal(new PanelState.Listening(TimeSpan.FromSeconds(3)), _dictation.State);
    }

    [Fact]
    public async Task Send_by_itself_at_the_limit()
    {
        _recorder.Audio = Full;
        await _dictation.Begin();

        _clock.Advance(Limit);
        await _dictation.Tick();

        Assert.Equal(new PanelState.DraftReady(_interpreter.Draft), _dictation.State);
        Assert.Equal(1, _interpreter.CallCount);
        Assert.Equal(Full, _interpreter.Audio);
    }

    [Fact]
    public async Task Send_a_recording_that_filled_a_moment_before_the_clock()
    {
        _recorder.Audio = Full;
        await _dictation.Begin();

        _clock.Advance(Limit - TimeSpan.FromMilliseconds(100));
        await _dictation.Send();

        Assert.Equal(new PanelState.DraftReady(_interpreter.Draft), _dictation.State);
    }

    [Fact]
    public async Task Send_once_when_a_tap_and_the_limit_come_together()
    {
        _recorder.Audio = Full;
        await _dictation.Begin();
        var gate = new TaskCompletionSource();
        _interpreter.Gate = gate.Task;
        _clock.Advance(Limit);

        var tap = _dictation.Send();
        await _dictation.Tick();
        gate.SetResult();
        await tap;

        Assert.Equal(1, _interpreter.CallCount);
        Assert.Equal(1, _recorder.StopCount);
        Assert.Equal(new PanelState.DraftReady(_interpreter.Draft), _dictation.State);
        Assert.Single(_changes, state => state is PanelState.DraftReady);
    }

    [Fact]
    public async Task Ignore_a_second_send_while_preparing()
    {
        await _dictation.Begin();
        var gate = new TaskCompletionSource();
        _interpreter.Gate = gate.Task;

        var first = _dictation.Send();
        await _dictation.Send();
        gate.SetResult();
        await first;

        Assert.Equal(1, _interpreter.CallCount);
        Assert.Equal(1, _recorder.StopCount);
    }

    [Fact]
    public async Task Ignore_a_send_before_listening()
    {
        await _dictation.Send();

        Assert.Equal(new PanelState.Starting(), _dictation.State);
        Assert.Equal(0, _recorder.StopCount);
    }

    [Fact]
    public async Task Show_how_much_was_heard_while_preparing()
    {
        await _dictation.Begin();
        _clock.Advance(TimeSpan.FromSeconds(4));

        await _dictation.Send();

        Assert.Contains(new PanelState.Preparing(TimeSpan.FromSeconds(4)), _changes);
    }

    [Fact]
    public async Task Fail_with_the_error_when_the_preparation_failed()
    {
        var failure = new Error("Test.Interpretation", "Interpretazione non riuscita.");
        _interpreter.Failure = failure;
        await _dictation.Begin();

        await _dictation.Send();

        Assert.Equal(new PanelState.Failed(failure), _dictation.State);
    }

    [Fact]
    public async Task Fail_with_the_generic_error_when_the_preparation_throws()
    {
        _interpreter.OnInterpret = () => throw new InvalidOperationException();
        await _dictation.Begin();

        await _dictation.Send();

        Assert.Equal(new PanelState.Failed(SpeechErrors.ProcessingFailed), _dictation.State);
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

        Assert.Equal(new PanelState.Closed(), _dictation.State);
        Assert.Equal(1, _recorder.CancelCount);
        Assert.Equal(0, _recorder.StopCount);
    }

    [Fact]
    public async Task Leave_the_recorder_alone_when_a_canceled_start_did_not_acquire_it()
    {
        var gate = new TaskCompletionSource();
        _recorder.StartGate = gate.Task;
        _recorder.StartResult = Result.Failure(SpeechErrors.StartFailed);

        var begin = _dictation.Begin();
        await _dictation.Cancel();
        gate.SetResult();
        await begin;

        Assert.Equal(new PanelState.Closed(), _dictation.State);
        Assert.Equal(0, _recorder.CancelCount);
    }

    [Fact]
    public async Task Close_without_recording_when_canceled_during_the_permission_question()
    {
        var gate = new TaskCompletionSource();
        _microphone.Gate = gate.Task;

        var begin = _dictation.Begin();
        await _dictation.Cancel();
        gate.SetResult();
        await begin;

        Assert.Equal(new PanelState.Closed(), _dictation.State);
        Assert.Equal(0, _recorder.StartCount);
        Assert.Equal(0, _recorder.CancelCount);
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

        Assert.Equal(new PanelState.Closed(), _dictation.State);
    }

    [Fact]
    public async Task Close_and_stop_the_recorder_when_canceled_while_listening()
    {
        await _dictation.Begin();

        await _dictation.Cancel();

        Assert.Equal(new PanelState.Closed(), _dictation.State);
        Assert.Equal(1, _recorder.CancelCount);
        Assert.Equal(0, _interpreter.CallCount);
    }

    [Fact]
    public async Task Ignore_a_tick_after_the_cancel()
    {
        await _dictation.Begin();
        await _dictation.Cancel();

        _clock.Advance(Limit);
        await _dictation.Tick();

        Assert.Equal(new PanelState.Closed(), _dictation.State);
        Assert.Equal(0, _interpreter.CallCount);
    }

    [Fact]
    public async Task Leave_no_draft_behind_when_canceled_while_preparing()
    {
        await _dictation.Begin();
        var gate = new TaskCompletionSource();
        _interpreter.Gate = gate.Task;

        var send = _dictation.Send();
        await _dictation.Cancel();
        gate.SetResult();
        await send;

        Assert.Equal(new PanelState.Closed(), _dictation.State);
        Assert.Equal(1, _interpreter.CallCount);
        Assert.DoesNotContain(_changes, state => state is PanelState.DraftReady);
    }

    [Fact]
    public async Task Keep_the_draft_when_canceled_after_it_was_ready()
    {
        await _dictation.Begin();
        await _dictation.Send();

        await _dictation.Cancel();

        Assert.Equal(new PanelState.DraftReady(_interpreter.Draft), _dictation.State);
    }

    [Fact]
    public async Task Record_again_when_retried_after_a_failure()
    {
        _microphone.Granted = false;
        await _dictation.Begin();
        _microphone.Granted = true;

        await _dictation.Retry();

        Assert.Equal(new PanelState.Listening(TimeSpan.Zero), _dictation.State);
        Assert.Equal(1, _recorder.StartCount);
    }

    [Fact]
    public async Task Ignore_a_retry_unless_failed()
    {
        await _dictation.Begin();

        await _dictation.Retry();

        Assert.Equal(new PanelState.Listening(TimeSpan.Zero), _dictation.State);
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

        Assert.Equal(new PanelState.Listening(TimeSpan.FromSeconds(1)), _dictation.State);
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
