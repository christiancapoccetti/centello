using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Centello.Infrastructure.Tests;

internal sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public CentelloDbContext Context { get; }

    public TestDatabase()
    {
        _connection.Open();
        var options = new DbContextOptionsBuilder<CentelloDbContext>()
            .UseSqlite(_connection)
            .Options;
        Context = new CentelloDbContext(options);
        Context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
