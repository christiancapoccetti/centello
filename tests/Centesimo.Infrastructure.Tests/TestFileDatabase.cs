using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Centesimo.Infrastructure.Tests;

internal sealed class TestFileDatabase : IDisposable
{
    public string Folder { get; } = Path.Combine(Path.GetTempPath(), "centesimo-test-" + Guid.NewGuid().ToString("N"));

    public string FilePath => Path.Combine(Folder, "centesimo.db3");

    public CentesimoDbContext Context { get; }

    public TestFileDatabase()
    {
        Directory.CreateDirectory(Folder);
        var options = new DbContextOptionsBuilder<CentesimoDbContext>()
            .UseSqlite($"Data Source={FilePath}")
            .Options;
        Context = new CentesimoDbContext(options);
        Context.Database.Migrate();
    }

    public void Dispose()
    {
        Context.Dispose();
        SqliteConnection.ClearAllPools();
        Directory.Delete(Folder, true);
    }
}
