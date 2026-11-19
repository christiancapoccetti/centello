using Microsoft.Extensions.DependencyInjection;

namespace Centesimo.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<CategoryService>();
        services.AddScoped<TagService>();
        services.AddScoped<ExpenseService>();
        services.AddScoped<BudgetOverviewService>();
        services.AddScoped<MonthlyOverviewService>();
        services.AddScoped<YearlyOverviewService>();
        services.AddScoped<IInsightsService, InsightsService>();
        services.AddScoped<CategorySpendingService>();
        services.AddScoped<MoneyManagerImportService>();
        services.AddScoped<RecurringPaymentService>();
        services.AddScoped<UpcomingRecurringPaymentService>();
        services.AddSingleton<OpenAiSettings>();
        services.AddSingleton<IOpenAiApiKeyProvider>(provider => provider.GetRequiredService<OpenAiSettings>());
        services.AddSingleton<IPendingExpenseSpeechDraft, PendingExpenseSpeechDraft>();
        services.AddSingleton<SpeechExpenseDraftService>();
        return services;
    }
}
