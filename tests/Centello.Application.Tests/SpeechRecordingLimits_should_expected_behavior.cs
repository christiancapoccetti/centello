using Centello.Application;

namespace Centello.Application.Tests;

public sealed class SpeechRecordingLimits_should_expected_behavior
{
    [Fact]
    public void keep_the_whole_chunk_when_it_fits_the_recording_budget()
    {
        var fits = SpeechRecordingLimits.BytesThatFit(SpeechRecordingLimits.MaximumAudioBytes - 8, 4);

        Assert.Equal(4, fits);
    }

    [Fact]
    public void keep_only_the_part_of_the_chunk_that_fits_the_recording_budget()
    {
        var fits = SpeechRecordingLimits.BytesThatFit(SpeechRecordingLimits.MaximumAudioBytes - 2, 6);

        Assert.Equal(2, fits);
    }

    [Fact]
    public void keep_nothing_once_the_recording_budget_is_used()
    {
        var fits = SpeechRecordingLimits.BytesThatFit(SpeechRecordingLimits.MaximumAudioBytes, sizeof(short));

        Assert.Equal(0, fits);
    }
}
