using System.Globalization;
using System.Windows.Input;
using Centello.Application;
using Centello.Domain;

namespace Centello.App.ViewModels;

public sealed class RecurringPaymentEditorViewModel : ObservableObject
{
    private const int NoEndDate = 0;
    private const int OnADate = 1;
    private static readonly CultureInfo ItalianCulture = CultureInfo.GetCultureInfo("it-IT");
    // The order of the "Ogni" segments.
    private static readonly RecurrenceFrequency[] Frequencies =
        [RecurrenceFrequency.Weekly, RecurrenceFrequency.Monthly, RecurrenceFrequency.Yearly];
    private readonly RecurringPaymentService _paymentService;
    private readonly RecurringPaymentAutomation _automation;
    private readonly IReminderOffer _reminderOffer;
    private Guid? _paymentId;
    private Mode _mode = Mode.New;
    private string _name = "";
    private string _amount = "";
    private int _frequencyIndex = Array.IndexOf(Frequencies, RecurrenceFrequency.Monthly);
    private DateTime _scheduleDate = DateTime.Today;
    private int _endIndex = NoEndDate;
    private DateTime _endDate = DateTime.Today;
    private string _note = "";
    private string _stateNote = "";
    private bool _isReminderOffered;
    private string _amountError = "";
    private string _endDateError = "";
    private string _errorMessage = "";
    private bool _isSaving;
    private FormState _original;

    public event EventHandler? Saved;
    /// <summary>The save stopped on a field. Its message is under that field.</summary>
    public event EventHandler? Rejected;

    public CategoryTagPicker Picker { get; }
    public ICommand SaveCommand { get; }
    public string Title => _mode.Title;
    public string DateLabel => _mode.DateLabel;
    public string StateGlyph => _mode.StateGlyph;
    public bool CanSave => _mode.CanSave;
    public bool IsReadOnly => !_mode.CanSave;
    public bool CanPause => _mode.CanPause;
    public bool CanResume => _mode.CanResume;
    public bool CanManage => _mode.CanPause || _mode.CanResume;
    /// <summary>What a paused or terminated payment says above the form. Empty otherwise.</summary>
    public string StateNote { get => _stateNote; private set => SetProperty(ref _stateNote, value); }
    public bool HasStateNote => StateNote.HasValue();
    public string Amount
    {
        get => _amount;
        set
        {
            if (SetProperty(ref _amount, value))
                AmountError = "";
        }
    }
    public int FrequencyIndex { get => _frequencyIndex; set => SetProperty(ref _frequencyIndex, value); }
    /// <summary>The first expense of a new payment, the next one of a stored payment, the last one of a terminated one.</summary>
    public DateTime ScheduleDate
    {
        get => _scheduleDate;
        set
        {
            if (SetProperty(ref _scheduleDate, value))
                EndDateError = "";
        }
    }
    public int EndIndex
    {
        get => _endIndex;
        set
        {
            if (!SetProperty(ref _endIndex, value))
                return;

            // The end date starts on a date the payment can end on, so it is valid until someone moves it.
            if (value == OnADate && EndDate < ScheduleDate)
                EndDate = ScheduleDate;

            EndDateError = "";
            OnPropertyChanged(nameof(ShowEndDate));
        }
    }
    public DateTime EndDate
    {
        get => _endDate;
        set
        {
            if (SetProperty(ref _endDate, value))
                EndDateError = "";
        }
    }
    /// <summary>A terminated payment already shows its last expense in the date above.</summary>
    public bool ShowEndDate => EndIndex == OnADate && CanSave;
    public string Note { get => _note; set => SetProperty(ref _note, value); }
    public bool IsReminderOffered { get => _isReminderOffered; private set => SetProperty(ref _isReminderOffered, value); }
    public bool IsSaving { get => _isSaving; private set => SetProperty(ref _isSaving, value); }
    public string AmountError { get => _amountError; private set => SetProperty(ref _amountError, value); }
    public string EndDateError { get => _endDateError; private set => SetProperty(ref _endDateError, value); }
    /// <summary>A failure that belongs to no field, such as a database error.</summary>
    public string ErrorMessage { get => _errorMessage; private set => SetProperty(ref _errorMessage, value); }
    /// <summary>True while the form differs from what is stored, or from the empty form of a new payment.</summary>
    public bool IsDirty => CanSave && Snapshot() != _original;
    public string EndQuestion => _name.HasValue() ? $"Terminare {_name}?" : "Terminare il pagamento?";

