namespace Centesimo.Application;

public interface ISpeechTranscriber
{
    /// <param name="keywords">Category and tag names, used as transcription hints when the provider supports them.</param>
    Task<Result<string>> Transcribe(
        RecordedAudio audio,
        IReadOnlyCollection<string> keywords,
        CancellationToken cancellationToken = default);
}
