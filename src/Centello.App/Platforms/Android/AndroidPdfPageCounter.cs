using Android.Graphics.Pdf;
using Android.OS;
using Centello.Application;

namespace Centello.App;

internal sealed class AndroidPdfPageCounter : IPdfPageCounter
{
    public Task<Result<int>> Count(ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken) =>
        Task.Run(() => Count(bytes), cancellationToken);

    private static Result<int> Count(ReadOnlyMemory<byte> bytes)
    {
        var path = Path.Combine(FileSystem.CacheDirectory, $"{Guid.NewGuid():N}.tmp");
        try
        {
            // Keep the descriptor seekable and unlink its path before writing receipt data.
            using var output = File.Open(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Delete);
            using var descriptor = ParcelFileDescriptor.FromFd(output.SafeFileHandle.DangerousGetHandle().ToInt32());
            File.Delete(path);
            output.Write(bytes.Span);
            output.Flush();
            output.Position = 0;

            PdfRenderer? renderer = null;
            try
            {
                renderer = new PdfRenderer(descriptor!);
                return Result<int>.Success(renderer.PageCount);
            }
            finally
            {
                if (renderer is null)
                    descriptor?.Close();
                else
                {
                    renderer.Close();
                    renderer.Dispose();
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or Java.IO.IOException
            or Java.Lang.IllegalArgumentException or Java.Lang.SecurityException)
        {
            return Result<int>.Failure(PhotoErrors.ProcessingFailed);
        }
        finally
        {
            TryDelete(path);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }
}
