namespace Centesimo.Application;

public sealed class ExpenseDocument
{
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private ExpenseDocument(ReadOnlyMemory<byte> bytes, string mediaType)
    {
        Bytes = bytes;
        MediaType = mediaType;
    }

    public ReadOnlyMemory<byte> Bytes { get; }

    public string MediaType { get; }

    public static Result<ExpenseDocument> Read(ReadOnlyMemory<byte> bytes)
    {
        if (bytes.Length > PhotoLimits.MaximumBytes)
            return Result<ExpenseDocument>.Failure(PhotoErrors.TooLarge);

        var mediaType = MediaTypeOf(bytes.Span);
        if (mediaType is null)
            return Result<ExpenseDocument>.Failure(PhotoErrors.Unsupported);

        return Result<ExpenseDocument>.Success(new ExpenseDocument(bytes, mediaType));
    }

    private static string? MediaTypeOf(ReadOnlySpan<byte> bytes)
    {
        if (bytes.StartsWith(Jpeg))
            return "image/jpeg";

        if (bytes.StartsWith(Png))
            return "image/png";

        return IsWebp(bytes) ? "image/webp" : null;
    }

    private static bool IsWebp(ReadOnlySpan<byte> bytes) =>
        bytes.Length >= 12 && bytes[..4].SequenceEqual("RIFF"u8) && bytes[8..12].SequenceEqual("WEBP"u8);
}
