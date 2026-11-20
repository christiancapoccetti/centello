using Centesimo.App.ViewModels;
using Centesimo.Application;

namespace Centesimo.App.Pages;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _viewModel;
    private readonly IConfirmation _confirmation;
    private readonly ISnackbar _snackbar;

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
        await _viewModel.Load();
    }

    private async void OnRecurringPaymentsTapped(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(RecurringPaymentsPage));

    private async void OnBackupTapped(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(BackupPage));

    private async void OnLicensesTapped(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(LicensesPage));

    private void OnReplaceClicked(object? sender, EventArgs e) => _viewModel.StartReplacing();

    private void OnCancelReplaceClicked(object? sender, EventArgs e) => _viewModel.StopReplacing();

    private async void OnSaveKeyClicked(object? sender, EventArgs e)
    {
        if (!_viewModel.HasKeyToSave())
            return;

        // Annulla in the dialog leaves everything as it is: the key stays typed and nothing is said.
        var accepted = await _confirmation.Ask("Inserimento vocale con OpenAI", OpenAiSettings.ConsentMessage, "Acconsento", "Annulla");
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
