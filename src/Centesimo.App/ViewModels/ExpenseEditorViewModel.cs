using System.Globalization;
using System.Windows.Input;
using Centesimo.Application;
using Centesimo.Domain;

namespace Centesimo.App.ViewModels;

public sealed class ExpenseEditorViewModel : ObservableObject
{
    private static readonly CultureInfo ItalianCulture = CultureInfo.GetCultureInfo("it-IT");
    private readonly ExpenseService _expenseService;
    private readonly IPendingExpenseSpeechDraft _pendingSpeechDraft;
    private Guid? _expenseId;
    private string _amount = "";
    private DateTime _occurredOn = DateTime.Today;
    private string _note = "";
    private string _amountError = "";
    private string _errorMessage = "";
    private bool _isLoading;
    private bool _isSaving;

    public event EventHandler? Saved;

    public CategoryTagPicker Picker { get; }
    public ICommand SaveCommand { get; }
    public string Title => _expenseId.HasValue ? "Modifica spesa" : "Nuova spesa";
    public bool IsEditMode => _expenseId.HasValue;
    public string Amount
    {
        get => _amount;
        set
        {
            if (SetProperty(ref _amount, value))
                AmountError = "";
        }
    }
    public DateTime OccurredOn { get => _occurredOn; set => SetProperty(ref _occurredOn, value); }
    public string Note { get => _note; set => SetProperty(ref _note, value); }
    public bool IsLoading { get => _isLoading; private set => SetProperty(ref _isLoading, value); }
    public bool IsSaving { get => _isSaving; private set => SetProperty(ref _isSaving, value); }
    public string AmountError { get => _amountError; private set => SetProperty(ref _amountError, value); }
    /// <summary>A failure that belongs to no field, such as a database error.</summary>
    public string ErrorMessage { get => _errorMessage; private set => SetProperty(ref _errorMessage, value); }

    public ExpenseEditorViewModel(
        CategoryTagPicker picker,
        ExpenseService expenseService,
        IPendingExpenseSpeechDraft pendingSpeechDraft)
    {
        Picker = picker;
        _expenseService = expenseService;
        _pendingSpeechDraft = pendingSpeechDraft;
        SaveCommand = new AsyncCommand(Save, () => !IsSaving);
    }

    public async Task Load(Guid? expenseId = null)
    {
        _expenseId = expenseId;
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(IsEditMode));
        IsLoading = true;
        var categories = await Picker.Load();
        if (categories.IsFailure)
            ErrorMessage = categories.Error.Message;
        else if (expenseId.HasValue)
            await LoadExpense(expenseId.Value);
        else
            await ApplyPendingSpeechDraft();

        IsLoading = false;
    }

    private async Task LoadExpense(Guid expenseId)
    {
        var result = await _expenseService.Get(expenseId);
        if (result.IsFailure)
        {
            ErrorMessage = result.Error.Message;
            return;
        }

        if (result.Value is not { } expense)
        {
            ErrorMessage = ApplicationErrors.ExpenseNotFound.Message;
            return;
        }

        Amount = expense.Amount.ToDecimal().ToString("0.00", ItalianCulture);
        OccurredOn = expense.OccurredOn.ToDateTime(TimeOnly.MinValue);
        Note = expense.Note;
        await Picker.Restore(expense.CategoryId, expense.TagId);
    }

    private async Task ApplyPendingSpeechDraft()
    {
        var draft = _pendingSpeechDraft.Take();
        if (draft is null)
            return;

        Amount = draft.AmountCents is { } cents ? (cents / 100m).ToString("0.00", ItalianCulture) : "";
        OccurredOn = draft.OccurredOn?.ToDateTime(TimeOnly.MinValue) ?? DateTime.Today;
        Note = draft.Note;
        await Picker.Suggest(draft.CategoryId, draft.TagId);
    }

    public async Task<Result> Delete()
    {
        if (!_expenseId.HasValue)
            return Result.Failure(ApplicationErrors.ExpenseNotFound);

        IsSaving = true;
        ErrorMessage = "";
        var result = await _expenseService.Delete(_expenseId.Value);
        IsSaving = false;
        if (result.IsFailure)
        {
            ErrorMessage = result.Error.Message;
            return result;
        }

        Saved?.Invoke(this, EventArgs.Empty);
        return result;
    }

    private async Task Save()
    {
        var amount = MoneyInputParser.ParsePositive(Amount, ItalianCulture);
        AmountError = amount.IsFailure ? amount.Error.Message : "";
        ErrorMessage = "";
        // Both checks run, so every field that stops the save says so at once.
        var hasCategory = Picker.Validate();
        if (amount.IsFailure || !hasCategory)
            return;

        IsSaving = true;
        var request = new SaveExpenseRequest(
            Picker.CategoryId!.Value,
            amount.Value.Cents,
            DateOnly.FromDateTime(OccurredOn),
            Picker.TagId,
            Note);
        Result result = _expenseId.HasValue
            ? await _expenseService.Update(_expenseId.Value, request)
            : await _expenseService.Create(request);
        if (result.IsFailure)
        {
            IsSaving = false;
            Explain(result.Error);
            return;
        }

        // Stays true after success so the save button cannot fire again while the page is popping.
        Saved?.Invoke(this, EventArgs.Empty);
    }

    private void Explain(Error error)
    {
        if (error == ApplicationErrors.InvalidAmount)
            AmountError = MoneyInputParser.AmountRequired.Message;
        else if (!Picker.Explain(error))
            ErrorMessage = error.Message;
    }
}
