using System.Globalization;
using Centesimo.Application;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Centesimo.Infrastructure;

#pragma warning disable CS9113 // automation and stagingDirectory belong to Analyse and Restore, which share this constructor.

/// <summary>The only code besides EF that touches a database file. A backup is a VACUUM INTO copy, so it is a
/// standalone file whatever the journal mode of the live database, plus a one-row table that marks it as a
/// Centesimo backup and dates it. The table never reaches the live database.</summary>
public sealed class SqliteDataBackup(CentesimoDbContext context, RecurringPaymentAutomation automation,
    Func<DateTimeOffset>? now = null, string stagingDirectory = "") : IDataBackup
{
    private const string BackupContentType = "application/octet-stream";
    private readonly Func<DateTimeOffset> _now = now ?? (() => DateTimeOffset.Now);

    public async Task<Result<ExportedFile>> Create(string directory, CancellationToken cancellationToken = default)
    {
        var createdAt = _now();
        var day = createdAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var path = Path.Combine(directory, $"centesimo-{day}.backup");
        var temporaryPath = path + ".tmp";
        try
        {
            Directory.CreateDirectory(directory);
            File.Delete(temporaryPath);
            await context.Database.ExecuteSqlAsync($"VACUUM INTO {temporaryPath}", cancellationToken);
            await Stamp(temporaryPath, createdAt, cancellationToken);
            File.Move(temporaryPath, path, overwrite: true);
            return Result<ExportedFile>.Success(new ExportedFile(path, BackupContentType));
        }
        catch (Exception exception) when (exception is not OperationCanceledException
            || !cancellationToken.IsCancellationRequested)
        {
            return Result<ExportedFile>.Failure(PortabilityErrors.BackupFailed);
        }
        finally
        {
            DeleteQuietly(temporaryPath);
        }
    }

    private static async Task Stamp(string path, DateTimeOffset createdAt, CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection($"Data Source={path};Pooling=False");
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE CentesimoBackup (CreatedAt TEXT NOT NULL);"
            + "INSERT INTO CentesimoBackup (CreatedAt) VALUES ($createdAt)";
        command.Parameters.AddWithValue("$createdAt", createdAt.ToString("O", CultureInfo.InvariantCulture));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void DeleteQuietly(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
