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
    private readonly DraftConsent _draftConsent;
    private readonly PhotoPicker _photos;
    private readonly ISnackbar _snackbar;
    private readonly Func<Dictation> _createDictation;
    private readonly IDispatcherTimer _speechLimit;
    private CancellationTokenSource? _reading;
    private Dictation? _speech;
    private Window? _window;

    public TodayPage(TodayViewModel viewModel, ExpenseDraftService drafts, DraftConsent draftConsent, PhotoPicker photos,
        ISnackbar snackbar, Func<Dictation> createDictation)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _drafts = drafts;
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

        CancelReading();
        await CancelSpeech();
    }

    private async void OnWindowStopped(object? sender, EventArgs e) => await CancelSpeech();

    protected override bool OnBackButtonPressed()
    {
        if (_viewModel.Capture.IsIdle && _speech is null)
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

    private async void OnSpeechClicked(object? sender, EventArgs e)
    {
        if (_speech is { } active)
        {
            await SendSpeech(active);
            return;
        }

        if (!_viewModel.IsCaptureAvailable || !_viewModel.Capture.IsIdle)
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
            _viewModel.Capture = _viewModel.Capture.Read(DraftSource.Voice).Fail(failed.Error);
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
            _viewModel.Capture = _viewModel.Capture.Fail(failed.Error);
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

    private void OnCameraClicked(object? sender, EventArgs e)
    {
        if (_speech is null)
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
