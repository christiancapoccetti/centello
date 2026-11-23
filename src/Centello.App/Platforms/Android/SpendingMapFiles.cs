using Centello.Application;

namespace Centello.App;

internal sealed class SpendingMapFiles(IDownloads downloads)
{
    public async Task<Result<string>> Write(byte[] png, string name, bool share)
    {
        string? directory = null;
        var keepForReceiver = false;
        try
        {
            directory = ExportCache.NewShareDirectory();
            Directory.CreateDirectory(directory);
            var file = new ExportedFile(Path.Combine(directory, name), "image/png");
            await File.WriteAllBytesAsync(file.FilePath, png);
            if (share)
            {
                await Share.Default.RequestAsync(new ShareFileRequest("Condividi il riepilogo", new ShareFile(file.FilePath, file.ContentType)));
                keepForReceiver = true;
                return Result<string>.Success("");
            }

            if (!downloads.CanSave)
                return await SaveDocument(file);

            var result = await downloads.Save(file);
            if (result.IsFailure)
                return Result<string>.Failure(result.Error);

            return Result<string>.Success($"Salvato in Download: {name}");
        }
        catch (Exception)
        {
            return Result<string>.Failure(PortabilityErrors.SaveFailed);
        }
        finally
        {
            if (!keepForReceiver && directory is not null)
                ExportCache.Delete(directory);
        }
    }

    private static async Task<Result<string>> SaveDocument(ExportedFile file)
    {
        if (Platform.CurrentActivity is not MainActivity activity || activity.ContentResolver is not { } resolver)
            return Result<string>.Failure(PortabilityErrors.SaveFailed);

        using var uri = await activity.ChooseSaveLocation(file);
        if (uri is null)
            return Result<string>.Success("");

        await using var target = resolver.OpenOutputStream(uri);
        if (target is null)
            return Result<string>.Failure(PortabilityErrors.SaveFailed);

        await using var source = File.OpenRead(file.FilePath);
        await source.CopyToAsync(target);
        return Result<string>.Success("Immagine salvata.");
    }
}
