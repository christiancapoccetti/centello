using Centello.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Centello.App.ViewModels;

public sealed class CategoriesViewModel(IServiceScopeFactory scopeFactory) : ObservableObject
{
    private const int ActiveSegment = 0;
    private CategoryList? _list;
    private int _segmentIndex = ActiveSegment;
    private string _errorMessage = "";

    /// <summary>The segment in view: the categories in use first, then the archived ones.</summary>
    public int SegmentIndex
    {
        get => _segmentIndex;
        set
        {
            if (SetProperty(ref _segmentIndex, value))
                NotifyRows();
        }
    }
    public bool IsActiveView => SegmentIndex == ActiveSegment;
    public IReadOnlyList<CategoryRow> Rows => (IsActiveView ? _list?.Active : _list?.Archived) ?? [];
    public bool HasRows => Rows.Count > 0;
    public bool HasNoActive => _list is not null && IsActiveView && !HasRows;
    public bool HasNoArchived => _list is not null && !IsActiveView && !HasRows;
    public string ErrorMessage { get => _errorMessage; private set => SetProperty(ref _errorMessage, value); }

    public async Task Load()
    {
        ErrorMessage = "";
        Result<CategoryList> result;
        using (var scope = scopeFactory.CreateScope())
            result = await scope.ServiceProvider.GetRequiredService<CategoryListService>().Get();

        if (result.IsFailure)
        {
            ErrorMessage = result.Error.Message;
            return;
        }

        _list = result.Value;
        NotifyRows();
    }

    private void NotifyRows()
    {
        OnPropertyChanged(nameof(IsActiveView));
        OnPropertyChanged(nameof(Rows));
        OnPropertyChanged(nameof(HasRows));
        OnPropertyChanged(nameof(HasNoActive));
        OnPropertyChanged(nameof(HasNoArchived));
    }
}
