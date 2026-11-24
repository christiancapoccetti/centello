#if ANDROID
using Android.Media;
using Centello.Application;
using System.Runtime.InteropServices;

namespace Centello.App;

public sealed class AndroidAudioRecorder : ISpeechRecorder, IDisposable
{
    private const int SampleRate = SpeechRecordingLimits.SampleRate;
    private static readonly Error Unavailable = new("Speech.Unavailable", "Il riconoscimento vocale non è disponibile.");
    private readonly SemaphoreSlim _gate = new(1, 1);
    private int _disposeStarted;
    private RecordingSession? _session;

    public bool IsListening => Volatile.Read(ref _session) is not null;

    public async Task<Result> Start(CancellationToken cancellationToken = default)
    {
        if (Volatile.Read(ref _disposeStarted) != 0)
            return Result.Failure(Unavailable);

        if (await Permissions.CheckStatusAsync<Permissions.Microphone>() != PermissionStatus.Granted)
            return Result.Failure(SpeechErrors.MicrophoneDenied);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (Volatile.Read(ref _disposeStarted) != 0)
                return Result.Failure(Unavailable);

            if (_session is not null)
                return Result.Failure(new Error("Speech.AlreadyListening", "La registrazione è già attiva."));

            var session = new RecordingSession();
            session.Start();
            _session = session;
            return Result.Success();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<Result<RecordedAudio>> Stop(CancellationToken cancellationToken = default)
    {
        var session = await TakeSession(cancellationToken, false);
        if (session is null)
            return Result<RecordedAudio>.Failure(new Error("Speech.NotListening", "La registrazione non è attiva."));

        try
        {
            return Result<RecordedAudio>.Success(await session.Stop());
        }
        catch (Exception)
        {
            return Result<RecordedAudio>.Failure(new Error("Speech.Failed", "Non riesco a registrare l'audio."));
        }
        finally
        {
            session.Dispose();
        }
    }

    public async Task Cancel()
    {
        using var session = await TakeSession(CancellationToken.None, true);
        if (session is not null)
            await session.CancelAndWait();
    }

    private async Task<RecordingSession?> TakeSession(CancellationToken cancellationToken, bool cancelImmediately)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var session = _session;
            _session = null;
            if (cancelImmediately)
                session?.Cancel();

            return session;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposeStarted, 1) != 0)
            return;

        Cancel().GetAwaiter().GetResult();
    }

    private sealed class RecordingSession : IDisposable
    {
        private readonly CancellationTokenSource _cancellation = new();
        private readonly MemoryStream _audio = new();
        private AudioRecord? _audioRecord;
        private Task? _recording;

        public void Start() => _recording = Task.Run(Record, CancellationToken.None);

        public async Task<RecordedAudio> Stop()
        {
            await CancelAndWait();
            return new RecordedAudio(_audio.ToArray());
        }

        public void Cancel()
        {
            _cancellation.Cancel();
            StopRecording(_audioRecord);
        }

        public async Task CancelAndWait()
        {
            Cancel();
            if (_recording is not null)
                await _recording;
        }

        private void Record()
        {
            var bufferSize = AudioRecord.GetMinBufferSize(SampleRate, ChannelIn.Mono, Encoding.Pcm16bit);
            var audioRecord = new AudioRecord(AudioSource.Mic, SampleRate, ChannelIn.Mono, Encoding.Pcm16bit, bufferSize * 2);
            _audioRecord = audioRecord;
            var buffer = new short[bufferSize];
            try
            {
                audioRecord.StartRecording();
                while (!_cancellation.IsCancellationRequested)
                {
                    var read = audioRecord.Read(buffer, 0, buffer.Length);
                    if (read <= 0)
                        continue;

                    var bytes = MemoryMarshal.AsBytes(buffer.AsSpan(0, read));
                    var fits = SpeechRecordingLimits.BytesThatFit((int)_audio.Length, bytes.Length);
                    _audio.Write(bytes[..fits]);
                    if (_audio.Length >= SpeechRecordingLimits.MaximumAudioBytes)
                        break;
                }
            }
            finally
            {
                StopRecording(audioRecord);
                audioRecord.Release();
                audioRecord.Dispose();
                _audioRecord = null;
                Array.Clear(buffer);
            }
        }

        public void Dispose()
        {
            Cancel();
            _audio.Dispose();
            _cancellation.Dispose();
        }

        private static void StopRecording(AudioRecord? audioRecord)
        {
            if (audioRecord is null)
                return;

            try
            {
                if (audioRecord.RecordingState == RecordState.Recording)
                    audioRecord.Stop();
            }
            catch (Java.Lang.IllegalStateException)
            {
            }
        }
    }
}
#endif
