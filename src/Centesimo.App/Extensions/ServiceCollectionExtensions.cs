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
        services.AddTransient<LicensesViewModel>();
        services.AddTransient<LicensesPage>();
        services.AddTransient<BackupViewModel>();
        services.AddTransient<BackupPage>();
        services.AddSingleton<RecurringPaymentAutomation>();
        services.AddSingleton<IReminderOffer, NotificationReminderOffer>();
        services.AddSingleton<IOpenAiApiKeyStore, SecureStorageOpenAiApiKeyStore>();
        services.AddSingleton<IInternetAccess, MauiInternetAccess>();
#if ANDROID
        services.AddSingleton<ISpeechRecorder, AndroidAudioRecorder>();
        services.AddSingleton<IRecurringPaymentReminder, AndroidRecurringPaymentReminder>();
        services.AddSingleton<IConfirmation, MaterialConfirmation>();
        services.AddSingleton<ISnackbar, MaterialSnackbar>();
#else
        services.AddSingleton<ISpeechRecorder, UnavailableSpeechRecorder>();
        services.AddSingleton<IRecurringPaymentReminder, NoOpRecurringPaymentReminder>();
        services.AddSingleton<IConfirmation, AlertConfirmation>();
        services.AddSingleton<ISnackbar, NoSnackbar>();
#endif
        services.AddSingleton<AppShell>();
        services.AddSingleton<Func<AppShell>>(provider =>
            () => provider.GetRequiredService<AppShell>());
        services.AddTransient<CategoryEditorViewModel>();
        services.AddTransient<CategoryEditorPage>();
        services.AddTransient<CategoryDetailViewModel>();
        services.AddTransient<CategoryDetailPage>();
        services.AddTransient<CategoryTagPicker>();
        services.AddTransient<ExpenseEditorViewModel>();
        services.AddTransient<ExpenseEditorPage>();
        services.AddTransient<ExpensesViewModel>();
        services.AddTransient<ExpensesPage>();
        services.AddTransient<RecurringPaymentsViewModel>();
        services.AddTransient<RecurringPaymentsPage>();
        services.AddTransient<RecurringPaymentEditorViewModel>();
        services.AddTransient<RecurringPaymentEditorPage>();
        return services;
    }
}
