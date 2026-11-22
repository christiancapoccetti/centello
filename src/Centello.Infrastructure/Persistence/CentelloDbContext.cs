using Centello.Domain;
using Microsoft.EntityFrameworkCore;

namespace Centello.Infrastructure;

public sealed class CentelloDbContext(DbContextOptions<CentelloDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<RecurringPayment> RecurringPayments => Set<RecurringPayment>();
    public DbSet<RecurrenceOccurrence> RecurrenceOccurrences => Set<RecurrenceOccurrence>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CentelloDbContext).Assembly);
}
