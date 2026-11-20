using Centesimo.Application;
using Centesimo.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Centesimo.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string databasePath)
    {
        if (databasePath.IsEmpty())
            throw new ArgumentException("Database path is required.", nameof(databasePath));

        services.AddSingleton(provider => new StoreChangeInterceptor(provider.GetService<IStoreChangeListener>()));
        services.AddDbContext<CentesimoDbContext>((provider, options) => options
            .UseSqlite($"Data Source={databasePath}")
            .AddInterceptors(provider.GetRequiredService<StoreChangeInterceptor>()));
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddScoped<IMoneyManagerBackupReader, MoneyManagerBackupReader>();
        services.AddScoped<IMoneyManagerImportRepository, MoneyManagerImportRepository>();
        services.AddScoped<IRecurringPaymentRepository, RecurringPaymentRepository>();
        services.AddScoped<IRecurringOccurrenceProcessor, RecurringOccurrenceProcessor>();
        services.AddSingleton(provider => new OpenAiClient(
            new HttpClient(),
            provider.GetRequiredService<IOpenAiApiKeyProvider>()));
        services.AddSingleton<ISpeechTranscriber, OpenAiSpeechTranscriber>();
        services.AddSingleton<IExpenseTextInterpreter, OpenAiExpenseTextInterpreter>();
        return services;
    }
}
