using Centello.App.Controls;
using Centello.App.ViewModels;
using Centello.Application;

namespace Centello.App.Pages;

public partial class TodayPage : ContentPage
{
    private readonly TodayViewModel _viewModel;
    private readonly PhoneLandscapeTrigger _landscapeTrigger;
    private readonly BrowseScrollAnchor _scrollAnchor;
    private readonly ExpenseDraftService _drafts;
    private readonly DraftConsent _draftConsent;
    private readonly PhotoPicker _photos;
    private const string MicrophoneRationale = "Centello usa il microfono solo mentre tieni premuto il pulsante. L’audio registrato viene inviato a Google per preparare la bozza.";

    private readonly ISnackbar _snackbar;
    private Task<Result>? _speechStart;
    private CancellationTokenSource? _reading;
    private int _speechSession;
    private bool _speechReleased;
    private long _speechPressedAt;

    public TodayPage(TodayViewModel viewModel, ExpenseDraftService drafts, DraftConsent draftConsent, PhotoPicker photos, ISnackbar snackbar)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _drafts = drafts;
        _draftConsent = draftConsent;
        _photos = photos;
        _snackbar = snackbar;
        viewModel.CanTakePhoto = PhotoPicker.CanCapture;
        // The scroll anchor can be a generated row in the section that changes parents.
        Activity.BindingContext = viewModel;
        BindingContext = viewModel;
        _scrollAnchor = new BrowseScrollAnchor(TodayOverview, TodayActivity);
        _landscapeTrigger = BrowseLayout.FindTrigger(PageLayout);
        _landscapeTrigger.IsActiveChanged += OnLandscapeChanged;
    }

    private void OnLandscapeChanged(object? sender, EventArgs e)
    {
        _scrollAnchor.CaptureBeforeReflow();
        ArrangePanes();
        _scrollAnchor.RestoreAfterLayout();
    }

    private void ArrangePanes()
    {
        if (_landscapeTrigger.IsActive)
        {
            Overview.Children.Remove(Activity);
            TodayActivity.Content = Activity;
            return;
        }

        TodayActivity.Content = null;
        Overview.Children.Add(Activity);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.HasDraftConsent = _draftConsent.IsEnabled;
        await _viewModel.Load();
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        CancelReading();
        await CancelSpeech();
    }

    protected override bool OnBackButtonPressed()
    {
        if (_viewModel.Capture.IsIdle && _speechStart is null)
            return base.OnBackButtonPressed();

        _ = CancelSpeech();
        CancelReading();
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
        if (!_viewModel.IsCaptureAvailable || !_viewModel.Capture.IsIdle || _speechStart is not null)
            return;

        var session = ++_speechSession;
        _speechPressedAt = Environment.TickCount64;
        _speechReleased = false;
        _speechStart = StartSpeech(session);
    }

    private async Task<Result> StartSpeech(int session)
    {
        try
        {
            var permission = await RequestPermission<Permissions.Microphone>("Microfono", MicrophoneRationale);
            if (session != _speechSession)
                return Result.Failure(DraftErrors.Canceled);

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

    private void OnCameraClicked(object? sender, EventArgs e)
    {
        if (_speechStart is null)
            _viewModel.Capture = _viewModel.Capture.ChoosePhoto();
    }

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

        var picking = _viewModel.Capture.Pick(source);
        _viewModel.Capture = picking;
        var location = Shell.Current.CurrentState.Location;
        Result<byte[]?> picked;
        try
        {
            picked = await take();
        }
        catch
        {
            picked = Result<byte[]?>.Failure(PhotoErrors.ProcessingFailed);
        }

        if (!ReferenceEquals(_viewModel.Capture, picking))
            return;

        if (Shell.Current.CurrentState.Location != location)
        {
            _viewModel.Capture = picking.Close();
            return;
        }

        if (picked.IsFailure)
        {
            _viewModel.Capture = picking.Read(source).Fail(picked.Error);
            return;
        }

        if (picked.Value is not { } bytes)
        {
            _viewModel.Capture = picking.Close();
            return;
        }

        _viewModel.Capture = picking.Read(source);
        await ReadDraft(token => _drafts.PrepareFromDocument(bytes, token), PhotoErrors.ProcessingFailed);
    }

    private async void OnSpeechReleased(object? sender, EventArgs e)
    {
        var start = _speechStart;
        if (start is null || _speechReleased)
            return;

        _speechReleased = true;
        var session = _speechSession;
        try
        {
            if (SpeechRecordingLimits.IsTap(TimeSpan.FromMilliseconds(Environment.TickCount64 - _speechPressedAt)))
            {
                await ExplainHold(start, session);
                return;
            }

            var startResult = await start;
            if (session != _speechSession)
                return;

            _viewModel.Capture = _viewModel.Capture.Read(DraftSource.Voice);
            if (startResult.IsFailure)
            {
                _viewModel.Capture = _viewModel.Capture.Fail(startResult.Error);
                return;
            }

            await ReadDraft(token => _drafts.PrepareFromSpeech(WhenFull.Refuse, token), SpeechErrors.ProcessingFailed);
        }
        catch
        {
            if (session == _speechSession)
                _viewModel.Capture = _viewModel.Capture.Read(DraftSource.Voice).Fail(SpeechErrors.ProcessingFailed);
        }
        finally
        {
            if (session == _speechSession)
                _speechStart = null;
        }
    }

    private async Task ReadDraft(Func<CancellationToken, Task<Result<ExpenseDraft>>> prepare, Error failure)
    {
        var capture = _viewModel.Capture;
        if (!capture.IsReading)
            return;

        using var reading = new CancellationTokenSource();
        _reading = reading;
        try
        {
            var draft = await prepare(reading.Token);
            if (reading.IsCancellationRequested || !ReferenceEquals(_viewModel.Capture, capture))
                return;

            if (draft.IsFailure)
            {
                _viewModel.Capture = _viewModel.Capture.Fail(draft.Error);
                return;
            }

            _viewModel.Capture = _viewModel.Capture.Finish();
            await ExpenseEditorPage.OpenDraft(draft.Value);
        }
        catch
        {
            if (reading.IsCancellationRequested)
                return;

            if (ReferenceEquals(_viewModel.Capture, capture))
                _viewModel.Capture = capture.Fail(failure);
            else if (_viewModel.Capture.IsIdle)
                _snackbar.Show(failure.Message);
        }
        finally
        {
            if (ReferenceEquals(_reading, reading))
                _reading = null;
        }
    }

    private async Task CancelSpeech()
    {
        ++_speechSession;
        _speechStart = null;
        if (!_viewModel.Capture.IsListening)
            return;

        _viewModel.Capture = _viewModel.Capture.Close();
        await _drafts.CancelListening();
    }

    private void CancelReading()
    {
        if (!_viewModel.Capture.IsReading)
            return;

        _reading?.Cancel();
        _viewModel.Capture = _viewModel.Capture.Finish();
    }

    // A tap is not a dictation: whatever started to record is dropped and the gesture is explained instead.
    private async Task ExplainHold(Task<Result> start, int session)
    {
        var started = await start;
        if (session != _speechSession)
            return;

        if (started.IsSuccess)
            await _drafts.CancelListening();

        _viewModel.Capture = _viewModel.Capture.Close();
        _snackbar.Show("Tieni premuto il microfono per dettare una spesa");
    }

    private void OnCloseCaptureClicked(object? sender, EventArgs e)
    {
        CancelReading();
        _viewModel.Capture = _viewModel.Capture.Close();
    }

    private async void OnInactiveCaptureClicked(object? sender, EventArgs e)
    {
        var openSettings = await DisplayAlertAsync(
            "Bozze assistite",
            DraftErrors.ConsentRequired.Message,
            "Apri impostazioni",
            "Annulla");
        if (openSettings)
            await Shell.Current.GoToAsync($"//{nameof(SettingsPage)}");
    }
}
