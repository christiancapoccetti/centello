using Centesimo.Application;
using Centesimo.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace Centesimo.App.ViewModels;

public sealed class TodayViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    // Null follows the month that contains today, so a home left open moves on when the month changes.
    private Period? _selected;
    private HomeOverview? _overview;
    private bool _isLoading;
    private string _errorMessage = "";
    private bool _isSpeechSheetVisible;
    private bool _isSpeechListening;
    private bool _isSpeechProcessing;
    private bool _isSpeechAvailable;
    private string _speechTranscription = "";
    private string _speechErrorMessage = "";

    public TodayViewModel(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public HomeOverview? Overview
    {
        get => _overview;
        private set
        {
            if (SetProperty(ref _overview, value))
                OnPropertyChanged(nameof(HasOverview));
        }
    }
    public bool HasOverview => Overview is not null;
    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value))
                OnPropertyChanged(nameof(HasError));
        }
    }
    public bool HasError => ErrorMessage.HasValue();
    public bool IsSpeechSheetVisible { get => _isSpeechSheetVisible; set => SetProperty(ref _isSpeechSheetVisible, value); }
    public bool IsSpeechListening { get => _isSpeechListening; set => SetProperty(ref _isSpeechListening, value); }
    public bool IsSpeechProcessing
    {
        get => _isSpeechProcessing;
        set
        {
            if (SetProperty(ref _isSpeechProcessing, value))
                OnPropertyChanged(nameof(IsSpeechIdle));
        }
    }
    public bool IsSpeechIdle => !IsSpeechProcessing;
    public bool IsSpeechAvailable
    {
        get => _isSpeechAvailable;
        set
        {
            if (SetProperty(ref _isSpeechAvailable, value))
                OnPropertyChanged(nameof(IsSpeechUnavailable));
        }
    }
    public bool IsSpeechUnavailable => !IsSpeechAvailable;
    public string SpeechTranscription
    {
        get => _speechTranscription;
        set
        {
            if (SetProperty(ref _speechTranscription, value))
                OnPropertyChanged(nameof(HasSpeechTranscription));
        }
    }
    public bool HasSpeechTranscription => SpeechTranscription.HasValue();
    public string SpeechErrorMessage { get => _speechErrorMessage; set { if (SetProperty(ref _speechErrorMessage, value)) OnPropertyChanged(nameof(HasSpeechError)); } }
    public bool HasSpeechError => SpeechErrorMessage.HasValue();

    public Task Load() => LoadPeriod(_selected);

    public Task ShowPrevious() => Navigate(Overview?.Navigation.Previous);

    public Task ShowNext() => Navigate(Overview?.Navigation.Next);

    public Task TogglePeriod() => Navigate(Overview?.Navigation.Toggle);

    private async Task Navigate(Period? target)
    {
        if (target is null)
            return;

        await LoadPeriod(target);
    }

    private async Task LoadPeriod(Period? period)
    {
        if (_isLoading)
            return;

        _isLoading = true;
        ErrorMessage = "";
        Result<HomeOverview> result;
        using (var scope = _scopeFactory.CreateScope())
            result = await scope.ServiceProvider.GetRequiredService<HomeOverviewService>().Get(period);

        _isLoading = false;
        if (result.IsFailure)
        {
            ErrorMessage = result.Error.Message;
            return;
        }

        // The current month has no next period. Reaching it again with the arrows must not pin the home to it.
        var followsToday = result.Value.Period.Kind == PeriodKind.Month && !result.Value.Navigation.HasNext;
        _selected = followsToday ? null : result.Value.Period;
        Overview = result.Value;
    }
}
