using Centesimo.Application;
using Centesimo.Domain;

namespace Centesimo.App;

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

    // Below Android 13 MediaPicker.CapturePhotoAsync demands WRITE_EXTERNAL_STORAGE. Declaring it would make
    // MediaPicker rotate and compress a picked gallery photo in place, so the camera is not offered there.
    public static bool CanCapture => OperatingSystem.IsAndroidVersionAtLeast(33);

    public async Task<Result<byte[]?>> Capture()
    {
        if (!CanCapture || !MediaPicker.Default.IsCaptureSupported)
            return Result<byte[]?>.Failure(PhotoErrors.CameraUnavailable);

        return await Take(() => MediaPicker.Default.CapturePhotoAsync(Options));
    }

    public Task<Result<byte[]?>> Pick() => Take(PickFirst);

    private static async Task<FileResult?> PickFirst() =>
        (await MediaPicker.Default.PickPhotosAsync(Options)).FirstOrDefault();

    private static async Task<Result<byte[]?>> Take(Func<Task<FileResult?>> open)
    {
        // Android can kill the app while the camera app is in front, so a previous photo may still be in the cache.
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
