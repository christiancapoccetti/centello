using Centesimo.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Centesimo.App.ViewModels;

/// <summary>Where the import is: waiting for a backup, reading one, showing what it holds, importing it or done.</summary>
public enum ImportStage
{
    Choose,
    Analysing,
    Preview,
    Importing,
    Done
}

public sealed class MoneyManagerImportViewModel(IServiceScopeFactory scopeFactory) : ObservableObject
{
    private const string ChooseAgain = " Scegli un file .mmbackup esportato da Money Manager.";

    private ImportStage _stage = ImportStage.Choose;
    private string _fileName = "";
    private string _errorMessage = "";
    private string _resultMessage = "";
    private MoneyManagerImportPreview? _preview;

    public ImportStage Stage
    {
        get => _stage;
        private set
        {
            if (!SetProperty(ref _stage, value))
                return;

            OnPropertyChanged(nameof(CanChoose));
            OnPropertyChanged(nameof(ShowFile));
            OnPropertyChanged(nameof(ShowPreview));
            OnPropertyChanged(nameof(IsPreviewReady));
            OnPropertyChanged(nameof(ShowProgress));
            OnPropertyChanged(nameof(ProgressText));
            OnPropertyChanged(nameof(IsDone));
            OnPropertyChanged(nameof(IsBusy));
            OnPropertyChanged(nameof(CanLeave));
        }
    }
    public string FileName { get => _fileName; private set => SetProperty(ref _fileName, value); }
    public string ErrorMessage { get => _errorMessage; private set => SetProperty(ref _errorMessage, value); }
    public string ResultMessage { get => _resultMessage; private set => SetProperty(ref _resultMessage, value); }
    public MoneyManagerImportPreview? Preview
    {
        get => _preview;
        private set
        {
            if (!SetProperty(ref _preview, value))
                return;

            OnPropertyChanged(nameof(NewCategories));
            OnPropertyChanged(nameof(NewExpenses));
            OnPropertyChanged(nameof(NewRecurringPayments));
            OnPropertyChanged(nameof(AlreadyPresent));
        }
    }

    public bool CanChoose => Stage is ImportStage.Choose;
    public bool ShowFile => Stage is ImportStage.Analysing or ImportStage.Preview or ImportStage.Importing;
    public bool ShowPreview => Stage is ImportStage.Preview or ImportStage.Importing;
    public bool IsPreviewReady => Stage is ImportStage.Preview;
    public bool ShowProgress => Stage is ImportStage.Analysing or ImportStage.Importing;
    public bool IsDone => Stage is ImportStage.Done;
    public bool IsBusy => ShowProgress;
    public bool CanLeave => !IsBusy;
    public string ProgressText => Stage is ImportStage.Analysing
        ? "Lettura del backup in corso."
        : "Importazione in corso. Puoi lasciare aperta questa schermata.";
    public string NewCategories => Count(Preview?.CategoriesCount);
    public string NewExpenses => Count(Preview?.ExpensesCount);
    public string NewRecurringPayments => Count(Preview?.RecurringPaymentsCount);
    public string AlreadyPresent => Count(Preview?.ExistingItemsCount);

    public async Task Analyse(string fileName, Stream backup)
    {
        ErrorMessage = "";
        FileName = fileName;
        Stage = ImportStage.Analysing;
        try
        {
            using var scope = scopeFactory.CreateScope();
            var result = await scope.ServiceProvider.GetRequiredService<MoneyManagerImportService>().Preview(backup);
            if (result.IsSuccess)
            {
                Preview = result.Value;
                Stage = ImportStage.Preview;
                return;
            }

            Reject(result.Error.Message);
        }
        catch
        {
            Reject("Non è stato possibile leggere il backup.");
        }
    }

    /// <summary>The file could not be opened at all. Anything already analysed stays on screen.</summary>
    public void FailToOpen() => ErrorMessage = "Non è stato possibile aprire il backup selezionato." + ChooseAgain;

    public async Task Import()
    {
        if (Preview is not { } preview || Stage is not ImportStage.Preview)
            return;

        ErrorMessage = "";
        Stage = ImportStage.Importing;
        try
        {
            using var scope = scopeFactory.CreateScope();
            var result = await scope.ServiceProvider.GetRequiredService<MoneyManagerImportService>().Import(preview);
            if (result.IsFailure)
            {
                ErrorMessage = result.Error.Message;
                Stage = ImportStage.Preview;
                return;
            }

            ResultMessage = result.Value.Summary;
            Preview = null;
            Stage = ImportStage.Done;
        }
        catch
        {
            ErrorMessage = "Non è stato possibile importare il backup.";
            Stage = ImportStage.Preview;
        }
    }

    private void Reject(string message)
    {
        Preview = null;
        FileName = "";
        ErrorMessage = message + ChooseAgain;
        Stage = ImportStage.Choose;
    }

    private static string Count(int? count) => ItalianFormat.Count(count ?? 0);
}
