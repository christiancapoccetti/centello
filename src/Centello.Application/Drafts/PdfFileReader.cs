namespace Centello.Application;

public static class PdfFileReader
{
    public static async Task<Result<byte[]?>> Read(Stream stream, CancellationToken cancellationToken = default)
    {
        using var output = new MemoryStream();
        var buffer = new byte[81920];

        while (true)
        {
            var remaining = DocumentLimits.MaximumPdfBytes + 1 - (int)output.Length;
            var read = await stream.ReadAsync(buffer.AsMemory(0, Math.Min(buffer.Length, remaining)), cancellationToken);
            if (read == 0)
                return Result<byte[]?>.Success(output.ToArray());

            if (read == remaining)
                return Result<byte[]?>.Failure(PhotoErrors.PdfTooLarge);

            output.Write(buffer, 0, read);
        }
    }
}
