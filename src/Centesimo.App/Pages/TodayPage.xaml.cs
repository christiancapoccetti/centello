using Centesimo.App.ViewModels;
using Centesimo.Application;

namespace Centesimo.App.Pages;

public partial class TodayPage : ContentPage
{
    private readonly TodayViewModel _viewModel;
    private readonly SpeechExpenseDraftService _speechDraftService;
    private Task<Result>? _speechStart;
    private int _speechSession;

    public TodayPage(TodayViewModel viewModel, SpeechExpenseDraftService speechDraftService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _speechDraftService = speechDraftService;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var load = _viewModel.Load();
        var availability = _speechDraftService.IsAvailable();
        await Task.WhenAll(load, availability);
        _viewModel.IsSpeechAvailable = await availability;
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        if (!_speechDraftService.IsListening)
            return;

        await _speechDraftService.Cancel();
        _viewModel.IsSpeechListening = false;
        _viewModel.IsSpeechProcessing = false;
        _viewModel.IsSpeechSheetVisible = false;
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

    private void OnSpeechPressed(object? sender, EventArgs e)
    {
        if (!_viewModel.IsSpeechAvailable || _viewModel.IsSpeechProcessing)
            return;

        var session = ++_speechSession;
        _speechStart = StartSpeech(session);
    }

    private async Task<Result> StartSpeech(int session)
    {
        try
        {
            var permission = await RequestMicrophonePermission();
            if (permission != PermissionStatus.Granted)
                return Result.Failure(SpeechErrors.MicrophoneDenied);

            var result = await _speechDraftService.Start();
            if (session != _speechSession || result.IsFailure)
            {
                if (result.IsSuccess)
                    await _speechDraftService.Cancel();
                return result;
            }

            _viewModel.SpeechErrorMessage = "";
            _viewModel.IsSpeechListening = true;
            _viewModel.IsSpeechProcessing = false;
            return result;
        }
        catch
        {
            return Result.Failure(new Centesimo.Application.Error("Speech.StartFailed", "Non riesco ad avviare il riconoscimento vocale."));
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
        try
        {
            var startResult = await start;
            _viewModel.IsSpeechListening = false;
            _viewModel.IsSpeechProcessing = true;
            _viewModel.IsSpeechSheetVisible = true;
            if (startResult.IsFailure)
            {
                _viewModel.SpeechErrorMessage = startResult.Error.Message;
                return;
            }

            var result = await _speechDraftService.StopAndPrepare();
            _viewModel.SpeechTranscription = _speechDraftService.LastTranscription;
            if (result.IsFailure)
            {
                _viewModel.SpeechErrorMessage = result.Error.Message;
                return;
            }

            // The editor is the confirmation step: the sheet only prepares a draft and never saves anything.
            _viewModel.IsSpeechSheetVisible = false;
            _viewModel.SpeechTranscription = "";
            await Shell.Current.GoToAsync(nameof(ExpenseEditorPage));
        }
        catch
        {
            _viewModel.SpeechErrorMessage = "Non riesco a elaborare il comando vocale.";
        }
        finally
        {
            _viewModel.IsSpeechListening = false;
            _viewModel.IsSpeechProcessing = false;
            if (_viewModel.HasSpeechError)
                _viewModel.IsSpeechSheetVisible = true;
        }
    }

    private void OnCloseSpeechClicked(object? sender, EventArgs e)
    {
        _viewModel.IsSpeechListening = false;
        _viewModel.IsSpeechProcessing = false;
        _viewModel.IsSpeechSheetVisible = false;
        _viewModel.SpeechTranscription = "";
        _viewModel.SpeechErrorMessage = "";
    }

    private async void OnInactiveSpeechClicked(object? sender, EventArgs e)
    {
        var openSettings = await DisplayAlertAsync(
            "Inserimento rapido",
            "Configura la chiave API OpenAI nelle impostazioni per usare i comandi vocali.",
            "Apri impostazioni",
            "Annulla");
        if (openSettings)
            await Shell.Current.GoToAsync($"//{nameof(SettingsPage)}");
    }
}
