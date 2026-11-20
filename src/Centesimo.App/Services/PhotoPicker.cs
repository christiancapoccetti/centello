using Centesimo.Application;
using Centesimo.Domain;

namespace Centesimo.App;

/// <summary>
/// Opens the camera or the gallery and hands back the photo as bytes, scaled, upright and without metadata (so no
/// GPS position). Nothing outlives the call: every image in the app cache is deleted before and after, which also
/// removes what a run killed while the camera app was in front left behind.
/// </summary>
public sealed class PhotoPicker
{
    private static readonly MediaPickerOptions Options = new()
    {
        MaximumWidth = PhotoLimits.MaximumDimension,
        MaximumHeight = PhotoLimits.MaximumDimension,
        CompressionQuality = PhotoLimits.JpegQuality,
        RotateImage = true,
        PreserveMetaData = false,
        SelectionLimit = 1
    };

    // Wider than what ExpensePhoto accepts on purpose: a camera app may leave a HEIC original next to the JPEG copy.
    private static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png", ".webp", ".heic", ".heif"];

    /// <returns>The photo, or null when the person backed out.</returns>
    public async Task<Result<byte[]?>> Capture()
    {
        if (!MediaPicker.Default.IsCaptureSupported)
            return Result<byte[]?>.Failure(PhotoErrors.CameraUnavailable);

        return await Take(() => MediaPicker.Default.CapturePhotoAsync(Options));
    }

    /// <returns>The photo, or null when the person backed out.</returns>
    public Task<Result<byte[]?>> Pick() => Take(PickFirst);

    private static async Task<FileResult?> PickFirst() =>
        (await MediaPicker.Default.PickPhotosAsync(Options)).FirstOrDefault();

    private static async Task<Result<byte[]?>> Take(Func<Task<FileResult?>> open)
    {
        DeleteLeftovers();
        try
        {
            var file = await open();
            return Result<byte[]?>.Success(file is null ? null : await ReadAll(file));
        }
        catch (OperationCanceledException)
        {
            return Result<byte[]?>.Success(null);
        }
        catch (FeatureNotSupportedException)
        {
            return Result<byte[]?>.Failure(PhotoErrors.CameraUnavailable);
        }
        catch (PermissionException)
        {
            return Result<byte[]?>.Failure(PhotoErrors.CameraDenied);
        }
        catch
        {
            return Result<byte[]?>.Failure(PhotoErrors.Unavailable);
        }
        finally
        {
            DeleteLeftovers();
        }
    }

    private static async Task<byte[]> ReadAll(FileResult file)
    {
        if (file.FullPath.HasValue())
            return await File.ReadAllBytesAsync(file.FullPath);

        await using var stream = await file.OpenReadAsync();
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);
        return buffer.ToArray();
    }

    // Only the cache is swept: a picked file can be the person's own gallery photo and must never be deleted.
    private static void DeleteLeftovers()
    {
        try
        {
            var images = Directory
                .EnumerateFiles(FileSystem.CacheDirectory, "*", SearchOption.AllDirectories)
                .Where(path => ImageExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase));
            foreach (var image in images)
                Delete(image);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static void Delete(string path)
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
