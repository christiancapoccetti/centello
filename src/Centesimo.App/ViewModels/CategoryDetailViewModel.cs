using Centesimo.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Centesimo.App.ViewModels;

public sealed class CategoryDetailViewModel(IServiceScopeFactory scopeFactory) : ObservableObject
{
    private Guid _categoryId;
    private Period? _period;
    private TagFilter _tag = TagFilter.All;
    private CategoryDetail? _detail;
    private IReadOnlyList<ExpenseGroup> _groups = [];
    private string _errorMessage = "";

    public Guid CategoryId => _categoryId;
    public CategoryDetail? Detail
    {
        get => _detail;
        private set
        {
            if (!SetProperty(ref _detail, value))
                return;

            OnPropertyChanged(nameof(IsWithinBudget));
            OnPropertyChanged(nameof(HasTagChips));
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(BarsDescription));
        }
    }
    public IReadOnlyList<ExpenseGroup> Groups { get => _groups; private set => SetProperty(ref _groups, value); }
    /// <summary>True while there is a budget and the spending is inside it: the line then says what is left.</summary>
    public bool IsWithinBudget => Detail is { HasBudget: true, IsOverBudget: false };
    /// <summary>"Tutti" and "Senza tag" alone filter nothing: the row of chips shows once a tag has spending.</summary>
    public bool HasTagChips => Detail?.Tags.Any(chip => chip.Filter is TagFilter.SingleTag) == true;
    public bool IsEmpty => Detail is { HasNoGroups: true };
    public string BarsDescription => Detail is null
        ? ""
        : $"Spesa {(Detail.Period.Kind == PeriodKind.Month ? "giornaliera" : "mensile")} di {Detail.Name} {ItalianFormat.InPeriod(Detail.Period)}";
    public string ErrorMessage { get => _errorMessage; private set => SetProperty(ref _errorMessage, value); }

    public void Initialize(Guid categoryId, Period period)
    {
        _categoryId = categoryId;
        _period = period;
    }

    public Task Load() => Show(_period, _tag);

    public Task ShowPrevious() => Show(Detail?.Navigation.Previous, _tag);

    public Task ShowNext() => Show(Detail?.Navigation.Next, _tag);

    public Task TogglePeriod() => Show(Detail?.Navigation.Toggle, _tag);

    public Task Filter(TagFilter tag) => Show(_period, tag);

    private async Task Show(Period? period, TagFilter tag)
    {
        if (period is null)
            return;

        ErrorMessage = "";
        Result<CategoryDetail> result;
        using (var scope = scopeFactory.CreateScope())
            result = await scope.ServiceProvider.GetRequiredService<CategoryDetailService>().Get(_categoryId, period, tag);

        if (result.IsFailure)
        {
            ErrorMessage = result.Error.Message;
            return;
        }

        _period = period;
        _tag = tag;
        if (!ExpenseGroup.Same(Groups, result.Value.Groups))
            Groups = result.Value.Groups.Select(group => new ExpenseGroup(group)).ToList();

        Detail = result.Value;
    }
}
