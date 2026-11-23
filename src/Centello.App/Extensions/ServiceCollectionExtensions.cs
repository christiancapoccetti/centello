using Centello.Application;
using Centello.App.Pages;
using Centello.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Centello.App;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUi(this IServiceCollection services)
    {
        // Android can recreate MainActivity while the process lives, and every window gets its own Shell. The Shell and
        // the tab pages hold the renderers of the activity that showed them, so they are never shared between windows.
        // The view models are, so the state and the data they hold outlive the activity.
        services.AddSingleton<TodayViewModel>();
        services.AddTransient<TodayPage>();
        services.AddSingleton<InsightsViewModel>();
        services.AddTransient<InsightsPage>();
        services.AddSingleton<CategoriesViewModel>();
        services.AddTransient<CategoriesPage>();
        services.AddSingleton<SettingsViewModel>();
        services.AddTransient<SettingsPage>();
        services.AddTransient<LicensesViewModel>();
        services.AddTransient<LicensesPage>();
        services.AddTransient<BackupViewModel>();
        services.AddTransient<BackupPage>();
        services.AddTransient<RestoreViewModel>();
        services.AddTransient<RestorePage>();
        services.AddSingleton<AppEntryRouter>();
        services.AddSingleton<INotificationPermission, MauiNotificationPermission>();
        services.AddSingleton<IReminderOffer, NotificationReminderOffer>();
        services.AddSingleton<IDraftConsentStore, PreferencesDraftConsentStore>();
        services.AddSingleton<IInternetAccess, MauiInternetAccess>();
        services.AddSingleton<PhotoPicker>();
#if ANDROID
        services.AddSingleton<IFirebaseGeneration, FirebaseGeneration>();
        services.AddSingleton<IPdfPageCounter, AndroidPdfPageCounter>();
        services.AddSingleton<ISpeechRecorder, AndroidAudioRecorder>();
        services.AddSingleton<IRecurringPaymentReminder, AndroidRecurringPaymentReminder>();
        services.AddSingleton<IConfirmation, MaterialConfirmation>();
        services.AddSingleton<ISnackbar, MaterialSnackbar>();
        services.AddSingleton<IDownloads, MediaStoreDownloads>();
        services.AddSingleton<IMicrophoneAccess, MicrophoneAccess>();
        services.AddSingleton<IPhotoCapture, PanelPhotoCapture>();
        services.AddSingleton<HomeScreen>();
        services.AddSingleton<IStoreChangeListener>(provider => provider.GetRequiredService<HomeScreen>());
#else
        services.AddSingleton<IFirebaseGeneration, UnavailableFirebaseGeneration>();
        services.AddSingleton<IPdfPageCounter, UnavailablePdfPageCounter>();
        services.AddSingleton<ISpeechRecorder, UnavailableSpeechRecorder>();
        services.AddSingleton<IRecurringPaymentReminder, NoOpRecurringPaymentReminder>();
        services.AddSingleton<IConfirmation, AlertConfirmation>();
        services.AddSingleton<ISnackbar, NoSnackbar>();
        services.AddSingleton<IDownloads, NoDownloads>();
#endif
        services.AddRecapStory();
        services.AddRecapNotices();
        services.AddTransient<AppShell>();
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
