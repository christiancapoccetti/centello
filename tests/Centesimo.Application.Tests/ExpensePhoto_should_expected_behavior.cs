using System.Text;

namespace Centesimo.Application.Tests;

public sealed class ExpensePhoto_should_expected_behavior
{
    private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF, 0xE0];
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private static byte[] Ascii(string text) => Encoding.ASCII.GetBytes(text);

    private static byte[] Webp() =>
        [.. Ascii("RIFF"), 0x24, 0x00, 0x00, 0x00, .. Ascii("WEBPVP8 ")];

    private static byte[] Padded(byte[] signature, int length)
    {
        var bytes = new byte[length];
        signature.CopyTo(bytes, 0);
        return bytes;
    }

    [Fact]
    public void Read_a_jpeg()
    {
        var result = ExpensePhoto.Read(JpegSignature);

        Assert.True(result.IsSuccess);
        Assert.Equal("image/jpeg", result.Value.MediaType);
        Assert.Equal(JpegSignature, result.Value.Bytes.ToArray());
    }

    [Fact]
    public void Read_a_png()
    {
        var result = ExpensePhoto.Read(PngSignature);

        Assert.True(result.IsSuccess);
        Assert.Equal("image/png", result.Value.MediaType);
    }

    [Fact]
    public void Read_a_webp()
    {
        var result = ExpensePhoto.Read(Webp());

        Assert.True(result.IsSuccess);
        Assert.Equal("image/webp", result.Value.MediaType);
    }

    [Fact]
    public void Refuse_an_empty_file()
    {
        var result = ExpensePhoto.Read(ReadOnlyMemory<byte>.Empty);

        Assert.True(result.IsFailure);
        Assert.Equal(PhotoErrors.Unsupported, result.Error);
    }

    [Fact]
    public void Refuse_a_heic()
    {
        byte[] heic = [0x00, 0x00, 0x00, 0x18, .. Ascii("ftypheic")];

        var result = ExpensePhoto.Read(heic);

        Assert.Equal(PhotoErrors.Unsupported, result.Error);
    }

    [Fact]
    public void Refuse_a_pdf()
    {
        var result = ExpensePhoto.Read(Ascii("%PDF-1.7"));

        Assert.Equal(PhotoErrors.Unsupported, result.Error);
    }

    [Fact]
    public void Refuse_a_riff_file_that_is_not_a_webp()
    {
        byte[] wave = [.. Ascii("RIFF"), 0x24, 0x00, 0x00, 0x00, .. Ascii("WAVE")];

        var result = ExpensePhoto.Read(wave);

        Assert.Equal(PhotoErrors.Unsupported, result.Error);
    }

    [Fact]
    public void Refuse_a_photo_over_the_size_limit()
    {
        var result = ExpensePhoto.Read(Padded(JpegSignature, PhotoLimits.MaximumBytes + 1));

        Assert.True(result.IsFailure);
        Assert.Equal(PhotoErrors.TooLarge, result.Error);
    }

    [Fact]
    public void Accept_a_photo_of_exactly_the_size_limit()
    {
        var result = ExpensePhoto.Read(Padded(JpegSignature, PhotoLimits.MaximumBytes));

        Assert.True(result.IsSuccess);
    }
}
