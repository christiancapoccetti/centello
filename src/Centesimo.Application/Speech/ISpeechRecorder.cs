namespace Centesimo.Application;

public interface ISpeechRecorder
{
    bool IsListening { get; }
    Task<Result> Start(CancellationToken cancellationToken = default);
    Task<Result<RecordedAudio>> Stop(CancellationToken cancellationToken = default);
    Task Cancel();
}
