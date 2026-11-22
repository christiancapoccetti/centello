using System.Buffers.Binary;
using Centello.Application;

namespace Centello.Infrastructure;

/// <summary>Wraps raw PCM 16-bit mono audio in a minimal RIFF/WAVE container.</summary>
public static class WavEncoder
{
    private const int HeaderSize = 44;
    private const short ChannelCount = 1;
    private const short BitsPerSample = 16;
    private const short BlockAlign = ChannelCount * BitsPerSample / 8;
    private const int PcmFormatChunkSize = 16;
    private const short PcmFormat = 1;

    public static byte[] Encode(RecordedAudio audio)
    {
        var pcm = audio.Pcm16.Span;
        var wav = new byte[HeaderSize + pcm.Length];
        var header = wav.AsSpan(0, HeaderSize);
        "RIFF"u8.CopyTo(header);
        BinaryPrimitives.WriteInt32LittleEndian(header[4..], HeaderSize - 8 + pcm.Length);
        "WAVEfmt "u8.CopyTo(header[8..]);
        BinaryPrimitives.WriteInt32LittleEndian(header[16..], PcmFormatChunkSize);
        BinaryPrimitives.WriteInt16LittleEndian(header[20..], PcmFormat);
        BinaryPrimitives.WriteInt16LittleEndian(header[22..], ChannelCount);
        BinaryPrimitives.WriteInt32LittleEndian(header[24..], SpeechRecordingLimits.SampleRate);
        BinaryPrimitives.WriteInt32LittleEndian(header[28..], SpeechRecordingLimits.SampleRate * BlockAlign);
        BinaryPrimitives.WriteInt16LittleEndian(header[32..], BlockAlign);
        BinaryPrimitives.WriteInt16LittleEndian(header[34..], BitsPerSample);
        "data"u8.CopyTo(header[36..]);
        BinaryPrimitives.WriteInt32LittleEndian(header[40..], pcm.Length);
        pcm.CopyTo(wav.AsSpan(HeaderSize));
        return wav;
    }
}
