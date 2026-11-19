using Centesimo.App.ViewModels;
using Centesimo.Application;

namespace Centesimo.App.Pages;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _viewModel;

    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadApiKeyStatus();
    }

    private async void OnSaveApiKeyClicked(object? sender, EventArgs e)
    {
        var accepted = await DisplayAlertAsync(
            "Inserimento rapido con OpenAI",
            OpenAiSettings.ConsentMessage,
            "Acconsento",
            "Annulla");
        await _viewModel.SaveApiKey(accepted);
    }

    private void OnRemoveApiKeyClicked(object? sender, EventArgs e) =>
        _viewModel.RemoveApiKey();

    private async void OnRecurringPaymentsTapped(object? sender, TappedEventArgs e) =>
        await Shell.Current.GoToAsync(nameof(RecurringPaymentsPage));

    private async void OnImportClicked(object? sender, EventArgs e)
    {
        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Scegli il backup Money Manager"
            });
            if (file is null)
            {
                _viewModel.ClearPreview();
                return;
            }

            await using var stream = await file.OpenReadAsync();
            await _viewModel.Preview(stream);
        }
        catch (OperationCanceledException)
        {
            _viewModel.ShowError("Importazione annullata.");
        }
        catch
        {
            _viewModel.ShowError("Non è stato possibile aprire il backup selezionato.");
        }
    }
    private async void OnImportPreviewClicked(object? sender, EventArgs e) => await _viewModel.ImportPreview();

    private async void OnOpenSourceLicensesClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(OpenSourceLicensesPage));

    private async void OnLicenseClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(LicensePage));
}
