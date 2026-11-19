using Centesimo.App.ViewModels;
using Centesimo.App.Controls;
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

    private async void OnPreviousMonthClicked(object? sender, EventArgs e) =>
        await _viewModel.PreviousMonth();

    private async void OnNextMonthClicked(object? sender, EventArgs e) =>
        await _viewModel.NextMonth();

    private async void OnCategoryClicked(object? sender, CategoryBreakdownItemClickedEventArgs e)
    {
        var period = _viewModel.IsYearlyOverview ? "&period=year" : "";
        await Shell.Current.GoToAsync($"{nameof(CategorySpendingPage)}?categoryId={e.Category.CategoryId}&year={_viewModel.SelectedYear}&month={_viewModel.SelectedMonth}{period}");
    }
    private void OnCardPressed(object? sender, EventArgs e) =>
        InteractionFeedback.Press(sender);

    private void OnCardReleased(object? sender, EventArgs e) =>
        InteractionFeedback.Release(sender);

    private async void OnAddExpenseClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(ExpenseEditorPage));

    private void OnSpeechPressed(object? sender, EventArgs e)
    {
        if (!_viewModel.IsSpeechAvailable)
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
            _viewModel.SpeechTranscription = "Tieni premuto il microfono e rilascia per elaborare.";
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
        try
        {
            var session = _speechSession;
            var start = _speechStart;
            if (start is null)
                return;

            var startResult = await start;
            _viewModel.IsSpeechListening = false;
            _viewModel.IsSpeechProcessing = true;
            _viewModel.IsSpeechSheetVisible = true;
            if (startResult.IsFailure)
            {
                _viewModel.IsSpeechProcessing = false;
                _viewModel.SpeechErrorMessage = $"Non ho capito bene. {startResult.Error.Message}";
                return;
            }

            var result = await _speechDraftService.StopAndPrepare();
            _viewModel.IsSpeechProcessing = false;
            _viewModel.SpeechTranscription = _speechDraftService.LastTranscription;
            if (result.IsFailure)
            {
                _viewModel.SpeechErrorMessage = $"Non ho capito bene. {result.Error.Message}";
                return;
            }

            _viewModel.IsSpeechSheetVisible = false;
            await Shell.Current.GoToAsync(nameof(ExpenseEditorPage));
        }
        catch
        {
            _viewModel.IsSpeechProcessing = false;
            _viewModel.SpeechErrorMessage = "Non riesco a elaborare il comando vocale.";
        }
        finally
        {
            _speechStart = null;
            _viewModel.IsSpeechListening = false;
            if (_viewModel.HasSpeechError)
                _viewModel.IsSpeechSheetVisible = true;
        }
    }

    private void OnCloseSpeechClicked(object? sender, EventArgs e)
    {
        _viewModel.IsSpeechListening = false;
        _viewModel.IsSpeechProcessing = false;
        _viewModel.IsSpeechSheetVisible = false;
        _viewModel.SpeechErrorMessage = "";
    }

    private void OnRetrySpeechClicked(object? sender, EventArgs e)
    {
        OnCloseSpeechClicked(sender, e);
        _viewModel.SpeechTranscription = "Tieni premuto il microfono per riprovare.";
    }

    private async void OnInactiveSpeechClicked(object? sender, EventArgs e)
    {
        var openSettings = await DisplayAlertAsync(
            "Inserimento rapido",
            "Configura la chiave API OpenAI nelle impostazioni per usare voce e testo libero.",
            "Apri impostazioni",
            "Annulla");
        if (openSettings)
            await Shell.Current.GoToAsync($"//{nameof(SettingsPage)}");
    }
}
