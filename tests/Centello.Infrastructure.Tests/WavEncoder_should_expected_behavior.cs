using System.Buffers.Binary;
using System.Text;
using Centello.Application;

namespace Centello.Infrastructure.Tests;

public sealed class WavEncoder_should_expected_behavior
{
    private static readonly byte[] Pcm = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06];

    [Fact]
    public void Write_a_44_byte_riff_header_followed_by_the_samples()
    {
        var wav = WavEncoder.Encode(new RecordedAudio(Pcm));

        Assert.Equal(44 + Pcm.Length, wav.Length);
        Assert.Equal(Pcm, wav[44..]);
    }

    [Fact]
    public void Describe_16_khz_mono_pcm_16_bit_audio()
    {
        var wav = WavEncoder.Encode(new RecordedAudio(Pcm));

        Assert.Equal("RIFF", Encoding.ASCII.GetString(wav, 0, 4));
        Assert.Equal(36 + Pcm.Length, BinaryPrimitives.ReadInt32LittleEndian(wav.AsSpan(4)));
        Assert.Equal("WAVE", Encoding.ASCII.GetString(wav, 8, 4));
        Assert.Equal("fmt ", Encoding.ASCII.GetString(wav, 12, 4));
        Assert.Equal(16, BinaryPrimitives.ReadInt32LittleEndian(wav.AsSpan(16)));
        Assert.Equal(1, BinaryPrimitives.ReadInt16LittleEndian(wav.AsSpan(20)));
        Assert.Equal(1, BinaryPrimitives.ReadInt16LittleEndian(wav.AsSpan(22)));
        Assert.Equal(16_000, BinaryPrimitives.ReadInt32LittleEndian(wav.AsSpan(24)));
        Assert.Equal(32_000, BinaryPrimitives.ReadInt32LittleEndian(wav.AsSpan(28)));
        Assert.Equal(2, BinaryPrimitives.ReadInt16LittleEndian(wav.AsSpan(32)));
        Assert.Equal(16, BinaryPrimitives.ReadInt16LittleEndian(wav.AsSpan(34)));
        Assert.Equal("data", Encoding.ASCII.GetString(wav, 36, 4));
        Assert.Equal(Pcm.Length, BinaryPrimitives.ReadInt32LittleEndian(wav.AsSpan(40)));
    }

    [Fact]
    public void Encode_empty_audio_as_a_header_only_file()
    {
        var wav = WavEncoder.Encode(new RecordedAudio(ReadOnlyMemory<byte>.Empty));

        Assert.Equal(44, wav.Length);
        Assert.Equal(36, BinaryPrimitives.ReadInt32LittleEndian(wav.AsSpan(4)));
        Assert.Equal(0, BinaryPrimitives.ReadInt32LittleEndian(wav.AsSpan(40)));
    }

    [Fact]
    public void Not_modify_the_source_audio()
    {
        var source = Pcm.ToArray();

        WavEncoder.Encode(new RecordedAudio(source));

        Assert.Equal(Pcm, source);
    }
}
