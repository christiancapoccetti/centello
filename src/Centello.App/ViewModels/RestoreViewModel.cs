using Centello.Application;
using Centello.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace Centello.App.ViewModels;

/// <summary>Where the restore is: waiting for a file, reading one, showing what it holds, replacing the data or done.</summary>
public enum RestoreStage
{
    Choose,
    Analysing,
    Preview,
    Restoring,
    Done
}

public sealed class RestoreViewModel(IServiceScopeFactory scopeFactory) : ObservableObject
{
    private RestoreStage _stage = RestoreStage.Choose;
    private string _fileName = "";
    private string _errorMessage = "";
    private RestorePreview? _preview;

    public RestoreStage Stage
    {
        get => _stage;
        private set
        {
            if (!SetProperty(ref _stage, value))
                return;

            OnPropertyChanged(nameof(CanChoose));
            OnPropertyChanged(nameof(ShowComparison));
            OnPropertyChanged(nameof(ShowWarning));
            OnPropertyChanged(nameof(IsPreviewReady));
            OnPropertyChanged(nameof(ShowProgress));
            OnPropertyChanged(nameof(ProgressText));
            OnPropertyChanged(nameof(IsDone));
            OnPropertyChanged(nameof(IsBusy));
            OnPropertyChanged(nameof(CanLeave));
        }
    }

    public string FileName
    {
        get => _fileName;
        private set
        {
            if (!SetProperty(ref _fileName, value))
                return;

            OnPropertyChanged(nameof(ShowFile));
            OnPropertyChanged(nameof(ChooseText));
        }
    }

    public string ErrorMessage { get => _errorMessage; private set => SetProperty(ref _errorMessage, value); }

    public RestorePreview? Preview
    {
        get => _preview;
        private set
        {
            if (!SetProperty(ref _preview, value))
                return;

            OnPropertyChanged(nameof(BackupDate));
            OnPropertyChanged(nameof(Rows));
            OnPropertyChanged(nameof(Warning));
            OnPropertyChanged(nameof(ShowWarning));
            OnPropertyChanged(nameof(DoneMessage));
        }
    }

    public string BackupDate => Preview?.BackupDate ?? "";
    public IReadOnlyList<RestoreComparisonRow> Rows => Preview?.Rows ?? [];
    public string Warning => Preview?.Warning ?? "";
    public string DoneMessage => Preview?.DoneMessage ?? "";

    public bool CanChoose => Stage is RestoreStage.Choose;
    public bool ShowFile => FileName.HasValue();
    public bool ShowComparison => Stage is RestoreStage.Preview or RestoreStage.Restoring;
    public bool ShowWarning => IsPreviewReady && Warning.HasValue();
    public bool IsPreviewReady => Stage is RestoreStage.Preview;
    public bool ShowProgress => IsBusy;
    public bool IsDone => Stage is RestoreStage.Done;
    public bool IsBusy => Stage is RestoreStage.Analysing or RestoreStage.Restoring;
    public bool CanLeave => !IsBusy;
    public string ChooseText => ShowFile ? "Scegli un altro file" : "Scegli file di backup";
    public string ProgressText => Stage is RestoreStage.Analysing ? "Lettura del file in corso…" : "Ripristino in corso…";

    public async Task Analyse(string fileName, Stream backup, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return;

        DiscardPreview();
        ErrorMessage = "";
        FileName = fileName;
        Stage = RestoreStage.Analysing;
        try
        {
            var result = await Task.Run(() => Run(service => service.Analyse(backup, cancellationToken)));
            if (cancellationToken.IsCancellationRequested)
            {
                if (result.IsSuccess)
                    Discard(result.Value);

                return;
            }

            if (result.IsFailure)
            {
                Reject(result.Error.Message);
                return;
            }

            Preview = result.Value;
            Stage = RestoreStage.Preview;
        }
        catch
        {
            if (!cancellationToken.IsCancellationRequested)
                Reject(PortabilityErrors.Unreadable.Message);
        }
    }

    /// <summary>The file could not be opened at all. Anything already analysed stays on screen.</summary>
    public void FailToOpen() =>
        ErrorMessage = "Non è stato possibile aprire il file selezionato. Scegli un file creato con “Crea file di backup”.";

    public async Task Restore(CancellationToken navigation)
    {
        if (Preview is not { } preview || Stage is not RestoreStage.Preview)
            return;

        ErrorMessage = "";
        Stage = RestoreStage.Restoring;
        try
        {
            var result = await Task.Run(() => Run(service => service.Restore(preview)));
            if (result.IsFailure)
            {
                ErrorMessage = result.Error.Message;
                Stage = RestoreStage.Preview;
                return;
            }

            Stage = RestoreStage.Done;
        }
        catch
        {
            ErrorMessage = PortabilityErrors.RestoreFailed.Message;
            Stage = RestoreStage.Preview;
        }
        finally
        {
            if (navigation.IsCancellationRequested)
                Abandon();
        }
    }

    public void Abandon()
    {
        if (Stage is RestoreStage.Restoring)
            return;

        DiscardPreview();
        FileName = "";
        ErrorMessage = "";
        Stage = RestoreStage.Choose;
    }

    private void DiscardPreview()
    {
        if (Preview is { } preview && Stage is not RestoreStage.Done)
            Discard(preview);

        Preview = null;
    }

    private void Discard(RestorePreview preview)
    {
        using var scope = scopeFactory.CreateScope();
        scope.ServiceProvider.GetRequiredService<IDataBackup>().Discard(preview);
    }

    // SQLite finishes its work on the calling thread: run on the UI thread, the restore would freeze the screen and
    // the progress bar would never be drawn. Each run gets its own scope, as the context is not shared across threads.
    private async Task<T> Run<T>(Func<IDataBackup, Task<T>> work)
    {
        using var scope = scopeFactory.CreateScope();
        return await work(scope.ServiceProvider.GetRequiredService<IDataBackup>());
    }

    private void Reject(string message)
    {
        Preview = null;
        ErrorMessage = message;
        Stage = RestoreStage.Choose;
    }
}
