using Centesimo.App.ViewModels;
using Centesimo.Application;

namespace Centesimo.App.Pages;

public partial class TodayPage : ContentPage
{
    private readonly TodayViewModel _viewModel;
    private readonly ExpenseDraftService _drafts;
    private readonly OpenAiSettings _settings;
    private readonly PhotoPicker _photos;
    private const string MicrophoneRationale = "Centesimo usa il microfono solo mentre tieni premuto il pulsante. L'audio registrato viene inviato a OpenAI per la trascrizione.";

    private readonly ISnackbar _snackbar;
    private Task<Result>? _speechStart;
    private int _speechSession;
    private long _speechPressedAt;

    public TodayPage(TodayViewModel viewModel, ExpenseDraftService drafts, OpenAiSettings settings, PhotoPicker photos, ISnackbar snackbar)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _drafts = drafts;
        _settings = settings;
        _photos = photos;
        _snackbar = snackbar;
        viewModel.CanTakePhoto = PhotoPicker.CanCapture;
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
        await Shell.Current.GoToAsync(ExpenseEditorPage.Route(expenseId));

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
        await Shell.Current.GoToAsync(ExpenseEditorPage.Route());

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
            var permission = await RequestPermission<Permissions.Microphone>("Microfono", MicrophoneRationale);
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

    private async Task<PermissionStatus> RequestPermission<TPermission>(string title, string rationale)
        where TPermission : Permissions.BasePermission, new()
    {
        var status = await Permissions.CheckStatusAsync<TPermission>();
        if (status == PermissionStatus.Granted)
            return status;

        var accepted = await DisplayAlertAsync(title, rationale, "Consenti", "Non ora");
        return accepted
            ? await Permissions.RequestAsync<TPermission>()
            : status;
    }

    private void OnCameraClicked(object? sender, EventArgs e) =>
        _viewModel.Capture = _viewModel.Capture.ChoosePhoto();

    private void OnPhotoMenuDismissed(object? sender, TappedEventArgs e) =>
        _viewModel.Capture = _viewModel.Capture.Close();

    private async void OnTakePhotoClicked(object? sender, EventArgs e) =>
        await ReadDocument(DraftSource.Photo, TakePhoto);

    private async void OnPickPhotoClicked(object? sender, EventArgs e) =>
        await ReadDocument(DraftSource.Photo, _photos.Pick);

    private async void OnPickPdfClicked(object? sender, EventArgs e) =>
        await ReadDocument(DraftSource.Pdf, _photos.PickPdf);

    private async Task<Result<byte[]?>> TakePhoto()
    {
        var permission = await RequestPermission<Permissions.Camera>("Fotocamera", PhotoPicker.CameraRationale);
        if (permission != PermissionStatus.Granted)
            return Result<byte[]?>.Failure(PhotoErrors.CameraDenied);

        return await _photos.Capture();
    }

    private async Task ReadDocument(DraftSource source, Func<Task<Result<byte[]?>>> take)
    {
        if (!_viewModel.Capture.IsChoosingPhoto)
            return;

        _viewModel.Capture = _viewModel.Capture.Pick(source);
        try
        {
            var picked = await take();
            if (picked.IsFailure)
            {
                _viewModel.Capture = _viewModel.Capture.Read(source).Fail(picked.Error);
                return;
            }

            if (picked.Value is not { } bytes)
            {
                _viewModel.Capture = _viewModel.Capture.Close();
                return;
            }

            _viewModel.Capture = _viewModel.Capture.Read(source);
            await Show(await _drafts.PrepareFromDocument(bytes));
        }
        catch
        {
            _viewModel.Capture = _viewModel.Capture.Read(source).Fail(PhotoErrors.ProcessingFailed);
        }
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

            await Show(await _drafts.PrepareFromSpeech(WhenFull.Refuse), _drafts.LastTranscription);
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

    private async void OnInactiveCaptureClicked(object? sender, EventArgs e)
    {
        var openSettings = await DisplayAlertAsync(
            "Voce e foto con OpenAI",
            "Configura la chiave API OpenAI nelle impostazioni per dettare le spese o leggere una spesa da una foto o da un PDF.",
            "Apri impostazioni",
            "Annulla");
        if (openSettings)
            await Shell.Current.GoToAsync($"//{nameof(SettingsPage)}");
    }
}
