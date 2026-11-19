using Centesimo.Application;

namespace Centesimo.App;

public sealed class UnavailableSpeechRecorder : ISpeechRecorder
{
    public bool IsListening => false;

    public Task<Result> Start(CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Failure(new Error("Speech.Unsupported", "Il riconoscimento vocale è disponibile solo su Android.")));

    public Task<Result<RecordedAudio>> Stop(CancellationToken cancellationToken = default) =>
        Task.FromResult(Result<RecordedAudio>.Failure(new Error("Speech.NotListening", "La registrazione non è attiva.")));

    public Task Cancel() => Task.CompletedTask;
}
