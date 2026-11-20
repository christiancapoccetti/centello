using Centesimo.App.ViewModels;

namespace Centesimo.App.Pages;

public partial class BackupPage : ContentPage
{
    private readonly BackupViewModel _viewModel;
    private readonly ISnackbar _snackbar;

    public BackupPage(BackupViewModel viewModel, ISnackbar snackbar)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _snackbar = snackbar;
        BindingContext = viewModel;
    }

    private async void OnBackClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");

    private async void OnExportCsvTapped(object? sender, EventArgs e)
    {
        var result = await _viewModel.ExportExpenses();
        if (result is null)
            return;

        if (result.IsFailure)
        {
            _snackbar.Show(result.Error.Message);
            return;
        }

        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Esporta spese",
            File = new ShareFile(result.Value.FilePath, result.Value.ContentType)
        });
    }
}
