namespace Centello.Application;

public static class SpeechRecordingLimits
{
    public const int SampleRate = 16_000;
    public const int MaximumDurationSeconds = 20;
    public const int MaximumAudioBytes = SampleRate * sizeof(short) * MaximumDurationSeconds;

    public static int BytesThatFit(int currentBytes, int additionalBytes) =>
        Math.Clamp(MaximumAudioBytes - currentBytes, 0, additionalBytes);
}
