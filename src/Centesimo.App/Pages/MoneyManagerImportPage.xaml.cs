using Centesimo.App.ViewModels;

namespace Centesimo.App.Pages;

public partial class MoneyManagerImportPage : ContentPage
{
    private readonly MoneyManagerImportViewModel _viewModel;

    public MoneyManagerImportPage(MoneyManagerImportViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    // Leaving while the backup is being written would hide whether it finished.
    protected override bool OnBackButtonPressed() => _viewModel.IsBusy || base.OnBackButtonPressed();

    private async void OnBackClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");

    private async void OnTodayClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync($"//{nameof(TodayPage)}");

    private async void OnChooseClicked(object? sender, EventArgs e)
    {
        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Scegli il backup Money Manager" });
            if (file is null)
                return;

            await using var stream = await file.OpenReadAsync();
            await _viewModel.Analyse(file.FileName, stream);
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
            _viewModel.FailToOpen();
        }
    }

    private async void OnImportClicked(object? sender, EventArgs e) => await _viewModel.Import();
}
