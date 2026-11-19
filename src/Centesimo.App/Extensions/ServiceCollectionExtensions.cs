using Centesimo.Application;
using Centesimo.App.Pages;
using Centesimo.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Centesimo.App;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUi(this IServiceCollection services)
    {
        services.AddSingleton<TodayViewModel>();
        services.AddSingleton<TodayPage>();
        services.AddSingleton<InsightsViewModel>();
        services.AddSingleton<InsightsPage>();
        services.AddSingleton<CategoriesViewModel>();
        services.AddSingleton<CategoriesPage>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<SettingsPage>();
        services.AddTransient<OpenSourceLicensesPage>();
        services.AddTransient<LicensePage>();
        services.AddSingleton<RecurringPaymentAutomation>();
        services.AddSingleton<IOpenAiApiKeyStore, SecureStorageOpenAiApiKeyStore>();
        services.AddSingleton<IInternetAccess, MauiInternetAccess>();
#if ANDROID
        services.AddSingleton<ISpeechRecorder, AndroidAudioRecorder>();
        services.AddSingleton<IRecurringPaymentReminder, AndroidRecurringPaymentReminder>();
#else
        services.AddSingleton<ISpeechRecorder, UnavailableSpeechRecorder>();
        services.AddSingleton<IRecurringPaymentReminder, NoOpRecurringPaymentReminder>();
#endif
        services.AddSingleton<AppShell>();
        services.AddSingleton<Func<AppShell>>(provider =>
            () => provider.GetRequiredService<AppShell>());
        services.AddSingleton<CategoryEditorViewModel>();
        services.AddSingleton<CategoryEditorPage>();
        services.AddTransient<CategorySpendingViewModel>();
        services.AddTransient<CategorySpendingPage>();
        services.AddTransient<TagExpensesViewModel>();
        services.AddTransient<TagExpensesPage>();
        services.AddTransient<ExpenseEditorViewModel>();
        services.AddTransient<ExpenseEditorPage>();
        services.AddTransient<RecurringPaymentsViewModel>();
        services.AddTransient<RecurringPaymentsPage>();
        services.AddTransient<RecurringPaymentEditorViewModel>();
        services.AddTransient<RecurringPaymentEditorPage>();
        return services;
    }
}
