using Centello.Application;
using Centello.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Centello.Infrastructure.Tests;

public sealed class StoreChangeInterceptor_should_expected_behavior
{
    [Fact]
    public async Task Signal_once_after_a_plain_save_when_the_row_is_visible_to_another_connection()
    {
        using var store = new SignalingStore();

        await store.Save(CreateCategory("Food"));

        Assert.Equal([1], store.Listener.CategoriesSeen);
    }

    [Fact]
    public async Task Signal_twice_after_a_multi_statement_save_once_for_ef_own_transaction_and_once_for_the_save()
    {
        using var store = new SignalingStore();
        using var scope = store.Provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CentelloDbContext>();
        context.Categories.AddRange(CreateCategory("Food"), CreateCategory("Home"));

        await context.SaveChangesAsync();

        Assert.Equal([2, 2], store.Listener.CategoriesSeen);
    }

    [Fact]
    public async Task Signal_after_the_commit_and_never_between_the_save_and_the_commit()
    {
        using var store = new SignalingStore();
        var signalsAfterSave = -1;

        var saved = await store.InTransaction(async (db, token) =>
        {
            db.Categories.Add(CreateCategory("Food"));
            await db.SaveChangesAsync(token);
            signalsAfterSave = store.Listener.CategoriesSeen.Count;
        });

        Assert.True(saved.IsSuccess);
        Assert.Equal(0, signalsAfterSave);
        Assert.Equal([1], store.Listener.CategoriesSeen);
    }

    [Fact]
    public async Task Stay_silent_when_the_transaction_rolls_back()
    {
        using var store = new SignalingStore();

        var failed = await store.InTransaction(async (db, token) =>
        {
            db.Categories.Add(CreateCategory("Food"));
            await db.SaveChangesAsync(token);
            throw new InvalidOperationException("Technical detail");
        });

        Assert.True(failed.IsFailure);
        Assert.Empty(store.Listener.CategoriesSeen);
    }

    [Fact]
    public async Task Stay_silent_when_the_save_fails()
    {
        using var store = new SignalingStore();
        await store.Save(CreateCategory("Food"));
        store.Listener.Clear();

        var failed = await store.Save(CreateCategory("Food"));

        Assert.True(failed.IsFailure);
        Assert.Empty(store.Listener.CategoriesSeen);
    }

    [Fact]
    public async Task Signal_after_a_recurring_payment_is_processed()
    {
        using var store = new SignalingStore();
        var category = CreateCategory("Bills");
        var payment = new RecurringPayment(Guid.NewGuid(), category.CategoryId, new Money(100),
            RecurrenceFrequency.Monthly, new DateOnly(2026, 1, 31));
        await store.Seed(category, payment);
        store.Listener.Clear();
        payment.MoveToNextOccurrence();
        var occurrence = new RecurrenceOccurrence(payment.RecurringPaymentId, new DateOnly(2026, 1, 31));
        var expense = new Expense(Guid.NewGuid(), category.CategoryId, new Money(100), occurrence.DueOn);
        using var scope = store.Provider.CreateScope();

        var processed = await scope.ServiceProvider
            .GetRequiredService<IRecurringOccurrenceProcessor>()
            .Process(payment, expense, occurrence);

        Assert.True(processed.IsSuccess);
        Assert.NotEmpty(store.Listener.ExpensesSeen);
        Assert.All(store.Listener.ExpensesSeen, seen => Assert.Equal(1, seen));
    }

    [Fact]
    public async Task Do_nothing_without_a_listener()
    {
        using var store = new SignalingStore(withListener: false);

        var saved = await store.Save(CreateCategory("Food"));
        var inTransaction = await store.InTransaction((db, _) =>
        {
            db.Categories.Add(CreateCategory("Home"));
            return Task.CompletedTask;
        });

        Assert.True(saved.IsSuccess);
        Assert.True(inTransaction.IsSuccess);
    }

    private static Category CreateCategory(string name) =>
        new(Guid.NewGuid(), name, "cart", "#123456");

    private sealed class RecordingListener : IStoreChangeListener
    {
        public Func<int> CountCategories { get; set; } = () => 0;
        public Func<int> CountExpenses { get; set; } = () => 0;
        public List<int> CategoriesSeen { get; } = [];
        public List<int> ExpensesSeen { get; } = [];

        public void StoreChanged()
        {
            CategoriesSeen.Add(CountCategories());
            ExpensesSeen.Add(CountExpenses());
        }

        public void Clear()
        {
            CategoriesSeen.Clear();
            ExpensesSeen.Clear();
        }
    }

    private sealed class SignalingStore : IDisposable
    {
        private readonly string _path = Path.Combine(Path.GetTempPath(), $"centello-signal-{Guid.NewGuid():N}.db3");

        public RecordingListener Listener { get; } = new();
        public ServiceProvider Provider { get; }

        public SignalingStore(bool withListener = true)
        {
            var services = new ServiceCollection();
            services.AddInfrastructure(_path);
            if (withListener)
                services.AddSingleton<IStoreChangeListener>(Listener);
            Provider = services.BuildServiceProvider();
            using (var scope = Provider.CreateScope())
                scope.ServiceProvider.GetRequiredService<CentelloDbContext>().Database.EnsureCreated();
            Listener.CountCategories = () => CountFromAnotherConnection("Categories");
            Listener.CountExpenses = () => CountFromAnotherConnection("Expenses");
            Listener.Clear();
        }

        public async Task<Result> Save(Category category)
        {
            using var scope = Provider.CreateScope();
            var probe = new Probe(scope.ServiceProvider.GetRequiredService<CentelloDbContext>());
            return await probe.Save(category);
        }

        public async Task Seed(params object[] entities)
        {
            using var scope = Provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<CentelloDbContext>();
            context.AddRange(entities);
            await context.SaveChangesAsync();
        }

        public async Task<Result<bool>> InTransaction(Func<CentelloDbContext, CancellationToken, Task> mutation)
        {
            using var scope = Provider.CreateScope();
            var probe = new Probe(scope.ServiceProvider.GetRequiredService<CentelloDbContext>());
            return await probe.InTransaction(mutation);
        }

        public void Dispose()
        {
            Provider.Dispose();
            SqliteConnection.ClearAllPools();
            File.Delete(_path);
        }

        private int CountFromAnotherConnection(string table)
        {
            using var connection = new SqliteConnection($"Data Source={_path};Pooling=False");
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT COUNT(*) FROM {table}";
            return Convert.ToInt32(command.ExecuteScalar());
        }
    }

    private sealed class Probe(CentelloDbContext context) : RepositoryBase(context)
    {
        public Task<Result> Save(Category category) =>
            SaveContext(db => db.Categories.Add(category));

        public Task<Result<bool>> InTransaction(Func<CentelloDbContext, CancellationToken, Task> mutation) =>
            SaveContextInTransaction(async (db, token) =>
            {
                await mutation(db, token);
                return true;
            });
    }
}
