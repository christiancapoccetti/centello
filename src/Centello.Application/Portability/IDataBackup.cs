namespace Centello.Application;

public interface IDataBackup
{
    /// <summary>Writes a standalone copy of every table to "centello-yyyy-MM-dd.backup" in the directory and
    /// stamps it with the time it was made.</summary>
    Task<Result<ExportedFile>> Create(string directory, CancellationToken cancellationToken = default);

    /// <summary>Checks a backup on a private copy, brings that copy to the current schema and compares it with the
    /// phone. The data of the phone is only read.</summary>
    Task<Result<RestorePreview>> Analyse(Stream backup, CancellationToken cancellationToken = default);

    /// <summary>Replaces every table of the phone with the copy <see cref="Analyse"/> prepared. All or nothing.</summary>
    Task<Result> Restore(RestorePreview preview, CancellationToken cancellationToken = default);

    void Discard(RestorePreview preview);
}
