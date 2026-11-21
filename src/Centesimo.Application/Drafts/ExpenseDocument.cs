namespace Centesimo.Application;

public sealed class ExpenseDocument
{
    private const string PdfMediaType = "application/pdf";

    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] Pdf = "%PDF-"u8.ToArray();

    private ExpenseDocument(ReadOnlyMemory<byte> bytes, string mediaType)
    {
        Bytes = bytes;
        MediaType = mediaType;
    }

    public ReadOnlyMemory<byte> Bytes { get; }

    public string MediaType { get; }

    public bool IsPdf => MediaType == PdfMediaType;

    public static Result<ExpenseDocument> Read(ReadOnlyMemory<byte> bytes)
    {
        var mediaType = MediaTypeOf(bytes.Span);
        if (mediaType is null)
            return Result<ExpenseDocument>.Failure(PhotoErrors.Unsupported);

        var limit = mediaType == PdfMediaType ? DocumentLimits.MaximumPdfBytes : PhotoLimits.MaximumBytes;
        if (bytes.Length > limit)
            return Result<ExpenseDocument>.Failure(mediaType == PdfMediaType ? PhotoErrors.PdfTooLarge : PhotoErrors.TooLarge);

        return Result<ExpenseDocument>.Success(new ExpenseDocument(bytes, mediaType));
    }

    private static string? MediaTypeOf(ReadOnlySpan<byte> bytes)
    {
        if (bytes.StartsWith(Jpeg))
            return "image/jpeg";

        if (bytes.StartsWith(Png))
            return "image/png";

        if (bytes.StartsWith(Pdf))
            return PdfMediaType;

        return IsWebp(bytes) ? "image/webp" : null;
    }

    private static bool IsWebp(ReadOnlySpan<byte> bytes) =>
        bytes.Length >= 12 && bytes[..4].SequenceEqual("RIFF"u8) && bytes[8..12].SequenceEqual("WEBP"u8);
}
