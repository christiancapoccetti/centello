using System.Runtime.Versioning;
using Android.Content;
using Android.Provider;
using Centesimo.Application;

namespace Centesimo.App;

/// <summary>Writes into the public Download folder through MediaStore, which needs no storage permission from API 29.
/// A second file with the same name gets a "(1)" suffix from MediaStore, so nothing already there is touched.</summary>
internal sealed class MediaStoreDownloads : IDownloads
{
    public bool CanSave => OperatingSystem.IsAndroidVersionAtLeast(29);

    public async Task<Result> Save(ExportedFile file, CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(29))
            return Result.Failure(PortabilityErrors.SaveFailed);

        return await Insert(file, cancellationToken);
    }

    [SupportedOSPlatform("android29.0")]
    private static async Task<Result> Insert(ExportedFile file, CancellationToken cancellationToken)
    {
        var resolver = Android.App.Application.Context.ContentResolver;
        if (resolver is null)
            return Result.Failure(PortabilityErrors.SaveFailed);

        var values = new ContentValues();
        values.Put(MediaStore.IMediaColumns.DisplayName, Path.GetFileName(file.FilePath));
        values.Put(MediaStore.IMediaColumns.MimeType, file.ContentType);
        values.Put(MediaStore.IMediaColumns.RelativePath, Android.OS.Environment.DirectoryDownloads);
        values.Put(MediaStore.IMediaColumns.IsPending, 1);

        var uri = resolver.Insert(MediaStore.Downloads.ExternalContentUri!, values);
        if (uri is null)
            return Result.Failure(PortabilityErrors.SaveFailed);

        try
        {
            await using (var target = resolver.OpenOutputStream(uri)
                ?? throw new IOException("MediaStore gave no output stream."))
            await using (var source = File.OpenRead(file.FilePath))
                await source.CopyToAsync(target, cancellationToken);

            var done = new ContentValues();
            done.Put(MediaStore.IMediaColumns.IsPending, 0);
            resolver.Update(uri, done, null, null);
            return Result.Success();
        }
        catch (Exception)
        {
            resolver.Delete(uri, null, null);
            return Result.Failure(PortabilityErrors.SaveFailed);
        }
    }
}
