using System.Diagnostics;
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
    private readonly AiAccess _aiAccess;
    private readonly DraftConsent _draftConsent;
    private readonly PhotoPicker _photos;
    private readonly ISnackbar _snackbar;
    private readonly Func<Dictation> _createDictation;
    private readonly IDispatcherTimer _speechLimit;
    private CancellationTokenSource? _preflight;
    private CancellationTokenSource? _reading;
    private Dictation? _speech;
    private Window? _window;

    public TodayPage(TodayViewModel viewModel, ExpenseDraftService drafts, AiAccess aiAccess, DraftConsent draftConsent, PhotoPicker photos,
        ISnackbar snackbar, Func<Dictation> createDictation)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _drafts = drafts;
        _aiAccess = aiAccess;
        _draftConsent = draftConsent;
        _photos = photos;
        _snackbar = snackbar;
        _createDictation = createDictation;
        _speechLimit = Dispatcher.CreateTimer();
        _speechLimit.Interval = TimeSpan.FromSeconds(SpeechRecordingLimits.MaximumDurationSeconds);
        _speechLimit.IsRepeating = false;
        _speechLimit.Tick += OnSpeechLimit;
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
        if (Window is { } window)
        {
            _window = window;
            window.Stopped += OnWindowStopped;
        }

        _viewModel.HasDraftConsent = _draftConsent.IsEnabled;
        await _viewModel.Load();
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        if (_window is { } window)
        {
            window.Stopped -= OnWindowStopped;
            _window = null;
        }

        CancelPreflight();
        CloseAccessSheet();
        CancelReading();
        await CancelSpeech();
    }

    private async void OnWindowStopped(object? sender, EventArgs e)
    {
        CancelPreflight();
        await CancelSpeech();
    }

    protected override bool OnBackButtonPressed()
    {
        if (AccessSheet.IsVisible)
        {
            CloseAccessSheet();
            return true;
        }

        if (_viewModel.Capture.IsIdle && _speech is null && _preflight is null)
            return base.OnBackButtonPressed();

        CancelPreflight();
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

    private async void OnCategoryKeyTapped(object? sender, TappedEventArgs e)
    {
        if (sender is BindableObject { BindingContext: CategoryKeyItem category })
            await OpenCategory(category.CategoryId);
    }

    private async void OnCategoryBudgetClicked(object? sender, EventArgs e)
    {
        if (sender is BindableObject { BindingContext: CategoryBudgetItem category })
            await OpenCategory(category.CategoryId);
    }

    private async Task OpenCategory(Guid categoryId)
    {
        if (_viewModel.Overview is { } overview)
            await Shell.Current.GoToAsync(CategoryDetailPage.Route(categoryId, overview.Period));
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

    private async void OnSpeechClicked(object? sender, EventArgs e)
    {
        if (_speech is { } active)
        {
            await SendSpeech(active);
            return;
        }

        if (!_viewModel.IsCaptureAvailable || !_viewModel.Capture.IsIdle || !await CheckCapture(DraftSource.Voice))
            return;

        var speech = _createDictation();
        _speech = speech;
        try
        {
            await speech.Begin();
        }
        catch (Exception exception) when (speech.State is PanelState.Closed)
        {
            Debug.WriteLine(exception);
        }

        if (speech.State is PanelState.Listening)
        {
            _viewModel.Capture = _viewModel.Capture.Listen();
            _speechLimit.Start();
            SemanticScreenReader.Default.Announce("In registrazione. Tocca per fermare.");
            return;
        }

        _speech = null;
        if (speech.State is PanelState.Failed failed)
            ShowCaptureFailure(DraftSource.Voice, failed.Error);
        else if (speech.State is PanelState.NeedsConsent)
            _viewModel.HasDraftConsent = false;
    }

    private async void OnSpeechLimit(object? sender, EventArgs e)
    {
        if (_speech is { } speech)
            await SendSpeech(speech);
    }

    private async Task SendSpeech(Dictation speech)
    {
        if (speech.State is not PanelState.Listening)
            return;

        _speechLimit.Stop();
        _viewModel.Capture = _viewModel.Capture.Read(DraftSource.Voice);
        await speech.Send();
        _speech = null;

        if (speech.State is PanelState.Failed failed)
        {
            ShowCaptureFailure(DraftSource.Voice, failed.Error);
            return;
        }

        if (speech.State is not PanelState.DraftReady ready)
            return;

        _viewModel.Capture = _viewModel.Capture.Finish();
        try
        {
            await ExpenseEditorPage.OpenDraft(ready.Draft);
        }
        catch
        {
            _snackbar.Show(SpeechErrors.ProcessingFailed.Message);
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

    private async void OnCameraClicked(object? sender, EventArgs e)
    {
        if (_speech is null && _viewModel.Capture.IsIdle && await CheckCapture(DraftSource.Photo))
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
        var menu = _viewModel.Capture;
        if (!menu.IsChoosingPhoto || !await CheckCapture(source) || !ReferenceEquals(_viewModel.Capture, menu))
            return;

        var picking = menu.Pick(source);
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

    private async Task ReadDraft(Func<CancellationToken, Task<Result<ExpenseDraft>>> prepare, Error failure)
    {
        var capture = _viewModel.Capture;
        if (!capture.IsReading || capture.Source is not { } source)
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
                ShowCaptureFailure(source, draft.Error);
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
        if (_speech is not { } speech)
            return;

        var wasListening = speech.State is PanelState.Listening;
        _speechLimit.Stop();
        if (_viewModel.Capture.Source == DraftSource.Voice)
            _viewModel.Capture = _viewModel.Capture.Finish().Close();

        try
        {
            await speech.Cancel();
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
        }
        finally
        {
            if (wasListening)
                _speech = null;
        }
    }

    private void CancelReading()
    {
        if (!_viewModel.Capture.IsReading)
            return;

        _reading?.Cancel();
        _viewModel.Capture = _viewModel.Capture.Finish();
    }

    private async Task<bool> CheckCapture(DraftSource source)
    {
        if (_preflight is not null || AccessSheet.IsVisible)
            return false;

        using var preflight = new CancellationTokenSource();
        _preflight = preflight;
        try
        {
            var access = await _drafts.CheckCapture(preflight.Token);
            if (preflight.IsCancellationRequested)
                return false;

            if (access.IsFailure)
                ShowCaptureFailure(source, access.Error);

            return access.IsSuccess;
        }
        catch (OperationCanceledException) when (preflight.IsCancellationRequested)
        {
            return false;
        }
        finally
        {
            if (ReferenceEquals(_preflight, preflight))
                _preflight = null;
        }
    }

    private void CancelPreflight()
    {
        _preflight?.Cancel();
        _preflight = null;
    }

    private void ShowCaptureFailure(DraftSource source, Error error)
    {
        if (error == DraftErrors.ConsentRequired)
        {
            _viewModel.HasDraftConsent = false;
            _viewModel.Capture = DraftCapture.Idle;
            return;
        }

        if (PanelFaces.AccessFailure(error, _aiAccess.SurveyUrl) is not { } blocked)
        {
            _viewModel.Capture = _viewModel.Capture.Read(source).Fail(error);
            return;
        }

        _viewModel.Capture = DraftCapture.Idle;
        AccessSheet.BindingContext = blocked;
        AccessSheet.IsVisible = true;
        if (blocked.IsTrialExhausted)
        {
            _aiAccess.Changed += OnAiAccessChanged;
            RefreshSurvey();
        }

        SetBackgroundAccessibility(excluded: true);
        SemanticScreenReader.Default.Announce(blocked.Title);
    }

    private void CloseAccessSheet()
    {
        _aiAccess.Changed -= OnAiAccessChanged;
        AccessSheet.IsVisible = false;
        AccessSheet.BindingContext = null;
        SetBackgroundAccessibility(excluded: false);
    }

    private void OnAiAccessChanged() => Dispatcher.Dispatch(RefreshSurvey);

    private void RefreshSurvey()
    {
        if (!AccessSheet.IsVisible || AccessSheet.BindingContext is not PanelFace.AccessBlocked { IsTrialExhausted: true } blocked)
            return;

        var surveyUrl = _aiAccess.SurveyUrl;
        if (surveyUrl != blocked.SurveyUrl)
            AccessSheet.BindingContext = PanelFaces.AccessFailure(DraftErrors.TrialExhausted, surveyUrl);
    }

    private void SetBackgroundAccessibility(bool excluded)
    {
        AutomationProperties.SetExcludedWithChildren(PeriodHeader, excluded);
        AutomationProperties.SetExcludedWithChildren(TodayOverview, excluded);
        AutomationProperties.SetExcludedWithChildren(TodayActivity, excluded);
        AutomationProperties.SetExcludedWithChildren(CaptureDock, excluded);
    }

    private void OnAccessSheetDismissed(object? sender, EventArgs e) => CloseAccessSheet();

    private async void OnAccessManualClicked(object? sender, EventArgs e)
    {
        CloseAccessSheet();
        await Shell.Current.GoToAsync(ExpenseEditorPage.Route());
    }

    private async void OnAccessInviteClicked(object? sender, EventArgs e)
    {
        CloseAccessSheet();
        await Shell.Current.GoToAsync(AiInvitePage.Route);
    }

    private async void OnSurveyClicked(object? sender, EventArgs e)
    {
        if (AccessSheet.BindingContext is not PanelFace.AccessBlocked { CanOpenSurvey: true }
            || !Uri.TryCreate(_aiAccess.SurveyUrl, UriKind.Absolute, out var survey))
            return;

        try
        {
            if (await Browser.Default.OpenAsync(survey, BrowserLaunchMode.External))
                return;
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
        }

        _snackbar.Show("Non è stato possibile aprire il browser");
    }

    private void OnCloseCaptureClicked(object? sender, EventArgs e)
    {
        CancelReading();
        _viewModel.Capture = _viewModel.Capture.Close();
    }

    private async void OnInactiveCaptureClicked(object? sender, EventArgs e)
    {
        var openSettings = await DisplayAlertAsync(
            "Inserimento spese con l’AI",
            DraftErrors.ConsentRequired.Message,
            "Apri impostazioni",
            "Annulla");
        if (openSettings)
            await Shell.Current.GoToAsync($"//{nameof(SettingsPage)}");
    }
}
