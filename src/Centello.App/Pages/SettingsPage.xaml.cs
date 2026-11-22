using Centello.App.ViewModels;
using Centello.Application;

namespace Centello.App.Pages;

public partial class SettingsPage : ContentPage
{
    private const string PrivacyPolicyUrl = "https://christiancapoccetti.github.io/centello/privacy-policy.html";
    private const int KeyboardWaitAttempts = 15;
    private static readonly TimeSpan KeyboardWaitStep = TimeSpan.FromMilliseconds(100);

    private readonly SettingsViewModel _viewModel;
    private readonly IConfirmation _confirmation;
    private readonly ISnackbar _snackbar;
    private Window? _settingsWindow;

    public SettingsPage(SettingsViewModel viewModel, IConfirmation confirmation, ISnackbar snackbar)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _confirmation = confirmation;
        _snackbar = snackbar;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _settingsWindow = Window;
        if (_settingsWindow is not null)
            _settingsWindow.Activated += OnWindowActivated;

        await _viewModel.Load();
    }

    protected override void OnDisappearing()
    {
        if (_settingsWindow is not null)
            _settingsWindow.Activated -= OnWindowActivated;

        _settingsWindow = null;
        base.OnDisappearing();
    }

    private void OnWindowActivated(object? sender, EventArgs e) => _viewModel.RefreshRecapNotice();

    private async void OnRecurringPaymentsTapped(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(RecurringPaymentsPage));

    private async void OnBackupTapped(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(BackupPage));

    private async void OnLicensesTapped(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(LicensesPage));

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

    // The tab bar stays above the keyboard on this page, so with both open the button under the field is behind the
    // bar. Once the keyboard has taken its room, which shows as the form getting shorter, the page scrolls to the button.
    private async void OnKeyFieldFocused(object? sender, EventArgs e)
    {
        var height = Form.Height;
        for (var attempt = 0; attempt < KeyboardWaitAttempts && Form.Height >= height; attempt++)
            await Task.Delay(KeyboardWaitStep);

        await Form.ScrollToAsync(SaveKeyButton, ScrollToPosition.End, true);
    }

    private async void OnRecapNoticeToggled(object? sender, ToggledEventArgs e) =>
        await _viewModel.SetRecapNotice(e.Value);

    private void OnAndroidNotificationSettingsTapped(object? sender, EventArgs e) => AppInfo.ShowSettingsUI();

    private void OnReplaceClicked(object? sender, EventArgs e) => _viewModel.StartReplacing();

    private void OnCancelReplaceClicked(object? sender, EventArgs e) => _viewModel.StopReplacing();

    private async void OnSaveKeyClicked(object? sender, EventArgs e)
    {
        if (!_viewModel.HasKeyToSave())
            return;

        // Annulla in the dialog leaves everything as it is: the key stays typed and nothing is said.
        var accepted = await _confirmation.Ask("Voce e foto con OpenAI", OpenAiSettings.ConsentMessage, "Acconsento", "Annulla");
        if (await _viewModel.SaveApiKey(accepted))
            _snackbar.Show("Chiave salvata. Tieni premuto il microfono per dettare.");
    }

    private async void OnRemoveClicked(object? sender, EventArgs e)
    {
        var confirmed = await _confirmation.Ask(
            "Rimuovere la chiave OpenAI?",
            "Il microfono smetterà di funzionare finché non salvi una nuova chiave. Le spese registrate restano sul telefono.",
            "Rimuovi",
            "Annulla",
            isDestructive: true);
        if (!confirmed)
            return;

        _viewModel.RemoveApiKey();
        _snackbar.Show("Chiave rimossa");
    }
}