    public RecurringPaymentEditorViewModel(CategoryTagPicker picker, RecurringPaymentService paymentService,
        RecurringPaymentAutomation automation, IReminderOffer reminderOffer)
    {
        Picker = picker;
        _paymentService = paymentService;
        _automation = automation;
        _reminderOffer = reminderOffer;
        SaveCommand = new AsyncCommand(Save, () => !IsSaving);
        _original = Snapshot();
    }

    public async Task Load(Guid? paymentId)
    {
        _paymentId = paymentId;
        if (paymentId.HasValue)
            await LoadPayment(paymentId.Value);
        else
            await LoadNew();

        _original = Snapshot();
    }

    /// <summary>"Netflix in pausa": what a snackbar says after something was done to the payment.</summary>
    public string Done(string what) => $"{(_name.HasValue() ? _name : "Pagamento")} {what}";

    public Task<Result> Pause() => Change(paymentId => _paymentService.Suspend(paymentId));

    public Task<Result> Resume() => Change(paymentId => _paymentService.Resume(paymentId));

    /// <summary>What ending the payment now leaves behind, for the dialog that asks to confirm it.</summary>
    public async Task<Result<RecurringEndPreview>> PreviewEnd()
    {
        var preview = await _paymentService.PreviewEnd(_paymentId.GetValueOrDefault());
        if (preview.IsFailure)
            ErrorMessage = preview.Error.Message;

        return preview;
    }

    public Task<Result> End(RecurringEndPreview preview) =>
        Change(paymentId => _paymentService.End(paymentId, preview.LastExpenseOn));

    /// <summary>Asks the system for the notification permission. True when reminders are now allowed.</summary>
    public async Task<bool> AcceptReminder()
    {
        IsReminderOffered = false;
        var allowed = await _reminderOffer.Accept();
        if (allowed)
            await _automation.ProcessDue(waitForCurrentPass: true);

        return allowed;
    }

    private async Task LoadNew()
    {
        var categories = await Picker.Load();
        if (categories.IsFailure)
        {
            ErrorMessage = categories.Error.Message;
            return;
        }

        IsReminderOffered = await _reminderOffer.IsOpen();
    }

    private async Task LoadPayment(Guid paymentId)
    {
        var found = await _paymentService.Get(paymentId);
        if (found.IsFailure || found.Value is not { } payment)
        {
            ErrorMessage = found.IsFailure ? found.Error.Message : ApplicationErrors.RecurringPaymentNotFound.Message;
            return;
        }

        _mode = Mode.Of(payment.Status);
        // A terminated payment only shows what it stored, so its category is offered even when it is archived.
        var categories = await Picker.Load(IsReadOnly ? payment.CategoryId : null);
        if (categories.IsFailure)
        {
            ErrorMessage = categories.Error.Message;
            return;
        }

        var shown = payment.IsEnded ? payment.EndsOn!.Value : payment.NextDueOn;
        Amount = payment.Amount.ToDecimal().ToString("0.00", ItalianCulture);
        FrequencyIndex = Array.IndexOf(Frequencies, payment.Frequency);
        ScheduleDate = shown.ToDateTime(TimeOnly.MinValue);
        EndDate = (payment.EndsOn ?? shown).ToDateTime(TimeOnly.MinValue);
        EndIndex = payment.EndsOn.HasValue ? OnADate : NoEndDate;
        Note = payment.Note;
        if (IsReadOnly)
            await Picker.Suggest(payment.CategoryId, payment.TagId);
        else
            await Picker.Restore(payment.CategoryId, payment.TagId);

        _name = payment.Note.HasValue()
            ? payment.Note
            : Picker.Categories.FirstOrDefault(category => category.CategoryId == payment.CategoryId)?.Name ?? "";
        StateNote = payment.Status switch
        {
            RecurringPaymentStatus.Active => "",
            RecurringPaymentStatus.Paused => "In pausa: non crea spese finché non lo riprendi.",
            RecurringPaymentStatus.Ended => $"Terminato il {ItalianFormat.FullDate(payment.EndsOn!.Value)}: non crea più spese."
        };
        // Every property that reads the mode changes with it.
        OnPropertyChanged("");
    }

