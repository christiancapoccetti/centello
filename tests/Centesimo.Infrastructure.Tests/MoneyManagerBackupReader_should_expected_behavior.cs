using System.IO.Compression;
using Centesimo.Application;
using Centesimo.Infrastructure;
using Centesimo.Domain;
using Microsoft.Data.Sqlite;

namespace Centesimo.Infrastructure.Tests;

public sealed class MoneyManagerBackupReader_should_expected_behavior
{
    [Fact]
    public async Task Read_imports_only_supported_active_expenses()
    {
        await using var backup = await CreateBackup(SchemaAndData);

        var result = await new MoneyManagerBackupReader().Read(backup);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Categories);
        Assert.Equal(25000, result.Value.Categories[0].MonthlyBudgetCents);
        Assert.Single(result.Value.Tags);
        Assert.Equal(2, result.Value.Expenses.Count);
        var expense = Assert.Single(result.Value.Expenses, value => value.SourceUid == "expense-1");
        Assert.Equal(1234, expense.AmountCents);
        Assert.Equal(new DateOnly(2026, 7, 20), expense.OccurredOn);
        Assert.Equal("Lunch", expense.Note);
        Assert.Equal(result.Value.Tags[0].SourceUid, expense.TagSourceUid);
        var regularExpense = Assert.Single(result.Value.Expenses, value => value.SourceUid == "regular-1");
        Assert.Equal(400, regularExpense.AmountCents);
        Assert.Equal(2, result.Value.IgnoredCount);
        Assert.Equal(1, result.Value.UncategorizedCount);
    }

    [Fact]
    public async Task Read_rejects_an_invalid_header()
    {
        await using var backup = new MemoryStream([1, 2, 3, 4]);

        var result = await new MoneyManagerBackupReader().Read(backup);

        Assert.True(result.IsFailure);
        Assert.Equal("Import.InvalidBackup", result.Error.Code);
    }

    [Fact]
    public async Task Read_rejects_an_unsupported_schema()
    {
        await using var backup = await CreateBackup("CREATE TABLE category (uid TEXT PRIMARY KEY);");

        var result = await new MoneyManagerBackupReader().Read(backup);

        Assert.True(result.IsFailure);
        Assert.Equal("Import.UnsupportedSchema", result.Error.Code);
    }

    [Fact]
    public async Task Read_rejects_too_many_archive_entries()
    {
        await using var backup = await CreateArchive(33);

        var result = await new MoneyManagerBackupReader().Read(backup);

        Assert.True(result.IsFailure);
        Assert.Equal("Import.InvalidArchive", result.Error.Code);
    }

    [Fact]
    public async Task Read_returns_a_failure_for_malformed_typed_values()
    {
        await using var backup = await CreateBackup(SchemaAndData.Replace("'Lunch'", "X'0102'"));

        var result = await new MoneyManagerBackupReader().Read(backup);

        Assert.True(result.IsFailure);
        Assert.Equal("Import.InvalidDatabase", result.Error.Code);
    }

    [Fact]
    public async Task Read_rejects_identifiers_over_the_supported_limit()
    {
        var longUid = new string('x', 201);
        await using var backup = await CreateBackup(SchemaAndData.Replace("expense-1", longUid));

        var result = await new MoneyManagerBackupReader().Read(backup);

        Assert.True(result.IsFailure);
        Assert.Equal("Import.InvalidDatabase", result.Error.Code);
    }

    [Theory]
    [InlineData("cart", "cart")]
    [InlineData("car", "car")]
    [InlineData("transport", "car")]
    [InlineData("cafe", "coffee")]
    [InlineData("bill", "bills")]
    [InlineData("travels", "travel")]
    [InlineData("education", "school")]
    [InlineData("products", "cart")]
    [InlineData("present", "gift")]
    [InlineData("electronics", "tech")]
    [InlineData("canister", "fuel")]
    [InlineData("health", "health")]
    [InlineData("heart", "heart")]
    [InlineData("home", "home")]
    [InlineData("sport", "sport")]
    [InlineData("family", "family")]
    [InlineData("food", "food")]
    public async Task Read_maps_money_manager_icons_to_catalog_keys(string source, string expected) =>
        Assert.Equal(expected, await ReadIcon(source));

    [Theory]
    [InlineData("electronics1", "tech")]
    [InlineData("home12", "home")]
    [InlineData("Cafe", "coffee")]
    public async Task Read_ignores_case_and_trailing_digits_in_icon_names(string source, string expected) =>
        Assert.Equal(expected, await ReadIcon(source));

    [Theory]
    [InlineData("console")]
    [InlineData("carpet")]
    [InlineData("homework")]
    [InlineData("1")]
    [InlineData("")]
    public async Task Read_draws_an_unknown_icon_as_more(string source) =>
        Assert.Equal("more", await ReadIcon(source));

    [Fact]
    public async Task Read_spreads_categories_over_the_six_chromatic_palette_slots()
    {
        await using var backup = await CreateBackup(SchemaAndData + ManyCategories);

        var result = await new MoneyManagerBackupReader().Read(backup);

        Assert.True(result.IsSuccess);
        Assert.Equal(41, result.Value.Categories.Count);
        Assert.Equal(
            ["butter", "coral", "lilac", "mint", "peri", "sky"],
            result.Value.Categories
                .Select(category => category.Color)
                .Distinct()
                .Order());
    }

    [Fact]
    public async Task Read_gives_a_category_the_same_color_on_every_import()
    {
        await using var first = await CreateBackup(SchemaAndData + ManyCategories);
        await using var second = await CreateBackup(SchemaAndData + ManyCategories);

        var firstResult = await new MoneyManagerBackupReader().Read(first);
        var secondResult = await new MoneyManagerBackupReader().Read(second);

        Assert.Equal(ColorsBySourceUid(firstResult.Value), ColorsBySourceUid(secondResult.Value));
    }

    private static async Task<string> ReadIcon(string moneyManagerIcon)
    {
        await using var backup = await CreateBackup(SchemaAndData.Replace("'food'", $"'{moneyManagerIcon}'"));

        var result = await new MoneyManagerBackupReader().Read(backup);

        Assert.True(result.IsSuccess);
        var icon = Assert.Single(result.Value.Categories).Icon;
        Assert.Equal(icon, CategoryIcons.Resolve(icon).Key);
        return icon;
    }

    private static List<string> ColorsBySourceUid(MoneyManagerImportData data) => data.Categories
        .OrderBy(category => category.SourceUid, StringComparer.Ordinal)
        .Select(category => $"{category.SourceUid}={category.Color}")
        .ToList();

    private static async Task<MemoryStream> CreateArchive(int entryCount)
    {
        await using var zip = new MemoryStream();
        using (var archive = new ZipArchive(zip, ZipArchiveMode.Create, true))
            for (var index = 0; index < entryCount; index++)
            {
                var entry = archive.CreateEntry(index == 0 ? "MyFinance.db" : $"entry-{index}");
                await using var destination = entry.Open();
                await destination.WriteAsync(new byte[16]);
            }

        var result = new MemoryStream();
        await result.WriteAsync(new byte[] { 8, 8, 8, 8, 8, 8, 8, 8 });
        zip.Position = 0;
        await zip.CopyToAsync(result);
        result.Position = 0;
        return result;
    }
    private static async Task<MemoryStream> CreateBackup(string sql)
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"centesimo-test-{Guid.NewGuid():N}.db");
        try
        {
            await using (var connection = new SqliteConnection($"Data Source={databasePath};Pooling=False"))
            {
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = sql;
                await command.ExecuteNonQueryAsync();
            }

            await using var zip = new MemoryStream();
            using (var archive = new ZipArchive(zip, ZipArchiveMode.Create, true))
            {
                var entry = archive.CreateEntry("MyFinance.db");
                await using var destination = entry.Open();
                await using var source = File.OpenRead(databasePath);
                await source.CopyToAsync(destination);
            }

            var result = new MemoryStream();
            await result.WriteAsync(new byte[] { 8, 8, 8, 8, 8, 8, 8, 8 });
            zip.Position = 0;
            await zip.CopyToAsync(result);
            result.Position = 0;
            return result;
        }
        finally
        {
            File.Delete(databasePath);
        }
    }

    private const string SchemaAndData = """
        CREATE TABLE category (uid TEXT PRIMARY KEY, title TEXT, type TEXT, icon TEXT, color INTEGER, limitAmount INTEGER, isRemoved INTEGER);
        CREATE TABLE tag (uid TEXT PRIMARY KEY, name TEXT, isRemoved INTEGER);
        CREATE TABLE [transaction] (uid TEXT PRIMARY KEY, type TEXT, amountInDefaultCurrency INTEGER, date TEXT, comment TEXT, isRemoved INTEGER);
        CREATE TABLE sync_link (entityType TEXT, entityUid TEXT, otherType TEXT, otherUid TEXT, isRemoved INTEGER);
        INSERT INTO category VALUES ('expense-category', 'Food', 'Expense', 'food', 1, 25000, 0);
        INSERT INTO category VALUES ('income-category', 'Salary', 'Income', 'money', 2, 50000, 0);
        INSERT INTO tag VALUES ('tag-1', 'Restaurant', 0);
        INSERT INTO [transaction] VALUES ('expense-1', 'Expense', 1234, '2026-07-20', 'Lunch', 0);
        INSERT INTO [transaction] VALUES ('income-1', 'Income', 5000, '2026-07-20', '', 0);
        INSERT INTO [transaction] VALUES ('removed-1', 'Expense', 200, '2026-07-20', '', 1);
        INSERT INTO [transaction] VALUES ('uncategorized-1', 'Expense', 300, '2026-07-20', '', 0);
        INSERT INTO [transaction] VALUES ('regular-1', 'Expense', 400, '2026-07-20', '', 0);
        INSERT INTO sync_link VALUES ('Transaction', 'expense-1', 'Category', 'expense-category', 0);
        INSERT INTO sync_link VALUES ('Transaction', 'expense-1', 'Tag', 'tag-1', 0);
        INSERT INTO sync_link VALUES ('Transaction', 'removed-1', 'Category', 'expense-category', 0);
        INSERT INTO sync_link VALUES ('Transaction', 'regular-1', 'Category', 'expense-category', 0);
        INSERT INTO sync_link VALUES ('RegularPaymentPeriod', 'period-1', 'Transaction', 'regular-1', 0);
        """;

    private static readonly string ManyCategories = string.Concat(Enumerable.Range(0, 40).Select(index => $"""

        INSERT INTO category VALUES ('category-{index}', 'Category {index}', 'Expense', 'unknown', {index}, NULL, 0);
        INSERT INTO [transaction] VALUES ('spend-{index}', 'Expense', 100, '2026-07-20', '', 0);
        INSERT INTO sync_link VALUES ('Transaction', 'spend-{index}', 'Category', 'category-{index}', 0);
        """));
}
