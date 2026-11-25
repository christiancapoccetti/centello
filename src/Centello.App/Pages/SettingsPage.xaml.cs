using Centello.App.ViewModels;
using Centello.Application;

namespace Centello.App.Pages;

public partial class SettingsPage : ContentPage
{
    private const string PrivacyPolicyUrl = "https://christiancapoccetti.github.io/centello/privacy-policy.html";

    private readonly SettingsViewModel _viewModel;
    private readonly AiAccess _aiAccess;
    private readonly IConfirmation _confirmation;
    private readonly ISnackbar _snackbar;
    private Window? _settingsWindow;
    private bool _changingDraftConsent;
    private bool _isVisible;

    public SettingsPage(SettingsViewModel viewModel, AiAccess aiAccess, IConfirmation confirmation, ISnackbar snackbar)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _aiAccess = aiAccess;
        _confirmation = confirmation;
        _snackbar = snackbar;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _isVisible = true;
        _aiAccess.Changed += OnAiAccessChanged;
        _settingsWindow = Window;
        if (_settingsWindow is not null)
            _settingsWindow.Activated += OnWindowActivated;

        await _viewModel.Load();
    }

    protected override void OnDisappearing()
    {
        _isVisible = false;
        _aiAccess.Changed -= OnAiAccessChanged;
        if (_settingsWindow is not null)
            _settingsWindow.Activated -= OnWindowActivated;

        _settingsWindow = null;
        base.OnDisappearing();
    }

    private void OnWindowActivated(object? sender, EventArgs e)
    {
        _viewModel.RefreshRecapNotice();
        _viewModel.RefreshAiAccess();
    }

    private void OnAiAccessChanged() => Dispatcher.Dispatch(() =>
    {
        if (_isVisible)
            _viewModel.RefreshAiAccess();
    });

    private async void OnRecurringPaymentsTapped(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(RecurringPaymentsPage));

    private async void OnBackupTapped(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(BackupPage));

    private async void OnLicensesTapped(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(LicensesPage));

    private async void OnAiInviteTapped(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(AiInvitePage));

    private async void OnPrivacyPolicyTapped(object? sender, EventArgs e)
    {
        try
        {
            await Launcher.Default.OpenAsync(new Uri(PrivacyPolicyUrl));
        }
        catch
        {
            _snackbar.Show("Non è stato possibile aprire il browser");
        }
    }

    private async void OnRecapNoticeToggled(object? sender, ToggledEventArgs e) =>
        await _viewModel.SetRecapNotice(e.Value);

    private void OnAndroidNotificationSettingsTapped(object? sender, EventArgs e) => AppInfo.ShowSettingsUI();

    private async void OnDraftConsentToggled(object? sender, ToggledEventArgs e)
    {
        if (_changingDraftConsent)
            return;

        _changingDraftConsent = true;
        try
        {
            if (e.Value && !await _confirmation.Ask(
                "Attivare l’inserimento spese con l’AI?",
                DraftConsent.Disclosure,
                "Acconsento",
                "Annulla"))
                return;

            _viewModel.SetDraftsEnabled(e.Value);
        }
        finally
        {
            _changingDraftConsent = false;
        }
    }
}
