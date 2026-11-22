using Centello.Application;
using Centello.Domain;

namespace Centello.App;

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

    private static readonly PickOptions PdfOptions = new()
    {
        FileTypes = FilePickerFileType.Pdf,
        PickerTitle = "Scegli un PDF"
    };

    // Wider than what ExpenseDocument accepts on purpose: a camera app may leave a HEIC original next to the JPEG copy.
    private static readonly string[] LeftoverExtensions = [".jpg", ".jpeg", ".png", ".webp", ".heic", ".heif", ".pdf"];

    // Below Android 13 MediaPicker.CapturePhotoAsync demands WRITE_EXTERNAL_STORAGE. Declaring it would make
    // MediaPicker rotate and compress a picked gallery photo in place, so the camera is not offered there.
    public static bool CanCapture => OperatingSystem.IsAndroidVersionAtLeast(33);

    public const string CameraRationale = "Centello usa la fotocamera per fotografare una spesa, come uno scontrino o una fattura. Invia la foto a OpenAI per leggerla. La copia sul telefono è temporanea.";

    public async Task<Result<byte[]?>> Capture()
    {
        if (!CanCapture || !MediaPicker.Default.IsCaptureSupported)
            return Result<byte[]?>.Failure(PhotoErrors.CameraUnavailable);

        return await Take(() => MediaPicker.Default.CapturePhotoAsync(Options));
    }

    public Task<Result<byte[]?>> Pick() => Take(PickFirst);

    public Task<Result<byte[]?>> PickPdf() => Take(() => FilePicker.Default.PickAsync(PdfOptions), readPdf: true);

    private static async Task<FileResult?> PickFirst() =>
        (await MediaPicker.Default.PickPhotosAsync(Options)).FirstOrDefault();

    private static async Task<Result<byte[]?>> Take(Func<Task<FileResult?>> open, bool readPdf = false)
    {
        // Android can kill the app while the camera app is in front, so a previous photo may still be in the cache.
        DeleteLeftovers();
        try
        {
            var file = await open();
            if (file is null)
                return Result<byte[]?>.Success(null);

            if (!readPdf)
                return Result<byte[]?>.Success(await ReadAll(file));

            await using var stream = await file.OpenReadAsync();
            return await PdfFileReader.Read(stream);
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

    // The pickers copy what the person chooses into the app cache and hand back that copy, so sweeping the cache never
    // touches the person's own photo or PDF. Nothing outside it may be deleted.
    private static void DeleteLeftovers()
    {
        try
        {
            var leftovers = Directory
                .EnumerateFiles(FileSystem.CacheDirectory, "*", SearchOption.AllDirectories)
                .Where(path => LeftoverExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase));
            foreach (var leftover in leftovers)
                Delete(leftover);
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
