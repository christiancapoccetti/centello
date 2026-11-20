using System.Globalization;
using System.Windows.Input;
using Centesimo.Application;
using Centesimo.Domain;

namespace Centesimo.App.ViewModels;

public sealed class ExpenseEditorViewModel : ObservableObject
{
    private static readonly CultureInfo ItalianCulture = CultureInfo.GetCultureInfo("it-IT");
    private readonly ExpenseService _expenseService;
    private Guid? _expenseId;
    private string _amount = "";
    private DateTime _occurredOn = DateTime.Today;
    private string _note = "";
    private string _amountError = "";
    private string _errorMessage = "";
    private ExpenseDraft? _draft;
    private bool _isSaving;
    private FormState _original;

    public event EventHandler? Saved;
    /// <summary>The save stopped on a field. Its message is under that field.</summary>
    public event EventHandler? Rejected;

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
    /// <summary>The draft a new expense started from, for the banner above the form. Null for a typed expense.</summary>
    public ExpenseDraft? Draft
    {
        get => _draft;
        private set
        {
            if (SetProperty(ref _draft, value))
                OnPropertyChanged(nameof(HasDraft));
        }
    }
    public bool HasDraft => Draft?.Banner.HasValue() == true;
    /// <summary>True while the form differs from what is stored. A new expense starts from an empty form, so a draft
    /// from the voice or a photo is unsaved input from the start.</summary>
    public bool IsDirty => Snapshot() != _original;
    public bool IsSaving { get => _isSaving; private set => SetProperty(ref _isSaving, value); }
    public string AmountError { get => _amountError; private set => SetProperty(ref _amountError, value); }
    /// <summary>A failure that belongs to no field, such as a database error.</summary>
    public string ErrorMessage { get => _errorMessage; private set => SetProperty(ref _errorMessage, value); }

    public ExpenseEditorViewModel(
        CategoryTagPicker picker,
        ExpenseService expenseService)
    {
        Picker = picker;
        _expenseService = expenseService;
        SaveCommand = new AsyncCommand(Save, () => !IsSaving);
        _original = Snapshot();
    }

    public Task Load(Guid? expenseId = null) => Open(expenseId, null);

    public Task Load(ExpenseDraft draft) => Open(null, draft);

    private async Task Open(Guid? expenseId, ExpenseDraft? draft)
    {
        _expenseId = expenseId;
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(IsEditMode));
        var categories = await Picker.Load();
        if (categories.IsFailure)
        {
            ErrorMessage = categories.Error.Message;
            return;
        }

        if (expenseId.HasValue)
            await LoadExpense(expenseId.Value);

        _original = Snapshot();
        if (draft is not null)
            await ApplyDraft(draft);
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

    private async Task ApplyDraft(ExpenseDraft draft)
    {
        Amount = draft.AmountCents is { } cents ? (cents / 100m).ToString("0.00", ItalianCulture) : "";
        OccurredOn = draft.OccurredOn?.ToDateTime(TimeOnly.MinValue) ?? DateTime.Today;
        Note = draft.Note;
        Draft = draft;
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
            ErrorMessage = result.Error.Message;

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
        {
            Rejected?.Invoke(this, EventArgs.Empty);
            return;
        }

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
            Rejected?.Invoke(this, EventArgs.Empty);
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

    private FormState Snapshot() => new(Amount.Trim(), Picker.CategoryId, Picker.TagId, OccurredOn.Date, Note.Trim());

    private readonly record struct FormState(string Amount, Guid? CategoryId, Guid? TagId, DateTime OccurredOn, string Note);
}
