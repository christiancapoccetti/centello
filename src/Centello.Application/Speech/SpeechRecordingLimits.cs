namespace Centello.Application;

public static class SpeechRecordingLimits
{
    public const int SampleRate = 16_000;
    public const int MaximumDurationSeconds = 20;
    public const int MaximumAudioBytes = SampleRate * sizeof(short) * MaximumDurationSeconds;
    /// <summary>A press shorter than this is a tap, not a dictation: nothing useful was said in it.</summary>
    public static readonly TimeSpan MinimumHold = TimeSpan.FromMilliseconds(350);

    public static bool IsTap(TimeSpan held) => held < MinimumHold;

    public static int BytesThatFit(int currentBytes, int additionalBytes) =>
        Math.Clamp(MaximumAudioBytes - currentBytes, 0, additionalBytes);
}
