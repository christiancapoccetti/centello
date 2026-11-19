using System.Globalization;
using System.Windows.Input;
using Centesimo.Application;
using Centesimo.Domain;

namespace Centesimo.App.ViewModels;

public sealed class RecurringPaymentEditorViewModel : ObservableObject
{
    private static readonly CultureInfo ItalianCulture = CultureInfo.GetCultureInfo("it-IT");
    private readonly RecurringPaymentService _paymentService;
    private readonly RecurringPaymentAutomation _automation;
    private Guid? _paymentId;
    private string _amount = "";
    private RecurrenceFrequency _frequency = RecurrenceFrequency.Monthly;
    private DateTime _nextDueOn = DateTime.Today;
    private DateTime? _endsOn;
    private bool _hasEndDate;
    private string _note = "";
    private string _amountError = "";
    private string _endDateError = "";
    private string _errorMessage = "";
    private bool _isSaving;

    public event EventHandler? Saved;

    public CategoryTagPicker Picker { get; }
    public IReadOnlyList<FrequencyOption> Frequencies { get; } =
    [
        new(RecurrenceFrequency.Weekly, "Settimanale"),
        new(RecurrenceFrequency.Monthly, "Mensile"),
        new(RecurrenceFrequency.Yearly, "Annuale")
    ];
    public FrequencyOption SelectedFrequency
    {
        get => Frequencies.Single(item => item.Value == Frequency);
        set => Frequency = value.Value;
    }
    public ICommand SaveCommand { get; }
    public string Title => _paymentId.HasValue ? "Modifica pagamento" : "Nuovo pagamento";
    public string Amount
    {
        get => _amount;
        set
        {
            if (SetProperty(ref _amount, value))
                AmountError = "";
        }
    }
    public RecurrenceFrequency Frequency
    {
        get => _frequency;
        set
        {
            if (SetProperty(ref _frequency, value))
                OnPropertyChanged(nameof(SelectedFrequency));
        }
    }
    public DateTime NextDueOn { get => _nextDueOn; set => SetProperty(ref _nextDueOn, value); }
    public DateTime? EndsOn
    {
        get => _endsOn;
        set
        {
            if (SetProperty(ref _endsOn, value))
                EndDateError = "";
        }
    }
    public bool HasEndDate
    {
        get => _hasEndDate;
        set
        {
            if (!SetProperty(ref _hasEndDate, value))
                return;

            if (!value)
                EndsOn = null;
            else if (!EndsOn.HasValue)
                EndsOn = NextDueOn > DateTime.Today ? NextDueOn : DateTime.Today;

            OnPropertyChanged(nameof(CanSelectEndDate));
        }
    }
    public bool CanSelectEndDate => HasEndDate;
    public string Note { get => _note; set => SetProperty(ref _note, value); }
    public bool IsSaving { get => _isSaving; private set => SetProperty(ref _isSaving, value); }
    public string AmountError { get => _amountError; private set => SetProperty(ref _amountError, value); }
    public string EndDateError { get => _endDateError; private set => SetProperty(ref _endDateError, value); }
    /// <summary>A failure that belongs to no field, such as a database error.</summary>
    public string ErrorMessage { get => _errorMessage; private set => SetProperty(ref _errorMessage, value); }

    public RecurringPaymentEditorViewModel(CategoryTagPicker picker, RecurringPaymentService paymentService,
        RecurringPaymentAutomation automation)
    {
        Picker = picker;
        _paymentService = paymentService;
        _automation = automation;
        SaveCommand = new AsyncCommand(Save, () => !IsSaving);
    }

    public async Task Load(Guid? paymentId)
    {
        _paymentId = paymentId;
        OnPropertyChanged(nameof(Title));
        var categories = await Picker.Load();
        if (categories.IsFailure)
        {
            ErrorMessage = categories.Error.Message;
            return;
        }

        if (!paymentId.HasValue)
            return;

        var found = await _paymentService.Get(paymentId.Value);
        if (found.IsFailure || found.Value is not { } payment)
        {
            ErrorMessage = found.IsFailure ? found.Error.Message : ApplicationErrors.RecurringPaymentNotFound.Message;
            return;
        }

        Amount = payment.Amount.ToDecimal().ToString("0.00", ItalianCulture);
        Frequency = payment.Frequency;
        NextDueOn = payment.NextDueOn.ToDateTime(TimeOnly.MinValue);
        EndsOn = payment.EndsOn?.ToDateTime(TimeOnly.MinValue);
        HasEndDate = EndsOn.HasValue;
        Note = payment.Note;
        await Picker.Restore(payment.CategoryId, payment.TagId);
    }

    private async Task Save()
    {
        var amount = MoneyInputParser.ParsePositive(Amount, ItalianCulture);
        AmountError = amount.IsFailure ? amount.Error.Message : "";
        EndDateError = EndsOn < NextDueOn ? ApplicationErrors.InvalidEndDate.Message : "";
        ErrorMessage = "";
        // Every check runs, so every field that stops the save says so at once.
        var hasCategory = Picker.Validate();
        if (amount.IsFailure || EndDateError.HasValue() || !hasCategory)
            return;

        IsSaving = true;
        var request = new SaveRecurringPaymentRequest(
            Picker.CategoryId!.Value,
            amount.Value.Cents,
            Frequency,
            DateOnly.FromDateTime(NextDueOn),
            Picker.TagId,
            Note,
            EndsOn.HasValue ? DateOnly.FromDateTime(EndsOn.Value) : null);
        Result result = _paymentId.HasValue
            ? await _paymentService.Update(_paymentId.Value, request)
            : await _paymentService.Create(request);
        IsSaving = false;
        if (result.IsFailure)
        {
            Explain(result.Error);
            return;
        }

        await _automation.ProcessDue();
        Saved?.Invoke(this, EventArgs.Empty);
    }

    private void Explain(Error error)
    {
        if (error == ApplicationErrors.InvalidAmount)
            AmountError = MoneyInputParser.AmountRequired.Message;
        else if (error == ApplicationErrors.InvalidEndDate)
            EndDateError = error.Message;
        else if (!Picker.Explain(error))
            ErrorMessage = error.Message;
    }

    public sealed record FrequencyOption(RecurrenceFrequency Value, string Name);
}
