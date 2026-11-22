using Centello.App.ViewModels;

namespace Centello.App.Pages;

public partial class RestorePage : ContentPage
{
    private readonly RestoreViewModel _viewModel;

    public RestorePage(RestoreViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    // Leaving while the data is being replaced would hide whether it finished.
    protected override bool OnBackButtonPressed() => _viewModel.IsBusy || base.OnBackButtonPressed();

    private async void OnBackClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");

    // A tab keeps its own stack: leaving it for Oggi would bring this finished page back under Impostazioni.
    private async void OnTodayClicked(object? sender, EventArgs e)
    {
        await Navigation.PopToRootAsync(false);
        await Shell.Current.GoToAsync($"//{nameof(TodayPage)}");
    }

    private async void OnChooseClicked(object? sender, EventArgs e)
    {
        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Scegli il file di backup" });
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

    private async void OnRestoreClicked(object? sender, EventArgs e) => await _viewModel.Restore();
}
