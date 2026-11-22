using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Centello.Infrastructure.Tests;

internal sealed class TestFileDatabase : IDisposable
{
    public string Folder { get; } = Path.Combine(Path.GetTempPath(), "centello-test-" + Guid.NewGuid().ToString("N"));

    public string FilePath => Path.Combine(Folder, "centello.db3");

    public CentelloDbContext Context { get; }

    public TestFileDatabase()
    {
        Directory.CreateDirectory(Folder);
        var options = new DbContextOptionsBuilder<CentelloDbContext>()
            .UseSqlite($"Data Source={FilePath}")
            .Options;
        Context = new CentelloDbContext(options);
        Context.Database.Migrate();
    }

    public void Dispose()
    {
        Context.Dispose();
        SqliteConnection.ClearAllPools();
        Directory.Delete(Folder, true);
    }
}
