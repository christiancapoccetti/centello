using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Centello.Infrastructure;

public sealed class CentelloDbContextFactory : IDesignTimeDbContextFactory<CentelloDbContext>
{
    public CentelloDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CentelloDbContext>()
            .UseSqlite("Data Source=centello.design.db3")
            .Options;
        return new CentelloDbContext(options);
    }
}
