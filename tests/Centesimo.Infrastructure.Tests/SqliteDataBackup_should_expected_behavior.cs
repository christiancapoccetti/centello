using System.Globalization;
using Centesimo.Application;
using Centesimo.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Centesimo.Infrastructure.Tests;

public sealed class SqliteDataBackup_should_expected_behavior : IDisposable
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 11, 12, 21, 40, 5, TimeSpan.FromHours(1));

    private readonly TestFileDatabase _database = new();
    private readonly ServiceProvider _services = new ServiceCollection().BuildServiceProvider();
    private string ExportDirectory => Path.Combine(_database.Folder, "exports");
    private string Staging => Path.Combine(_database.Folder, "staging");

    public SqliteDataBackup_should_expected_behavior()
    {
        var category = new Category(Guid.NewGuid(), "Cibo", "cart", "mint");
        var tag = new Tag(Guid.NewGuid(), category.CategoryId, "Pranzo");
        _database.Context.AddRange(category, tag,
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(1_250), new DateOnly(2026, 11, 5), tag.TagId),
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(300), new DateOnly(2026, 11, 6)),
            new RecurringPayment(Guid.NewGuid(), category.CategoryId, new Money(900),
                RecurrenceFrequency.Monthly, new DateOnly(2026, 12, 1)));
        _database.Context.SaveChanges();
    }

    public void Dispose()
    {
        _services.Dispose();
        _database.Dispose();
    }

    private SqliteDataBackup Backup(DateTimeOffset? now = null) =>
        new(_database.Context,
            new RecurringPaymentAutomation(_services.GetRequiredService<IServiceScopeFactory>(),
                new NoOpRecurringPaymentReminder()),
            () => now ?? CreatedAt,
            Staging);

    private static SqliteConnection Open(string path, string mode = "ReadOnly")
    {
        var connection = new SqliteConnection($"Data Source={path};Mode={mode};Pooling=False");
        connection.Open();
        return connection;
    }

    private static T Scalar<T>(string path, string sql)
    {
        using var connection = Open(path);
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return (T)Convert.ChangeType(command.ExecuteScalar()!, typeof(T), CultureInfo.InvariantCulture);
    }

    private static List<string> Tables(string path)
    {
        using var connection = Open(path);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%'";
        using var reader = command.ExecuteReader();
        var names = new List<string>();
        while (reader.Read())
            names.Add(reader.GetString(0));

        return names;
    }

    [Fact]
    public async Task Name_the_file_after_the_day_it_is_made()
    {
        var result = await Backup().Create(ExportDirectory);

        Assert.True(result.IsSuccess);
        Assert.Equal(Path.Combine(ExportDirectory, "centesimo-2026-11-12.backup"), result.Value.FilePath);
        Assert.Equal("application/octet-stream", result.Value.ContentType);
        Assert.True(File.Exists(result.Value.FilePath));
    }

    [Fact]
    public async Task Copy_every_row_of_every_table()
    {
        var result = await Backup().Create(ExportDirectory);

        var tables = Tables(_database.FilePath);
        Assert.Contains("Expenses", tables);
        foreach (var table in tables)
            Assert.Equal(
                Scalar<long>(_database.FilePath, $"SELECT COUNT(*) FROM \"{table}\""),
                Scalar<long>(result.Value.FilePath, $"SELECT COUNT(*) FROM \"{table}\""));

        Assert.Equal(2, Scalar<long>(result.Value.FilePath, "SELECT COUNT(*) FROM Expenses"));
    }

    [Fact]
    public async Task Stamp_the_backup_with_the_time_it_was_made()
    {
        var result = await Backup().Create(ExportDirectory);

        var stamp = Scalar<string>(result.Value.FilePath, "SELECT CreatedAt FROM CentesimoBackup");
        Assert.Equal(CreatedAt, DateTimeOffset.ParseExact(stamp, "O", CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task Replace_the_backup_made_earlier_the_same_day()
    {
        await Backup(CreatedAt).Create(ExportDirectory);

        var result = await Backup(CreatedAt.AddHours(2)).Create(ExportDirectory);

        Assert.True(result.IsSuccess);
        Assert.Single(Directory.GetFiles(ExportDirectory));
        var stamp = Scalar<string>(result.Value.FilePath, "SELECT CreatedAt FROM CentesimoBackup");
        Assert.Equal(CreatedAt.AddHours(2), DateTimeOffset.ParseExact(stamp, "O", CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task Leave_the_live_database_unmarked()
    {
        await Backup().Create(ExportDirectory);

        Assert.DoesNotContain("CentesimoBackup", Tables(_database.FilePath));
    }

    [Fact]
    public async Task Leave_no_temporary_file_after_a_success()
    {
        var result = await Backup().Create(ExportDirectory);

        Assert.Equal([result.Value.FilePath], Directory.GetFiles(ExportDirectory));
    }

    [Fact]
    public async Task Fail_and_leave_no_temporary_file_when_the_backup_cannot_be_put_in_place()
    {
        Directory.CreateDirectory(Path.Combine(ExportDirectory, "centesimo-2026-11-12.backup"));

        var result = await Backup().Create(ExportDirectory);

        Assert.True(result.IsFailure);
        Assert.Equal(PortabilityErrors.BackupFailed, result.Error);
        Assert.Empty(Directory.GetFiles(ExportDirectory));
    }

    [Fact]
    public async Task Leave_no_temporary_file_when_cancelled()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Backup().Create(ExportDirectory, cancellation.Token));

        Assert.Empty(Directory.GetFiles(ExportDirectory));
    }

    [Fact]
    public async Task Open_standalone_in_delete_journal_mode_with_an_intact_structure()
    {
        var result = await Backup().Create(ExportDirectory);

        Assert.Equal("delete", Scalar<string>(result.Value.FilePath, "PRAGMA journal_mode"));
        Assert.Equal("ok", Scalar<string>(result.Value.FilePath, "PRAGMA integrity_check"));
    }

    [Fact]
    public async Task Accept_a_directory_name_with_a_quote_and_a_space()
    {
        var directory = Path.Combine(_database.Folder, "it's here");

        var result = await Backup().Create(directory);

        Assert.True(result.IsSuccess);
        Assert.StartsWith(directory, result.Value.FilePath);
    }

    private async Task<string> CreateBackupFile()
    {
        var result = await Backup().Create(ExportDirectory);
        Assert.True(result.IsSuccess);
        return result.Value.FilePath;
    }

    private async Task<Result<RestorePreview>> Analyse(string path)
    {
        await using var stream = File.OpenRead(path);
        return await Backup().Analyse(stream);
    }

    private string CopyOf(string path)
    {
        var copy = Path.Combine(_database.Folder, Guid.NewGuid().ToString("N") + ".copy");
        File.Copy(path, copy);
        return copy;
    }

    private static void Run(string path, string sql)
    {
        using var connection = Open(path, "ReadWrite");
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private List<Guid> ExpenseIds() => _database.Context.Expenses
        .AsNoTracking()
        .Select(expense => expense.ExpenseId)
        .ToList();

    private async Task AddExpenses(int count)
    {
        var category = _database.Context.Categories.AsNoTracking().First();
        for (var index = 0; index < count; index++)
            _database.Context.Add(new Expense(Guid.NewGuid(), category.CategoryId, new Money(100 + index),
                new DateOnly(2026, 11, 7)));

        await _database.Context.SaveChangesAsync();
    }

    private async Task RemoveFirstExpense()
    {
        _database.Context.Remove(_database.Context.Expenses.OrderByDescending(expense => expense.Amount).First());
        await _database.Context.SaveChangesAsync();
    }

    private async Task<RestorePreview> AnalyseToPreview(string path)
    {
        var result = await Analyse(path);
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : "");
        return result.Value;
    }

    [Fact]
    public async Task Bring_back_the_data_of_the_backup_and_count_the_expenses_that_would_be_lost()
    {
        var file = await CreateBackupFile();
        var inBackup = ExpenseIds();
        await AddExpenses(2);
        await RemoveFirstExpense();

        var preview = await AnalyseToPreview(file);
        var restored = await Backup().Restore(preview);

        Assert.Equal(2, preview.LostExpenses);
        Assert.Equal(2, preview.Backup.Expenses);
        Assert.Equal(3, preview.Phone.Expenses);
        Assert.Equal(CreatedAt, preview.CreatedAt);
        Assert.True(restored.IsSuccess);
        Assert.Equal(inBackup.Order(), ExpenseIds().Order());
    }

    [Fact]
    public async Task Leave_the_phone_untouched_while_analysing()
    {
        var file = await CreateBackupFile();
        await AddExpenses(1);
        var idsBefore = ExpenseIds();

        await AnalyseToPreview(file);

        Assert.Equal(idsBefore.Order(), ExpenseIds().Order());
        Assert.DoesNotContain("CentesimoBackup", Tables(_database.FilePath));
    }

    [Fact]
    public async Task Leave_the_staged_copy_without_the_mark_and_standalone()
    {
        var preview = await AnalyseToPreview(await CreateBackupFile());

        Assert.DoesNotContain("CentesimoBackup", Tables(preview.StagedPath));
        Assert.Equal("delete", Scalar<string>(preview.StagedPath, "PRAGMA journal_mode"));
        Assert.StartsWith(Staging, preview.StagedPath);
    }

    [Fact]
    public async Task Refuse_a_file_that_is_not_a_database()
    {
        await using var stream = new MemoryStream("%PDF-1.7 not a backup"u8.ToArray());

        var result = await Backup().Analyse(stream);

        Assert.Equal(PortabilityErrors.NotABackup, result.Error);
        Assert.Empty(Directory.GetFiles(Staging));
    }

    [Fact]
    public async Task Report_an_unreadable_file_when_the_stream_fails_midway()
    {
        await AddExpenses(2);
        var before = ExpenseIds();
        var bytes = await File.ReadAllBytesAsync(await CreateBackupFile());
        await using var stream = new FailingStream(bytes, failAfter: 4096);

        var result = await Backup().Analyse(stream);

        Assert.Equal(PortabilityErrors.Unreadable, result.Error);
        Assert.Empty(Directory.GetFiles(Staging));
        Assert.Equal(before, ExpenseIds());
    }

    [Fact]
    public async Task Refuse_a_database_without_the_mark()
    {
        var unmarked = Path.Combine(_database.Folder, "unmarked.copy");
        await _database.Context.Database.ExecuteSqlAsync($"VACUUM INTO {unmarked}");

        var result = await Analyse(unmarked);

        Assert.Equal(PortabilityErrors.NotABackup, result.Error);
        Assert.Empty(Directory.GetFiles(Staging));
    }

    [Fact]
    public async Task Refuse_a_backup_with_a_mark_that_holds_no_date()
    {
        var copy = CopyOf(await CreateBackupFile());
        Run(copy, "UPDATE CentesimoBackup SET CreatedAt = 'yesterday'");

        var result = await Analyse(copy);

        Assert.Equal(PortabilityErrors.NotABackup, result.Error);
    }

    [Fact]
    public async Task Refuse_a_backup_that_holds_a_trigger()
    {
        var copy = CopyOf(await CreateBackupFile());
        Run(copy, "CREATE TRIGGER Planted AFTER INSERT ON Expenses BEGIN DELETE FROM Categories; END");

        var result = await Analyse(copy);

        Assert.Equal(PortabilityErrors.NotABackup, result.Error);
        Assert.Empty(Directory.GetFiles(Staging));
    }

    [Fact]
    public async Task Refuse_a_backup_that_holds_a_view()
    {
        var copy = CopyOf(await CreateBackupFile());
        Run(copy, "CREATE VIEW Planted AS SELECT * FROM Expenses");

        var result = await Analyse(copy);

        Assert.Equal(PortabilityErrors.NotABackup, result.Error);
    }

    [Fact]
    public async Task Refuse_a_backup_made_by_a_newer_version()
    {
        var copy = CopyOf(await CreateBackupFile());
        Run(copy, "INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('29990101000000_Future', '10.0.10')");

        var result = await Analyse(copy);

        Assert.Equal(PortabilityErrors.NewerVersion, result.Error);
        Assert.Empty(Directory.GetFiles(Staging));
    }

    [Fact]
    public async Task Refuse_a_backup_without_migrations()
    {
        var copy = CopyOf(await CreateBackupFile());
        Run(copy, "DELETE FROM __EFMigrationsHistory");

        var result = await Analyse(copy);

        Assert.Equal(PortabilityErrors.NotABackup, result.Error);
    }

    [Fact]
    public async Task Refuse_a_truncated_backup()
    {
        var copy = CopyOf(await CreateBackupFile());
        var bytes = await File.ReadAllBytesAsync(copy);
        await File.WriteAllBytesAsync(copy, bytes[..(bytes.Length / 2)]);

        var result = await Analyse(copy);

        Assert.Equal(PortabilityErrors.Damaged, result.Error);
        Assert.Empty(Directory.GetFiles(Staging));
    }

    [Fact]
    public async Task Refuse_a_backup_with_overwritten_pages()
    {
        var copy = CopyOf(await CreateBackupFile());
        var bytes = await File.ReadAllBytesAsync(copy);
        Array.Fill(bytes, (byte)0xAB, 4096, bytes.Length - 4096);
        await File.WriteAllBytesAsync(copy, bytes);

        var result = await Analyse(copy);

        Assert.Equal(PortabilityErrors.Damaged, result.Error);
    }

    [Fact]
    public async Task Refuse_a_backup_that_breaks_a_foreign_key()
    {
        var copy = CopyOf(await CreateBackupFile());
        Run(copy, "PRAGMA foreign_keys = OFF; UPDATE Expenses SET CategoryId = 'no-such-category'");

        var result = await Analyse(copy);

        Assert.Equal(PortabilityErrors.Damaged, result.Error);
        Assert.Empty(Directory.GetFiles(Staging));
    }

    [Fact]
    public async Task Leave_nothing_staged_when_cancelled()
    {
        var file = await CreateBackupFile();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await using var stream = File.OpenRead(file);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Backup().Analyse(stream, cancellation.Token));

        Assert.Empty(Directory.GetFiles(Staging));
    }

    [Fact]
    public async Task Bring_an_older_backup_to_the_current_schema()
    {
        var old = Path.Combine(_database.Folder, "old.copy");
        var options = new DbContextOptionsBuilder<CentesimoDbContext>()
            .UseSqlite($"Data Source={old};Pooling=False")
            .Options;
        await using (var context = new CentesimoDbContext(options))
        {
            context.GetService<IMigrator>().Migrate("20260720111928_InitialCreate");
            context.Database.ExecuteSql($"""
                INSERT INTO Categories (CategoryId, Name, Icon, Color, IsArchived)
                VALUES ('00000000-0000-0000-0000-000000000001', 'Casa', 'cart', '#176B5B', 0)
                """);
        }

        Run(old, "CREATE TABLE CentesimoBackup (CreatedAt TEXT NOT NULL);"
            + $"INSERT INTO CentesimoBackup VALUES ('{CreatedAt.ToString("O", CultureInfo.InvariantCulture)}')");
        var preview = await AnalyseToPreview(old);

        var restored = await Backup().Restore(preview);

        Assert.True(restored.IsSuccess);
        Assert.Equal(1, preview.Backup.Categories);
        var category = _database.Context.Categories.AsNoTracking().Single();
        Assert.Equal("mint", category.Color);
        Assert.Empty(_database.Context.Database.GetPendingMigrations());
    }

    [Fact]
    public async Task Show_the_restored_rows_to_a_connection_opened_before_the_restore()
    {
        var file = await CreateBackupFile();
        await AddExpenses(3);
        await _database.Context.Database.OpenConnectionAsync();
        Assert.Equal(5, await _database.Context.Expenses.CountAsync());
        var preview = await AnalyseToPreview(file);

        await Backup().Restore(preview);

        Assert.Equal(2, await _database.Context.Expenses.CountAsync());
    }

    [Fact]
    public async Task Wait_for_a_writer_to_finish_before_replacing_the_data()
    {
        var file = await CreateBackupFile();
        await AddExpenses(1);
        var preview = await AnalyseToPreview(file);
        await using var writer = Open(_database.FilePath, "ReadWrite");
        await using var begin = writer.CreateCommand();
        begin.CommandText = "BEGIN IMMEDIATE";
        await begin.ExecuteNonQueryAsync();
        var release = Task.Run(async () =>
        {
            await Task.Delay(600);
            await using var rollback = writer.CreateCommand();
            rollback.CommandText = "ROLLBACK";
            await rollback.ExecuteNonQueryAsync();
        });
        var started = DateTime.UtcNow;

        var restored = await Backup().Restore(preview);
        await release;

        Assert.True(restored.IsSuccess);
        Assert.True(DateTime.UtcNow - started >= TimeSpan.FromMilliseconds(400));
        Assert.Equal(2, ExpenseIds().Count);
    }

    [Fact]
    public async Task Create_the_expenses_due_since_the_backup_after_restoring()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var category = _database.Context.Categories.AsNoTracking().First();
        var payment = new RecurringPayment(Guid.NewGuid(), category.CategoryId, new Money(700),
            RecurrenceFrequency.Weekly, today.AddDays(-10));
        _database.Context.Add(payment);
        await _database.Context.SaveChangesAsync();
        var file = await CreateBackupFile();
        _database.Context.Remove(payment);
        await _database.Context.SaveChangesAsync();
        var services = new ServiceCollection();
        services.AddApplicationServices();
        services.AddInfrastructure(_database.FilePath);
        services.AddSingleton<IRecurringPaymentReminder, NoOpRecurringPaymentReminder>();
        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var backup = new SqliteDataBackup(scope.ServiceProvider.GetRequiredService<CentesimoDbContext>(),
            provider.GetRequiredService<RecurringPaymentAutomation>(), () => CreatedAt, Staging);
        await using var stream = File.OpenRead(file);
        var preview = (await backup.Analyse(stream)).Value;

        var restored = await backup.Restore(preview);

        Assert.True(restored.IsSuccess);
        var created = await _database.Context.Expenses
            .AsNoTracking()
            .CountAsync(expense => expense.RecurringPaymentId == payment.RecurringPaymentId);
        Assert.True(created >= 2);
    }

    [Fact]
    public async Task Refuse_to_install_a_file_outside_the_staging_folder()
    {
        var file = await CreateBackupFile();
        var idsBefore = ExpenseIds();
        var preview = (await AnalyseToPreview(file)) with { StagedPath = file };

        var restored = await Backup().Restore(preview);

        Assert.Equal(PortabilityErrors.RestoreFailed, restored.Error);
        Assert.True(File.Exists(file));
        Assert.Equal(idsBefore.Order(), ExpenseIds().Order());
    }

    [Fact]
    public async Task Refuse_to_install_a_path_that_climbs_out_of_the_staging_folder()
    {
        var file = await CreateBackupFile();
        var climbing = Path.Combine(Staging, "..", "exports", Path.GetFileName(file));
        var preview = (await AnalyseToPreview(file)) with { StagedPath = climbing };

        var restored = await Backup().Restore(preview);

        Assert.Equal(PortabilityErrors.RestoreFailed, restored.Error);
        Assert.True(File.Exists(file));
    }

    [Fact]
    public async Task Clear_the_copy_of_an_earlier_analysis_at_the_next_one()
    {
        var file = await CreateBackupFile();
        var first = await AnalyseToPreview(file);

        var second = await AnalyseToPreview(file);

        Assert.False(File.Exists(first.StagedPath));
        Assert.Equal([second.StagedPath], Directory.GetFiles(Staging));
    }

    [Fact]
    public async Task Delete_the_staged_copy_once_installed()
    {
        var preview = await AnalyseToPreview(await CreateBackupFile());

        await Backup().Restore(preview);

        Assert.Empty(Directory.GetFiles(Staging));
    }

    [Fact]
    public async Task Keep_the_staged_copy_and_the_data_when_the_install_fails()
    {
        var file = await CreateBackupFile();
        var analysed = await AnalyseToPreview(file);
        var broken = Path.Combine(Staging, "broken.db3");
        await File.WriteAllBytesAsync(broken, [.. "SQLite format 3\0"u8.ToArray(), .. new byte[4000]]);
        var idsBefore = ExpenseIds();
        var preview = analysed with { StagedPath = broken };

        var restored = await Backup().Restore(preview);

        Assert.Equal(PortabilityErrors.RestoreFailed, restored.Error);
        Assert.True(File.Exists(broken));
        Assert.Equal(idsBefore.Order(), ExpenseIds().Order());
    }

    [Fact]
    public async Task Keep_the_live_database_in_wal_mode_and_intact_after_restoring()
    {
        var preview = await AnalyseToPreview(await CreateBackupFile());

        await Backup().Restore(preview);

        Assert.Equal("wal", Scalar<string>(_database.FilePath, "PRAGMA journal_mode"));
        Assert.Equal("ok", Scalar<string>(_database.FilePath, "PRAGMA integrity_check"));
        Assert.DoesNotContain("CentesimoBackup", Tables(_database.FilePath));
    }
}

file sealed class FailingStream(byte[] content, int failAfter) : MemoryStream(content)
{
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (Position >= failAfter)
            throw new IOException("The connection dropped.");

        return await base.ReadAsync(buffer[..Math.Min(buffer.Length, failAfter - (int)Position)], cancellationToken);
    }
}
