namespace Centesimo.Application;

/// <summary>PCM 16-bit mono little-endian audio sampled at <see cref="SpeechRecordingLimits.SampleRate"/>.</summary>
public sealed record RecordedAudio(ReadOnlyMemory<byte> Pcm16)
{
    /// <summary>The recording reached the limit: the recorder stopped there and kept all of it.</summary>
    public bool IsFull => Pcm16.Length >= SpeechRecordingLimits.MaximumAudioBytes;
}
