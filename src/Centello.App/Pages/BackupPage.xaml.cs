using Centello.App.ViewModels;
using Centello.Application;

namespace Centello.App.Pages;

public partial class BackupPage : ContentPage
{
    private readonly BackupViewModel _viewModel;
    private readonly ISnackbar _snackbar;
    private readonly IDownloads _downloads;

    public BackupPage(BackupViewModel viewModel, ISnackbar snackbar, IDownloads downloads)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _snackbar = snackbar;
        _downloads = downloads;
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

        var file = result.Value;
        var name = Path.GetFileName(file.FilePath);
        var shareDirectory = "";
        var shared = false;
        try
        {
            if (_downloads.CanSave && (await _downloads.Save(file)).IsSuccess)
            {
                _snackbar.Show($"Salvato in Download: {name}");
                return;
            }

            shareDirectory = ExportCache.NewShareDirectory();
            var sharePath = Path.Combine(shareDirectory, name);
            Directory.CreateDirectory(shareDirectory);
            File.Move(file.FilePath, sharePath);
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = title,
                File = new ShareFile(sharePath, file.ContentType)
            });
            shared = true;
        }
        catch (Exception)
        {
            _snackbar.Show("Non è stato possibile salvare o condividere il file.");
        }
        finally
        {
            ExportCache.Delete(Path.GetDirectoryName(file.FilePath)!);
            if (!shared && shareDirectory.Length != 0)
                ExportCache.Delete(shareDirectory);
        }
    }
}
