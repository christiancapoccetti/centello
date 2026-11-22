using System.Text;

namespace Centello.Application.Tests;

public sealed class ExpenseDocument_should_expected_behavior
{
    private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF, 0xE0];
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private static readonly byte[] PdfSignature = "%PDF-1.7"u8.ToArray();

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
        var result = ExpenseDocument.Read(JpegSignature);

        Assert.True(result.IsSuccess);
        Assert.Equal("image/jpeg", result.Value.MediaType);
        Assert.Equal(JpegSignature, result.Value.Bytes.ToArray());
    }

    [Fact]
    public void Read_a_png()
    {
        var result = ExpenseDocument.Read(PngSignature);

        Assert.True(result.IsSuccess);
        Assert.Equal("image/png", result.Value.MediaType);
    }

    [Fact]
    public void Read_a_webp()
    {
        var result = ExpenseDocument.Read(Webp());

        Assert.True(result.IsSuccess);
        Assert.Equal("image/webp", result.Value.MediaType);
    }

    [Fact]
    public void Refuse_an_empty_file()
    {
        var result = ExpenseDocument.Read(ReadOnlyMemory<byte>.Empty);

        Assert.True(result.IsFailure);
        Assert.Equal(PhotoErrors.Unsupported, result.Error);
    }

    [Fact]
    public void Refuse_a_heic()
    {
        byte[] heic = [0x00, 0x00, 0x00, 0x18, .. Ascii("ftypheic")];

        var result = ExpenseDocument.Read(heic);

        Assert.Equal(PhotoErrors.Unsupported, result.Error);
    }

    [Fact]
    public void Read_a_pdf()
    {
        var result = ExpenseDocument.Read(PdfSignature);

        Assert.True(result.IsSuccess);
        Assert.Equal("application/pdf", result.Value.MediaType);
        Assert.True(result.Value.IsPdf);
        Assert.Equal(PdfSignature, result.Value.Bytes.ToArray());
    }

    [Fact]
    public void Not_take_an_image_for_a_pdf()
    {
        Assert.False(ExpenseDocument.Read(JpegSignature).Value.IsPdf);
    }

    [Fact]
    public void Refuse_text_that_only_looks_like_a_pdf()
    {
        var result = ExpenseDocument.Read(Ascii("%PDF"));

        Assert.Equal(PhotoErrors.Unsupported, result.Error);
    }

    [Fact]
    public void Refuse_a_pdf_over_the_pdf_size_limit()
    {
        var result = ExpenseDocument.Read(Padded(PdfSignature, DocumentLimits.MaximumPdfBytes + 1));

        Assert.True(result.IsFailure);
        Assert.Equal(PhotoErrors.PdfTooLarge, result.Error);
        Assert.Contains("PDF", result.Error.Message);
    }

    [Fact]
    public void Accept_a_pdf_of_exactly_the_pdf_size_limit()
    {
        var result = ExpenseDocument.Read(Padded(PdfSignature, DocumentLimits.MaximumPdfBytes));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Keep_the_photo_limit_for_images_even_when_a_pdf_may_be_larger()
    {
        var result = ExpenseDocument.Read(Padded(PngSignature, PhotoLimits.MaximumBytes + 1));

        Assert.Equal(PhotoErrors.TooLarge, result.Error);
    }

    [Fact]
    public void Refuse_a_riff_file_that_is_not_a_webp()
    {
        byte[] wave = [.. Ascii("RIFF"), 0x24, 0x00, 0x00, 0x00, .. Ascii("WAVE")];

        var result = ExpenseDocument.Read(wave);

        Assert.Equal(PhotoErrors.Unsupported, result.Error);
    }

    [Fact]
    public void Refuse_a_photo_over_the_size_limit()
    {
        var result = ExpenseDocument.Read(Padded(JpegSignature, PhotoLimits.MaximumBytes + 1));

        Assert.True(result.IsFailure);
        Assert.Equal(PhotoErrors.TooLarge, result.Error);
    }

    [Fact]
    public void Accept_a_photo_of_exactly_the_size_limit()
    {
        var result = ExpenseDocument.Read(Padded(JpegSignature, PhotoLimits.MaximumBytes));

        Assert.True(result.IsSuccess);
    }
}
