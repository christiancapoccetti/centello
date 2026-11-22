using System.Globalization;
using Centello.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Centello.Infrastructure;

internal static class SqliteRestoreValidation
{
    private const string MigrationLockTable = "__EFMigrationsLock";

    private static readonly string[] InvalidRows =
    [
        """
        SELECT 1 FROM Categories
        WHERE NOT valid_id(CategoryId)
           OR NOT has_text(Name) OR NOT has_text(Icon) OR NOT has_text(Color)
           OR (MonthlyBudget IS NOT NULL AND (typeof(MonthlyBudget) <> 'integer' OR MonthlyBudget < 0))
           OR IsArchived NOT IN (0, 1)
        LIMIT 1
        """,
        """
        SELECT 1 FROM Tags
        WHERE NOT valid_id(TagId) OR NOT valid_id(CategoryId) OR NOT has_text(Name)
           OR IsArchived NOT IN (0, 1)
        LIMIT 1
        """,
        """
        SELECT 1 FROM Expenses AS expense
        LEFT JOIN Tags AS tag ON tag.TagId = expense.TagId
        WHERE NOT valid_id(ExpenseId) OR NOT valid_id(expense.CategoryId)
           OR (expense.TagId IS NOT NULL AND tag.CategoryId <> expense.CategoryId)
           OR (RecurringPaymentId IS NOT NULL AND NOT valid_id(RecurringPaymentId))
           OR typeof(Amount) <> 'integer' OR Amount <= 0
           OR NOT valid_date(OccurredOn) OR typeof(Note) <> 'text'
           OR (PhotoPath IS NOT NULL AND typeof(PhotoPath) <> 'text')
        LIMIT 1
        """,
        $"""
        SELECT 1 FROM RecurringPayments AS payment
        LEFT JOIN Tags AS tag ON tag.TagId = payment.TagId
        WHERE NOT valid_id(RecurringPaymentId) OR NOT valid_id(payment.CategoryId)
           OR (payment.TagId IS NOT NULL AND tag.CategoryId <> payment.CategoryId)
           OR typeof(Amount) <> 'integer' OR Amount <= 0
           OR typeof(payment.Note) <> 'text'
           OR Frequency NOT IN ({(int)RecurrenceFrequency.Weekly}, {(int)RecurrenceFrequency.Monthly}, {(int)RecurrenceFrequency.Yearly})
           OR NOT valid_date(NextDueOn) OR (EndsOn IS NOT NULL AND NOT valid_date(EndsOn))
           OR IsSuspended NOT IN (0, 1)
           OR typeof(AnchorMonth) <> 'integer' OR AnchorMonth NOT BETWEEN 1 AND 12
           OR typeof(AnchorDay) <> 'integer' OR AnchorDay NOT BETWEEN 1 AND 31
        LIMIT 1
        """,
        """
        SELECT 1 FROM RecurrenceOccurrences
        WHERE NOT valid_id(RecurringPaymentId) OR NOT valid_date(DueOn)
        LIMIT 1
        """
    ];

    public static async Task<bool> HasExpectedSchema(SqliteConnection source, string migration,
        SqliteTransaction? transaction, CancellationToken cancellationToken)
    {
        // Build the reference from the shipped migration, independently of the installed database.
        await using var reference = new SqliteConnection("Data Source=:memory:;Pooling=False");
        await reference.OpenAsync(cancellationToken);
        await using var context = new CentelloDbContext(new DbContextOptionsBuilder<CentelloDbContext>()
            .UseSqlite(reference)
            .Options);
        await context.GetService<IMigrator>().MigrateAsync(migration, cancellationToken);

        var expected = await ReadSchema(reference, null, cancellationToken);
        var actual = await ReadSchema(source, transaction, cancellationToken);
        if (!actual.Any(entry => entry.Name == MigrationLockTable))
            expected.RemoveAll(entry => entry.Name == MigrationLockTable);

        return actual.SequenceEqual(expected);
    }

    public static async Task<bool> HasExpectedHistory(SqliteConnection source, string[] migrations,
        SqliteTransaction? transaction, CancellationToken cancellationToken)
    {
        await using var command = source.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT MigrationId, ProductVersion FROM __EFMigrationsHistory ORDER BY MigrationId";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        foreach (var migration in migrations)
        {
            if (!await reader.ReadAsync(cancellationToken) || reader.GetValue(0) is not string id || id != migration
                || reader.GetValue(1) is not string version || version.IsEmpty())
                return false;
        }

        return !await reader.ReadAsync(cancellationToken);
    }

    public static async Task<bool> HasEmptyMigrationLock(SqliteConnection source, SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = source.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT 1 FROM sqlite_schema WHERE type = 'table' AND name = '__EFMigrationsLock'";
        if (await command.ExecuteScalarAsync(cancellationToken) is null)
            return true;

        command.CommandText = "SELECT 1 FROM __EFMigrationsLock LIMIT 1";
        return await command.ExecuteScalarAsync(cancellationToken) is null;
    }

    public static async Task<bool> HasValidRows(SqliteConnection source, SqliteTransaction? transaction,
        CancellationToken cancellationToken)
    {
        source.CreateFunction<object, bool>("valid_id", IsIdentifier, isDeterministic: true);
        source.CreateFunction<object, bool>("valid_date", IsDate, isDeterministic: true);
        source.CreateFunction<object, bool>("has_text", value => value is string text && text.HasValue(), isDeterministic: true);

        await using var command = source.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "PRAGMA integrity_check";
        if (await command.ExecuteScalarAsync(cancellationToken) is not "ok")
            return false;

        command.CommandText = "PRAGMA foreign_key_check";
        if (await command.ExecuteScalarAsync(cancellationToken) is not null)
            return false;

        foreach (var query in InvalidRows)
        {
            command.CommandText = query;
            if (await command.ExecuteScalarAsync(cancellationToken) is not null)
                return false;
        }

        return true;
    }

    private static async Task<List<SchemaEntry>> ReadSchema(SqliteConnection connection, SqliteTransaction? transaction,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT type, name, tbl_name, sql FROM sqlite_schema ORDER BY type, name";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var entries = new List<SchemaEntry>();
        while (await reader.ReadAsync(cancellationToken))
        {
            entries.Add(new SchemaEntry(reader.GetString(0), reader.GetString(1), reader.GetString(2),
                reader.IsDBNull(3) ? "" : reader.GetString(3).ReplaceLineEndings("\n")));
        }

        return entries;
    }

    // The SQLite provider binds GUID and date query parameters in these formats.
    private static bool IsIdentifier(object value) => value is string text
        && Guid.TryParseExact(text, "D", out var id) && id != Guid.Empty && text == id.ToString("D").ToUpperInvariant();

    private static bool IsDate(object value) => value is string text
        && DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

    private sealed record SchemaEntry(string Type, string Name, string Table, string Definition);
}
