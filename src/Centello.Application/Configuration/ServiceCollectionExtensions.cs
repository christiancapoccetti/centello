using Microsoft.Extensions.DependencyInjection;

namespace Centello.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<CategoryService>();
        services.AddScoped<CategoryChoiceService>();
        services.AddScoped<CategoryListService>();
        services.AddScoped<TagService>();
        services.AddScoped<ExpenseService>();
        services.AddScoped<HomeOverviewService>();
        services.AddScoped<ExpenseHistoryService>();
        services.AddScoped<CategoryDetailService>();
        services.AddScoped<IInsightsService, InsightsService>();
        services.AddScoped<IRecapService, RecapService>();
        services.AddScoped<RecurringPaymentService>();
        services.AddScoped<RecurringPaymentListService>();
        services.AddScoped<UpcomingRecurringPaymentService>();
        services.AddScoped<ExpenseExportService>();
        services.AddSingleton<RecurringPaymentAutomation>();
        services.AddSingleton<DraftConsent>();
        services.AddSingleton<ExpenseDraftService>();
        services.AddSingleton(TimeProvider.System);
        services.AddTransient<Dictation>();
        services.AddTransient<PhotoReading>();
        return services;
    }
}
