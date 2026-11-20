using Centesimo.App.ViewModels;
using Centesimo.Application;

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

    private async void OnCreateBackupTapped(object? sender, EventArgs e) =>
        await Present(await _viewModel.CreateBackup(), "File di backup");

    private async void OnRestoreTapped(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(RestorePage));

    private async void OnExportCsvTapped(object? sender, EventArgs e) =>
        await Present(await _viewModel.ExportExpenses(), "Esporta spese");

    private async Task Present(Result<ExportedFile>? result, string title)
    {
        if (result is null)
            return;

        if (result.IsFailure)
        {
            _snackbar.Show(result.Error.Message);
            return;
        }

        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = title,
            File = new ShareFile(result.Value.FilePath, result.Value.ContentType)
        });
    }
}
