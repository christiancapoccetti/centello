using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Centello.Infrastructure;

public sealed class SqliteDatabaseStartup(IServiceScopeFactory scopeFactory, ILogger<SqliteDatabaseStartup>? logger = null)
{
    private const int CommandTimeoutSeconds = 2;
    private volatile bool _isReady;

    public bool IsReady => _isReady;

    public Task Run() => Task.Run(Apply);

    private async Task Apply()
    {
        if (IsReady)
            return;

        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(4));
            var cancellationToken = timeout.Token;
            await using var scope = scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<CentelloDbContext>();
            var connection = (SqliteConnection)context.Database.GetDbConnection();
            connection.DefaultTimeout = CommandTimeoutSeconds;
            context.Database.SetCommandTimeout(CommandTimeoutSeconds);

            await using var gate = OpenMigrationGate(connection.DataSource);
            await gate.OpenAsync(cancellationToken);
            // All app migrations share this SQLite writer reservation. The OS releases it on process death.
            await using var reservation = gate.BeginTransaction(deferred: false);
            if ((await context.Database.GetPendingMigrationsAsync(cancellationToken)).Any())
            {
                if (File.Exists(connection.DataSource))
                    await ClearAbandonedLock(connection, cancellationToken);

                await context.Database.MigrateAsync(cancellationToken);
            }

            _isReady = true;
        }
        catch (Exception exception)
        {
            logger?.LogWarning(exception, "Could not initialize the database.");
        }
    }

    internal static SqliteConnection OpenMigrationGate(string databasePath) => new(
        new SqliteConnectionStringBuilder
        {
            DataSource = databasePath + ".migration-gate",
            DefaultTimeout = CommandTimeoutSeconds,
            Pooling = false
        }.ToString());

    private static async Task ClearAbandonedLock(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await connection.OpenAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1 FROM sqlite_schema WHERE type = 'table' AND name = '__EFMigrationsLock'";
            if (await command.ExecuteScalarAsync(cancellationToken) is null)
                return;

            // Android stops the previous version when replacing the package. Every current app migrator holds the
            // sidecar reservation, so a row without that reservation was left by an interrupted migration.
            command.CommandText = "DELETE FROM __EFMigrationsLock WHERE Id = 1";
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            await connection.CloseAsync();
        }
    }
}
