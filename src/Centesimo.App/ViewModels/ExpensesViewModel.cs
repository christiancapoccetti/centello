using Centesimo.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Centesimo.App.ViewModels;

/// <summary>One group of an expense list in the shape a grouped list takes: the rows, with the texts of their header.</summary>
public sealed class ExpenseGroup(HistoryGroup group) : List<HistoryRow>(group.Rows)
{
    public string Label { get; } = group.Label;
    public string Total { get; } = group.Total;

    /// <summary>True when both lists show the same rows under the same headers. A list that did not change is left
    /// alone, so coming back from an expense keeps the place it was opened from.</summary>
    public static bool Same(IReadOnlyList<ExpenseGroup> first, IReadOnlyList<HistoryGroup> second) =>
        first.Count == second.Count && first
            .Zip(second)
            .All(pair => pair.First.Label == pair.Second.Label
                && pair.First.Total == pair.Second.Total
                && pair.First.SequenceEqual(pair.Second.Rows));
}

/// <summary>A chip of the category filter. No category is "Tutte".</summary>
public sealed record CategoryFilterChip(Guid? CategoryId, string Name, string ColorKey, bool IsSelected);

public sealed class ExpensesViewModel(IServiceScopeFactory scopeFactory) : ObservableObject
{
    private Period? _period;
    private Guid? _categoryId;
    private ExpenseHistory? _history;
    private IReadOnlyList<ExpenseGroup> _groups = [];
    private IReadOnlyList<CategoryFilterChip> _chips = [];
    private string _errorMessage = "";

    public ExpenseHistory? History
    {
        get => _history;
        private set
        {
            if (!SetProperty(ref _history, value))
                return;

            OnPropertyChanged(nameof(HasGroups));
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(HasFilter));
        }
    }
    public IReadOnlyList<ExpenseGroup> Groups { get => _groups; private set => SetProperty(ref _groups, value); }
    public IReadOnlyList<CategoryFilterChip> Chips { get => _chips; private set => SetProperty(ref _chips, value); }
    public bool HasGroups => History is { HasGroups: true };
    public bool IsEmpty => History is { HasNoGroups: true };
    public bool HasFilter => _categoryId.HasValue;
    public string ErrorMessage { get => _errorMessage; private set => SetProperty(ref _errorMessage, value); }

    /// <summary>The month to open on and the category to list, or every category when null.</summary>
    public void Initialize(Period period, Guid? categoryId)
    {
        _period = period;
        _categoryId = categoryId;
    }

    public Task Load() => Show(_period, _categoryId);

    public Task ShowPrevious() => Show(History?.Navigation.Previous, _categoryId);

    public Task ShowNext() => Show(History?.Navigation.Next, _categoryId);

    public Task Filter(Guid? categoryId) => Show(_period, categoryId);

    private async Task Show(Period? period, Guid? categoryId)
    {
        if (period is null)
            return;

        ErrorMessage = "";
        var filter = categoryId.HasValue ? ExpenseFilter.ForCategory(categoryId.Value) : ExpenseFilter.All;
        Result<ExpenseHistory> result;
        using (var scope = scopeFactory.CreateScope())
            result = await scope.ServiceProvider.GetRequiredService<ExpenseHistoryService>().Get(period, filter);

        if (result.IsFailure)
        {
            ErrorMessage = result.Error.Message;
            return;
        }

        _period = period;
        _categoryId = categoryId;
        var history = result.Value;
        if (!ExpenseGroup.Same(Groups, history.Groups))
            Groups = history.Groups.Select(group => new ExpenseGroup(group)).ToList();

        Chips =
        [
            new CategoryFilterChip(null, "Tutte", "", !categoryId.HasValue),
            .. history.CategoryChips.Select(chip => new CategoryFilterChip(chip.CategoryId, chip.Name, chip.ColorKey, chip.IsSelected))
        ];
        History = history;
    }
}
