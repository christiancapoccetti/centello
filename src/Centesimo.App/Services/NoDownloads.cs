using Centesimo.Application;

namespace Centesimo.App;

public interface IDownloads
{
    bool CanSave { get; }

    /// <summary>Copies the file into the public Download folder of the phone, where the person finds it
    /// without a share sheet.</summary>
    Task<Result> Save(ExportedFile file, CancellationToken cancellationToken = default);
}

/// <summary>For platforms and Android versions that cannot write to Download: the file is shared instead.</summary>
public sealed class NoDownloads : IDownloads
{
    public bool CanSave => false;

    public async Task<Result> Save(ExportedFile file, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        return Result.Failure(PortabilityErrors.SaveFailed);
    }
}
