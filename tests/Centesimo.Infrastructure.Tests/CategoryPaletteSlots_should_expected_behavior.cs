using Centesimo.Application;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Centesimo.Infrastructure.Tests;

public sealed class CategoryPaletteSlots_should_expected_behavior : IDisposable
{
    private const string PreviousMigration = "20260720111928_InitialCreate";
    private const string Migration = "20261119120711_CategoryPaletteSlots";
    private static readonly string[] SlotKeys = ["coral", "peri", "butter", "mint", "lilac", "sky", "grey"];
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly CentesimoDbContext _context;
    private readonly IMigrator _migrator;

    public CategoryPaletteSlots_should_expected_behavior()
    {
        _connection.Open();
        var options = new DbContextOptionsBuilder<CentesimoDbContext>()
            .UseSqlite(_connection)
            .Options;
        _context = new CentesimoDbContext(options);
        _migrator = _context.GetService<IMigrator>();
        _migrator.Migrate(PreviousMigration);
    }

    [Fact]
    public void Convert_legacy_hex_colors_to_palette_slots()
    {
        Store("#176B5B", "#8B4A5D", "#725C00", "#4F5F7A", "#6A4F88", "#8b4a5d");

        _migrator.Migrate(Migration);

        Assert.Equal(["mint", "coral", "butter", "peri", "lilac", "coral"], StoredColors());
    }

    [Fact]
    public void Convert_any_other_value_to_grey()
    {
        Store("#6F7975", "#123456", "red");

        _migrator.Migrate(Migration);

        Assert.Equal(["grey", "grey", "grey"], StoredColors());
    }

    [Fact]
    public void Keep_palette_slots()
    {
        Store(SlotKeys);

        _migrator.Migrate(Migration);

        Assert.Equal(SlotKeys, StoredColors());
    }

    [Fact]
    public void Change_nothing_when_applied_again()
    {
        Store("#176B5B", "#123456", "sky");
        _migrator.Migrate(Migration);
        _context.Database.ExecuteSql($"DELETE FROM __EFMigrationsHistory WHERE MigrationId = {Migration}");

        _migrator.Migrate(Migration);

        Assert.Equal(["mint", "grey", "sky"], StoredColors());
    }

    [Fact]
    public void Store_only_keys_of_the_live_palette()
    {
        Store("#176B5B", "#8B4A5D", "#725C00", "#4F5F7A", "#6A4F88", "#123456");

        _migrator.Migrate(Migration);

        Assert.All(StoredColors(), color => Assert.Equal(color, CategoryPalette.Resolve(color).Key));
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    // Raw SQL on purpose: rows must be written at the schema of the previous migration, not through the current model.
    private void Store(params string[] colors)
    {
        foreach (var color in colors)
            _context.Database.ExecuteSql($"""
                INSERT INTO Categories (CategoryId, Name, Icon, Color, IsArchived)
                VALUES ({Guid.NewGuid().ToString()}, {Guid.NewGuid().ToString()}, 'cart', {color}, 0)
                """);
    }

    private List<string> StoredColors() => _context.Database
        .SqlQuery<string>($"SELECT Color AS Value FROM Categories ORDER BY rowid")
        .ToList();
}
