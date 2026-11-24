using Centello.Application;
using Centello.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace Centello.App.ViewModels;

/// <summary>A category the first run offers to create. Every one starts chosen.</summary>
public sealed class SuggestedCategoryChip(SuggestedCategory category) : ObservableObject
{
    private bool _isSelected = true;

    public string Name { get; } = category.Name;
    public string IconKey { get; } = category.IconKey;
    public string ColorKey { get; } = category.ColorKey;
    public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
}

public sealed class CategoryBudgetItem
{
    public CategoryBudgetItem(CategoryBudgetRow row)
    {
        CategoryId = row.CategoryId;
        Name = row.Name;
        IconKey = row.IconKey;
        ColorKey = row.ColorKey;

        var spent = CompactMoney(row.SpentCents);
        if (row.BudgetCents is not { } budget || row.RemainingCents is not { } remaining)
        {
            Amount = spent;
            Description = $"{Name}, {spent} spesi";
            return;
        }

        HasBudget = true;
        IsOverBudget = remaining < 0;
        Amount = IsOverBudget
            ? $"{CompactMoney(-remaining)} oltre budget"
            : $"{CompactMoney(remaining)} disponibili";
        BudgetDetail = $"{spent} spesi su {CompactMoney(budget)}";
        Description = $"{Name}, {BudgetDetail}, {Amount}";
        var progress = Math.Min(1, (double)row.SpentCents / budget);
        ProgressWidth = new GridLength(progress, GridUnitType.Star);
        RemainingWidth = new GridLength(1 - progress, GridUnitType.Star);
    }

    public Guid CategoryId { get; }
    public string Name { get; }
    public string IconKey { get; }
    public string ColorKey { get; }
    public string Amount { get; }
    public string BudgetDetail { get; } = "";
    public string Description { get; }
    public bool HasBudget { get; }
    public bool IsOverBudget { get; }
    public GridLength ProgressWidth { get; }
    public GridLength RemainingWidth { get; }

    private static string CompactMoney(long cents) => $"{ItalianFormat.Budget(cents)} €";
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
    private bool _hasDraftConsent;

    public TodayViewModel(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public HomeOverview? Overview
    {
        get => _overview;
        private set
        {
            if (!SetProperty(ref _overview, value))
                return;

            OnPropertyChanged(nameof(HasOverview));
            OnPropertyChanged(nameof(BudgetItems));
            OnPropertyChanged(nameof(SpendChartHeading));
            NotifyCaptureAvailability();
        }
    }
    public bool HasOverview => Overview is not null;
    public IReadOnlyList<CategoryBudgetItem> BudgetItems => Overview is { } overview
        ? [.. overview.BudgetRows.Select(row => new CategoryBudgetItem(row))]
        : [];
    public string SpendChartHeading => Overview?.Period.Kind == PeriodKind.Year ? "Spesa mensile" : "Spesa giornaliera";
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
    public bool HasDraftConsent
    {
        get => _hasDraftConsent;
        set
        {
            if (SetProperty(ref _hasDraftConsent, value))
                NotifyCaptureAvailability();
        }
    }
    public bool IsCaptureAvailable => HasDraftConsent && CanCapture;
    public bool CanSetUpCapture => !HasDraftConsent && CanCapture;
    public bool CanTakePhoto { get; set; }

    private bool CanCapture => Overview?.IsFirstRun != true;

    private void NotifyCaptureAvailability()
    {
        OnPropertyChanged(nameof(IsCaptureAvailable));
        OnPropertyChanged(nameof(CanSetUpCapture));
    }

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
