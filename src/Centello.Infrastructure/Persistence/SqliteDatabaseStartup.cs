using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Centello.Infrastructure;

public sealed class SqliteDatabaseStartup(IServiceScopeFactory scopeFactory, ILogger<SqliteDatabaseStartup>? logger = null)
{
    private const int CommandTimeoutSeconds = 2;
    private readonly object _startup = new();
    private Task<bool>? _attempt;

    public bool IsReady => Volatile.Read(ref _attempt) is { IsCompletedSuccessfully: true } attempt && attempt.Result;

    public Task<bool> Run()
    {
        lock (_startup)
        {
            if (_attempt is null || (_attempt.IsCompleted && !IsReady))
                _attempt = Task.Run(Apply);

            return _attempt;
        }
    }

    private async Task<bool> Apply()
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<CentelloDbContext>();
            var connection = (SqliteConnection)context.Database.GetDbConnection();
            connection.DefaultTimeout = CommandTimeoutSeconds;
            context.Database.SetCommandTimeout(CommandTimeoutSeconds);

            await using var gate = OpenMigrationGate(connection.DataSource);
            await gate.OpenAsync();
            // All app migrations share this SQLite writer reservation. The OS releases it on process death.
            await using var reservation = gate.BeginTransaction(deferred: false);
            if ((await context.Database.GetPendingMigrationsAsync()).Any())
            {
                if (File.Exists(connection.DataSource))
                    await ClearAbandonedLock(connection);

                await context.Database.MigrateAsync();
            }

            return true;
        }
        catch (Exception exception)
        {
            logger?.LogWarning(exception, "Could not initialize the database.");
            return false;
        }
    }

    internal static SqliteConnection OpenMigrationGate(string databasePath) => new(
        new SqliteConnectionStringBuilder
        {
            DataSource = databasePath + ".migration-gate",
            DefaultTimeout = CommandTimeoutSeconds,
            Pooling = false
        }.ToString());

    private static async Task ClearAbandonedLock(SqliteConnection connection)
    {
        await connection.OpenAsync();
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1 FROM sqlite_schema WHERE type = 'table' AND name = '__EFMigrationsLock'";
            if (await command.ExecuteScalarAsync() is null)
                return;

            // Android stops the previous version when replacing the package. Every current app migrator holds the
            // sidecar reservation, so a row without that reservation was left by an interrupted migration.
            command.CommandText = "DELETE FROM __EFMigrationsLock WHERE Id = 1";
            await command.ExecuteNonQueryAsync();
        }
        finally
        {
            await connection.CloseAsync();
        }
    }
}
