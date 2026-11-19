namespace Centesimo.Application;

/// <summary>PCM 16-bit mono little-endian audio sampled at <see cref="SpeechRecordingLimits.SampleRate"/>.</summary>
public sealed record RecordedAudio(ReadOnlyMemory<byte> Pcm16);
