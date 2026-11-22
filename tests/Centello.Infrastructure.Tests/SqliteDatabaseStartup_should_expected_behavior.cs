using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Centello.Infrastructure.Tests;

public sealed class SqliteDatabaseStartup_should_expected_behavior : IDisposable
{
    private const string Initial = "20260720111928_InitialCreate";
    private readonly TestFileDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Resume_an_interrupted_migration_and_preserve_existing_data()
    {
        PreparePreviousVersion();
        await using var services = CreateServices();
        var startup = services.GetRequiredService<SqliteDatabaseStartup>();

        await startup.Run();

        Assert.True(startup.IsReady);
        Assert.Empty(await _database.Context.Database.GetPendingMigrationsAsync());
        Assert.Equal("Casa", await _database.Context.Categories.Select(category => category.Name).SingleAsync());
        Assert.Equal("mint", await _database.Context.Categories.Select(category => category.Color).SingleAsync());
        Assert.Equal(0L, await MigrationLockRows());
    }

    [Fact]
    public async Task Leave_the_migration_lock_untouched_while_another_connection_holds_the_gate()
    {
        PreparePreviousVersion();
        await using var services = CreateServices();
        var startup = services.GetRequiredService<SqliteDatabaseStartup>();
        await using var gate = new SqliteConnection($"Data Source={_database.FilePath}.migration-gate;Pooling=False");
        await gate.OpenAsync();
        await using var reservation = gate.BeginTransaction(deferred: false);

        await startup.Run();

        Assert.False(startup.IsReady);
        Assert.NotEmpty(await _database.Context.Database.GetPendingMigrationsAsync());
        Assert.Equal(1L, await MigrationLockRows());
        await reservation.RollbackAsync();

        await startup.Run();

        Assert.True(startup.IsReady);
        Assert.Empty(await _database.Context.Database.GetPendingMigrationsAsync());
    }

    [Fact]
    public async Task Initialize_a_new_database_in_wal_mode()
    {
        var newPath = Path.Combine(_database.Folder, "new.db3");
        await using var services = CreateServices(newPath);
        var startup = services.GetRequiredService<SqliteDatabaseStartup>();

        await startup.Run();

        Assert.True(startup.IsReady);
        await using var connection = new SqliteConnection($"Data Source={newPath};Pooling=False");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode";
        Assert.Equal("wal", await command.ExecuteScalarAsync());
    }

    private ServiceProvider CreateServices(string? databasePath = null) => new ServiceCollection()
        .AddInfrastructure(databasePath ?? _database.FilePath)
        .BuildServiceProvider();

    private void PreparePreviousVersion()
    {
        _database.Context.GetService<IMigrator>().Migrate(Initial);
        _database.Context.Database.ExecuteSqlRaw("""
            INSERT INTO Categories (CategoryId, Name, Icon, Color, IsArchived)
            VALUES ('00000000-0000-0000-0000-000000000001', 'Casa', 'cart', '#176B5B', 0);
            INSERT INTO __EFMigrationsLock (Id, Timestamp) VALUES (1, CURRENT_TIMESTAMP);
            """);
    }

    private async Task<long> MigrationLockRows() => await _database.Context.Database
        .SqlQueryRaw<long>("SELECT COUNT(*) AS Value FROM __EFMigrationsLock")
        .SingleAsync();
}
