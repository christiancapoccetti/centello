using Centello.Application;
using Centello.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Centello.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string databasePath)
    {
        if (databasePath.IsEmpty())
            throw new ArgumentException("Database path is required.", nameof(databasePath));

        services.AddSingleton(provider => new StoreChangeInterceptor(provider.GetService<IStoreChangeListener>()));
        services.AddDbContext<CentelloDbContext>((provider, options) => options
            .UseSqlite($"Data Source={databasePath}")
            .AddInterceptors(provider.GetRequiredService<StoreChangeInterceptor>()));
        services.AddSingleton<SqliteDatabaseStartup>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddScoped<IRecurringPaymentRepository, RecurringPaymentRepository>();
        services.AddScoped<IRecurringOccurrenceProcessor, RecurringOccurrenceProcessor>();
        services.AddScoped<IDataBackup, SqliteDataBackup>();
        services.AddSingleton<IExpenseInterpreter, FirebaseExpenseInterpreter>();
        return services;
    }
}
