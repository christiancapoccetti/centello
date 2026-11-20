namespace Centesimo.Application;

public interface IDataBackup
{
    /// <summary>Writes a standalone copy of every table to "centesimo-yyyy-MM-dd.backup" in the directory and
    /// stamps it with the time it was made.</summary>
    Task<Result<ExportedFile>> Create(string directory, CancellationToken cancellationToken = default);
}
