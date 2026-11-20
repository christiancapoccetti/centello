using Centesimo.App.ViewModels;
using Centesimo.Application;

namespace Centesimo.App.Pages;

public partial class TodayPage : ContentPage
{
    private readonly TodayViewModel _viewModel;
    private readonly ExpenseDraftService _drafts;
    private readonly OpenAiSettings _settings;
    private readonly ISnackbar _snackbar;
    private Task<Result>? _speechStart;
    private int _speechSession;
    private long _speechPressedAt;

    public TodayPage(TodayViewModel viewModel, ExpenseDraftService drafts, OpenAiSettings settings, ISnackbar snackbar)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _drafts = drafts;
        _settings = settings;
        _snackbar = snackbar;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var load = _viewModel.Load();
        var availability = _settings.HasApiKey();
        await Task.WhenAll(load, availability);
        _viewModel.IsCaptureAvailable = await availability;
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        if (!_drafts.IsListening)
            return;

        await _drafts.CancelListening();
        _viewModel.Capture = _viewModel.Capture.Close();
    }

    protected override bool OnBackButtonPressed()
    {
        if (_viewModel.Capture.IsIdle)
            return base.OnBackButtonPressed();

        _viewModel.Capture = _viewModel.Capture.Close();
        return true;
    }

    private async void OnPreviousTapped(object? sender, EventArgs e) =>
        await _viewModel.ShowPrevious();

    private async void OnNextTapped(object? sender, EventArgs e) =>
        await _viewModel.ShowNext();

    private async void OnPeriodToggled(object? sender, EventArgs e) =>
        await _viewModel.TogglePeriod();

    private async void OnExpenseOpened(object? sender, Guid expenseId) =>
        await Shell.Current.GoToAsync($"{nameof(ExpenseEditorPage)}?expenseId={expenseId}");

    private async void OnCategoryKeyTapped(object? sender, TappedEventArgs e)
    {
        if (sender is BindableObject { BindingContext: CategoryKeyItem category } && _viewModel.Overview is { } overview)
            await Shell.Current.GoToAsync(CategoryDetailPage.Route(category.CategoryId, overview.Period));
    }

    private async void OnAllExpensesTapped(object? sender, EventArgs e)
    {
        if (_viewModel.HistoryMonth is { } month)
            await Shell.Current.GoToAsync(ExpensesPage.Route(month));
    }

    private async void OnAddExpenseClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(ExpenseEditorPage));

    private void OnSuggestionTapped(object? sender, EventArgs e)
    {
        if (sender is BindableObject { BindingContext: SuggestedCategoryChip chip })
            _viewModel.Toggle(chip);
    }

    private async void OnCreateSuggestedClicked(object? sender, EventArgs e)
    {
        var created = await _viewModel.CreateSuggested();
        if (created.IsFailure)
            return;

        _snackbar.Show(created.Value == 1
            ? "1 categoria creata: ora puoi aggiungere le spese"
            : $"{created.Value} categorie create: ora puoi aggiungere le spese");
    }

    private async void OnCreateByHandClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync(CategoryEditorPage.Route());

    private void OnSpeechPressed(object? sender, EventArgs e)
    {
        if (!_viewModel.IsCaptureAvailable || !_viewModel.Capture.IsIdle)
            return;

        var session = ++_speechSession;
        _speechPressedAt = Environment.TickCount64;
        _speechStart = StartSpeech(session);
    }

    private async Task<Result> StartSpeech(int session)
    {
        try
        {
            var permission = await RequestMicrophonePermission();
            if (permission != PermissionStatus.Granted)
                return Result.Failure(SpeechErrors.MicrophoneDenied);

            var result = await _drafts.StartListening();
            if (session != _speechSession || result.IsFailure)
            {
                if (result.IsSuccess)
                    await _drafts.CancelListening();
                return result;
            }

            _viewModel.Capture = _viewModel.Capture.Listen();
            return result;
        }
        catch
        {
            return Result.Failure(SpeechErrors.StartFailed);
        }
    }

    private async Task<PermissionStatus> RequestMicrophonePermission()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
        if (status == PermissionStatus.Granted)
            return status;

        var accepted = await DisplayAlertAsync(
            "Microfono",
            "Centesimo usa il microfono solo mentre tieni premuto il pulsante. L'audio registrato viene inviato a OpenAI per la trascrizione.",
            "Consenti",
            "Non ora");
        return accepted
            ? await Permissions.RequestAsync<Permissions.Microphone>()
            : status;
    }

    private async void OnSpeechReleased(object? sender, EventArgs e)
    {
        var start = _speechStart;
        if (start is null)
            return;

        _speechStart = null;
        if (SpeechRecordingLimits.IsTap(TimeSpan.FromMilliseconds(Environment.TickCount64 - _speechPressedAt)))
        {
            await ExplainHold(start);
            return;
        }

        try
        {
            var startResult = await start;
            _viewModel.Capture = _viewModel.Capture.Read(DraftSource.Voice);
            if (startResult.IsFailure)
            {
                _viewModel.Capture = _viewModel.Capture.Fail(startResult.Error);
                return;
            }

            await Show(await _drafts.PrepareFromSpeech(), _drafts.LastTranscription);
        }
        catch
        {
            _viewModel.Capture = _viewModel.Capture.Read(DraftSource.Voice).Fail(SpeechErrors.ProcessingFailed);
        }
    }

    private async Task Show(Result<ExpenseDraft> draft, string heard = "")
    {
        if (draft.IsFailure)
        {
            _viewModel.Capture = _viewModel.Capture.Fail(draft.Error, heard);
            return;
        }

        // The editor is the confirmation step: the sheet only prepares a draft and never saves anything.
        _viewModel.Capture = _viewModel.Capture.Finish();
        await ExpenseEditorPage.OpenDraft(draft.Value);
    }

    // A tap is not a dictation: whatever started to record is dropped and the gesture is explained instead.
    private async Task ExplainHold(Task<Result> start)
    {
        var started = await start;
        if (started.IsSuccess)
            await _drafts.CancelListening();

        _viewModel.Capture = _viewModel.Capture.Close();
        _snackbar.Show("Tieni premuto il microfono per dettare una spesa");
    }

    private void OnCloseCaptureClicked(object? sender, EventArgs e) =>
        _viewModel.Capture = _viewModel.Capture.Close();

    private async void OnInactiveSpeechClicked(object? sender, EventArgs e)
    {
        var openSettings = await DisplayAlertAsync(
            "Inserimento vocale",
            "Configura la chiave API OpenAI nelle impostazioni per usare i comandi vocali.",
            "Apri impostazioni",
            "Annulla");
        if (openSettings)
            await Shell.Current.GoToAsync($"//{nameof(SettingsPage)}");
    }
}
