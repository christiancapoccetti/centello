using Centesimo.Application;
using Centesimo.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace Centesimo.App.ViewModels;

/// <summary>A category the first run offers to create. Every one starts chosen.</summary>
public sealed class SuggestedCategoryChip(SuggestedCategory category) : ObservableObject
{
    private bool _isSelected = true;

    public string Name { get; } = category.Name;
    public string IconKey { get; } = category.IconKey;
    public string ColorKey { get; } = category.ColorKey;
    public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
}

public sealed class TodayViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    // Null follows the month that contains today, so a home left open moves on when the month changes.
    private Period? _selected;
    private HomeOverview? _overview;
    private bool _isLoading;
    private string _errorMessage = "";
    private DraftCapture _capture = DraftCapture.Idle;
    private bool _isCaptureAvailable;

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
    public IReadOnlyList<SuggestedCategoryChip> Suggestions { get; } =
        [.. SuggestedCategories.All.Select(category => new SuggestedCategoryChip(category))];
    public bool CanCreateSuggested => Suggestions.Any(chip => chip.IsSelected);
    /// <summary>The month the expense history opens on: the month in view or, from a year, the month a tap on the
    /// title leads to.</summary>
    public Period? HistoryMonth => Overview?.Period.Kind == PeriodKind.Month ? Overview.Period : Overview?.Navigation.Toggle;
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
    public DraftCapture Capture { get => _capture; set => SetProperty(ref _capture, value); }
    public bool IsCaptureAvailable
    {
        get => _isCaptureAvailable;
        set
        {
            if (SetProperty(ref _isCaptureAvailable, value))
                OnPropertyChanged(nameof(IsCaptureUnavailable));
        }
    }
    public bool IsCaptureUnavailable => !IsCaptureAvailable;

    public Task Load() => LoadPeriod(_selected);

    public void Toggle(SuggestedCategoryChip chip)
    {
        chip.IsSelected = !chip.IsSelected;
        OnPropertyChanged(nameof(CanCreateSuggested));
    }

    /// <summary>Creates the chosen suggestions and reads the home again, which then leaves the first run. The value
    /// is how many categories were created.</summary>
    public async Task<Result<int>> CreateSuggested()
    {
        var names = Suggestions
            .Where(chip => chip.IsSelected)
            .Select(chip => chip.Name)
            .ToList();
        Result<IReadOnlyList<Category>> created;
        using (var scope = _scopeFactory.CreateScope())
            created = await scope.ServiceProvider.GetRequiredService<CategoryService>().CreateSuggested(names);

        if (created.IsFailure)
        {
            ErrorMessage = created.Error.Message;
            return Result<int>.Failure(created.Error);
        }

        await Load();
        return Result<int>.Success(created.Value.Count);
    }

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
