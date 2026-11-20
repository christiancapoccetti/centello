using System.Globalization;
using Centesimo.Application;
using Centesimo.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Centesimo.Infrastructure.Tests;

public sealed class SqliteDataBackup_should_expected_behavior : IDisposable
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 11, 12, 21, 40, 5, TimeSpan.FromHours(1));

    private readonly TestFileDatabase _database = new();
    private readonly ServiceProvider _services = new ServiceCollection().BuildServiceProvider();
    private string ExportDirectory => Path.Combine(_database.Folder, "exports");

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
            () => now ?? CreatedAt);

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
}
