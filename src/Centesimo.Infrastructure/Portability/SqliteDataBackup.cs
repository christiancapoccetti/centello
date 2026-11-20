using System.Globalization;
using Centesimo.Application;
using Centesimo.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Centesimo.Infrastructure;

/// <summary>The only code besides EF that touches a database file. A backup is a VACUUM INTO copy, so it is a
/// standalone file whatever the journal mode of the live database, plus a one-row table that marks it as a
/// Centesimo backup and dates it. The table never reaches the live database: restoring checks a private copy, brings
/// it to the current schema, drops the mark and copies it into the live file with SQLite's online backup, so open
/// connections stay valid and a crash leaves the old data.</summary>
public sealed class SqliteDataBackup(CentesimoDbContext context, RecurringPaymentAutomation automation,
    Func<DateTimeOffset>? now = null, string stagingDirectory = "") : IDataBackup
{
    private const string BackupContentType = "application/octet-stream";
    private const string MarkTable = "CentesimoBackup";
    private const int NotADatabaseCode = 26;
    private const long MaximumBytes = 256L * 1024 * 1024;
    private static readonly byte[] SqliteHeader = "SQLite format 3\0"u8.ToArray();

    private readonly Func<DateTimeOffset> _now = now ?? (() => DateTimeOffset.Now);

    // On Android the temp folder is the app cache: Auto Backup skips it and the system may trim it.
    private readonly string _staging = stagingDirectory.HasValue()
        ? stagingDirectory
        : Path.Combine(Path.GetTempPath(), "centesimo-restore");

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

    public async Task<Result<RestorePreview>> Analyse(Stream backup, CancellationToken cancellationToken = default)
    {
        ClearStaging();
        var path = Path.Combine(_staging, $"{Guid.NewGuid():N}.db3");
        try
        {
            var result = await Check(backup, path, cancellationToken);
            if (result.IsFailure)
                DeleteStaged(path);

            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            DeleteStaged(path);
            throw;
        }
        catch (SqliteException exception) when (exception.SqliteErrorCode == NotADatabaseCode)
        {
            DeleteStaged(path);
            return Result<RestorePreview>.Failure(PortabilityErrors.NotABackup);
        }
        catch (Exception)
        {
            DeleteStaged(path);
            return Result<RestorePreview>.Failure(PortabilityErrors.Damaged);
        }
    }

    // The automation gate also runs the due pass on the new data before it opens again.
    public Task<Result> Restore(RestorePreview preview, CancellationToken cancellationToken = default) =>
        automation.Exclusive(() => Install(preview.StagedPath, cancellationToken));

    private async Task<Result<RestorePreview>> Check(Stream backup, string path, CancellationToken cancellationToken)
    {
        if (!await CopyWithLimit(backup, path, cancellationToken) || !await HasSqliteHeader(path, cancellationToken))
            return Result<RestorePreview>.Failure(PortabilityErrors.NotABackup);

        var mark = await Inspect(path, cancellationToken);
        if (mark.IsFailure)
            return Result<RestorePreview>.Failure(mark.Error);

        await using var staged = StagedContext(path);
        var applied = await staged.Database.GetAppliedMigrationsAsync(cancellationToken);
        if (!applied.Any())
            return Result<RestorePreview>.Failure(PortabilityErrors.NotABackup);

        if (applied.Except(staged.Database.GetMigrations()).Any())
            return Result<RestorePreview>.Failure(PortabilityErrors.NewerVersion);

        // An older backup reaches the current schema here, on the copy, before the phone is involved.
        await staged.Database.MigrateAsync(cancellationToken);
        if (!await Seal(staged, cancellationToken))
            return Result<RestorePreview>.Failure(PortabilityErrors.Damaged);

        var contents = await Summarize(staged, cancellationToken);
        var phone = await Summarize(context, cancellationToken);
        return Result<RestorePreview>.Success(new RestorePreview(path, mark.Value, contents, phone));
    }

    /// <summary>Checks a copy that has a SQLite header and removes the mark, so the live schema never carries it.</summary>
    private static async Task<Result<DateTimeOffset>> Inspect(string path, CancellationToken cancellationToken)
    {
        await using var connection = Open(path, SqliteOpenMode.ReadWrite);
        await connection.OpenAsync(cancellationToken);
        if (await Scalar(connection, "PRAGMA quick_check", cancellationToken) is not "ok")
            return Result<DateTimeOffset>.Failure(PortabilityErrors.Damaged);

        // Centesimo files have none, and a planted trigger would otherwise reach the live database.
        if (await Count(connection, "type IN ('trigger', 'view')", cancellationToken) > 0)
            return Result<DateTimeOffset>.Failure(PortabilityErrors.NotABackup);

        if (await Count(connection, $"type = 'table' AND name = '{MarkTable}'", cancellationToken) == 0)
            return Result<DateTimeOffset>.Failure(PortabilityErrors.NotABackup);

        var stamp = await Scalar(connection, $"SELECT CreatedAt FROM {MarkTable} LIMIT 1", cancellationToken);
        if (!DateTimeOffset.TryParseExact(stamp as string, "O", CultureInfo.InvariantCulture, DateTimeStyles.None,
                out var createdAt))
            return Result<DateTimeOffset>.Failure(PortabilityErrors.NotABackup);

        await Execute(connection, $"DROP TABLE {MarkTable}", cancellationToken);
        return Result<DateTimeOffset>.Success(createdAt);
    }

    /// <summary>Leaves the migrated copy a standalone file, so it opens read-only and is deleted whole, and tells
    /// whether its rows still agree with its foreign keys.</summary>
    private static async Task<bool> Seal(CentesimoDbContext staged, CancellationToken cancellationToken)
    {
        await staged.Database.CloseConnectionAsync();
        await using var connection = NonPooled(staged.Database.GetConnectionString()!);
        await connection.OpenAsync(cancellationToken);
        if (await Scalar(connection, "PRAGMA foreign_key_check", cancellationToken) is not null)
            return false;

        await Execute(connection, "PRAGMA journal_mode = DELETE", cancellationToken);
        return true;
    }

    /// <summary>The same count for both sides of the comparison. Sequential: one DbContext runs one query at a
    /// time.</summary>
    private static async Task<DataSummary> Summarize(CentesimoDbContext database, CancellationToken cancellationToken)
    {
        var expenseIds = await database.Expenses
            .AsNoTracking()
            .Select(expense => expense.ExpenseId)
            .ToListAsync(cancellationToken);
        return new DataSummary(
            expenseIds.ToHashSet(),
            await database.Categories.CountAsync(cancellationToken),
            await database.Tags.CountAsync(cancellationToken),
            await database.RecurringPayments.CountAsync(cancellationToken));
    }

    private async Task<Result> Install(string stagedPath, CancellationToken cancellationToken)
    {
        // Only a copy Analyse checked may replace the phone's data.
        if (!IsStaged(stagedPath) || !File.Exists(stagedPath))
            return Result.Failure(PortabilityErrors.RestoreFailed);

        try
        {
            await using var source = Open(stagedPath, SqliteOpenMode.ReadOnly);
            await using var live = Open(LivePath(), SqliteOpenMode.ReadWrite);
            await source.OpenAsync(cancellationToken);
            await live.OpenAsync(cancellationToken);
            // A query still finishing on another connection holds a shared lock: wait for it instead of failing.
            await Execute(live, "PRAGMA busy_timeout = 10000", cancellationToken);
            // One write transaction on the live file: the pooled handles see the new pages, and a process that dies
            // first leaves a hot journal that restores the old ones.
            source.BackupDatabase(live);
        }
        catch (SqliteException)
        {
            // The staged copy stays, so the button can be pressed again.
            return Result.Failure(PortabilityErrors.RestoreFailed);
        }

        DeleteStaged(stagedPath);
        return Result.Success();
    }

    private static async Task Stamp(string path, DateTimeOffset createdAt, CancellationToken cancellationToken)
    {
        await using var connection = Open(path);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE TABLE {MarkTable} (CreatedAt TEXT NOT NULL);"
            + $"INSERT INTO {MarkTable} (CreatedAt) VALUES ($createdAt)";
        command.Parameters.AddWithValue("$createdAt", createdAt.ToString("O", CultureInfo.InvariantCulture));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    // Derived from the context, so the file replaced is always the one the app reads.
    private string LivePath() => new SqliteConnectionStringBuilder(context.Database.GetConnectionString()).DataSource;

    // Non-pooled: no handle outlives the operation, so the copy can be deleted at once.
    private static SqliteConnection Open(string path, SqliteOpenMode mode = SqliteOpenMode.ReadWriteCreate) =>
        NonPooled(new SqliteConnectionStringBuilder { DataSource = path, Mode = mode }.ToString());

    private static SqliteConnection NonPooled(string connectionString) =>
        new(new SqliteConnectionStringBuilder(connectionString) { Pooling = false }.ToString());

    private static CentesimoDbContext StagedContext(string path) => new(new DbContextOptionsBuilder<CentesimoDbContext>()
        .UseSqlite(new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString())
        .Options);

    private static async Task<long> Count(SqliteConnection connection, string condition,
        CancellationToken cancellationToken) =>
        Convert.ToInt64(await Scalar(connection, $"SELECT COUNT(*) FROM sqlite_master WHERE {condition}",
            cancellationToken));

    private static async Task<object?> Scalar(SqliteConnection connection, string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return await command.ExecuteScalarAsync(cancellationToken);
    }

    private static async Task Execute(SqliteConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private bool IsStaged(string path) => Path.GetFullPath(path)
        .StartsWith(Path.GetFullPath(_staging) + Path.DirectorySeparatorChar, StringComparison.Ordinal);

    // At most one staged copy: the ones an abandoned restore left behind go here.
    private void ClearStaging()
    {
        Directory.CreateDirectory(_staging);
        foreach (var file in Directory.EnumerateFiles(_staging))
            DeleteQuietly(file);
    }

    private static async Task<bool> CopyWithLimit(Stream source, string path, CancellationToken cancellationToken)
    {
        await using var destination = File.Create(path);
        var buffer = new byte[81920];
        long copied = 0;
        int read;
        while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            copied += read;
            if (copied > MaximumBytes)
                return false;

            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        return true;
    }

    private static async Task<bool> HasSqliteHeader(string path, CancellationToken cancellationToken)
    {
        var actual = new byte[SqliteHeader.Length];
        await using var stream = File.OpenRead(path);
        return await stream.ReadAtLeastAsync(actual, actual.Length, false, cancellationToken) == actual.Length
            && actual.SequenceEqual(SqliteHeader);
    }

    private static void DeleteStaged(string path)
    {
        DeleteQuietly(path);
        DeleteQuietly(path + "-wal");
        DeleteQuietly(path + "-shm");
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
