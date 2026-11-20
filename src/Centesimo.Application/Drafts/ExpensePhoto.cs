namespace Centesimo.Application;

/// <summary>
/// An image OpenAI accepts, with its media type read from its first bytes rather than from a file name. Only
/// <see cref="Read"/> makes one, so an empty, oversized or non-image file never reaches a request.
/// </summary>
public sealed class ExpensePhoto
{
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private ExpensePhoto(ReadOnlyMemory<byte> bytes, string mediaType)
    {
        Bytes = bytes;
        MediaType = mediaType;
    }

    public ReadOnlyMemory<byte> Bytes { get; }

    /// <summary><c>image/jpeg</c>, <c>image/png</c> or <c>image/webp</c>.</summary>
    public string MediaType { get; }

    public static Result<ExpensePhoto> Read(ReadOnlyMemory<byte> bytes)
    {
        if (bytes.Length > PhotoLimits.MaximumBytes)
            return Result<ExpensePhoto>.Failure(PhotoErrors.TooLarge);

        var mediaType = MediaTypeOf(bytes.Span);
        if (mediaType is null)
            return Result<ExpensePhoto>.Failure(PhotoErrors.Unsupported);

        return Result<ExpensePhoto>.Success(new ExpensePhoto(bytes, mediaType));
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