    private async Task Save()
    {
        var amount = MoneyInputParser.ParsePositive(Amount, ItalianCulture);
        var endsOnDate = EndIndex == OnADate;
        AmountError = amount.IsFailure ? amount.Error.Message : "";
        EndDateError = endsOnDate && EndDate < ScheduleDate ? ApplicationErrors.InvalidEndDate.Message : "";
        ErrorMessage = "";
        // Every check runs, so every field that stops the save says so at once.
        var hasCategory = Picker.Validate();
        if (amount.IsFailure || EndDateError.HasValue() || !hasCategory)
        {
            Rejected?.Invoke(this, EventArgs.Empty);
            return;
        }

        IsSaving = true;
        var request = new SaveRecurringPaymentRequest(
            Picker.CategoryId!.Value,
            amount.Value.Cents,
            Frequencies[FrequencyIndex],
            DateOnly.FromDateTime(ScheduleDate),
            Picker.TagId,
            Note,
            endsOnDate ? DateOnly.FromDateTime(EndDate) : null,
            _paymentId.HasValue ? DateOnly.FromDateTime(_original.ScheduleDate) : null);
        Result result = _paymentId.HasValue
            ? await _paymentService.Update(_paymentId.Value, request)
            : await _paymentService.Create(request);
        IsSaving = false;
        if (result.IsFailure)
        {
            Explain(result.Error);
            Rejected?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (IsReminderOffered)
            _reminderOffer.Close();

        await _automation.ProcessDue(waitForCurrentPass: true);
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

    // Pause, resume and end act on the stored payment. The due payments are processed again because each of them
    // changes which expenses and reminders are due.
    private async Task<Result> Change(Func<Guid, Task<Result>> change)
    {
        var result = await change(_paymentId.GetValueOrDefault());
        if (result.IsFailure)
            ErrorMessage = result.Error.Message;
        else
            await _automation.ProcessDue(waitForCurrentPass: true);

        return result;
    }

    private FormState Snapshot() => new(Amount.Trim(), Picker.CategoryId, Picker.TagId, FrequencyIndex, ScheduleDate.Date,
        EndIndex == OnADate ? EndDate.Date : null, Note.Trim());

    private readonly record struct FormState(string Amount, Guid? CategoryId, Guid? TagId, int FrequencyIndex,
        DateTime ScheduleDate, DateTime? EndDate, string Note);

    /// <summary>What the editor says and allows for a new payment and for each status of a stored one.</summary>
    private sealed record Mode(string Title, string DateLabel, string StateGlyph, bool CanSave, bool CanPause, bool CanResume)
    {
        public static readonly Mode New = new("Nuovo pagamento", "Prima spesa il", "", CanSave: true, CanPause: false, CanResume: false);

        public static Mode Of(RecurringPaymentStatus status) => status switch
        {
            RecurringPaymentStatus.Active => new("Modifica pagamento", "Prossima spesa il", "", CanSave: true, CanPause: true, CanResume: false),
            RecurringPaymentStatus.Paused => new("Modifica pagamento", "Prossima spesa il", MaterialSymbols.PauseCircle, CanSave: true, CanPause: false, CanResume: true),
            RecurringPaymentStatus.Ended => new("Pagamento terminato", "Ultima spesa il", MaterialSymbols.EventBusy, CanSave: false, CanPause: false, CanResume: false)
        };
    }
}
