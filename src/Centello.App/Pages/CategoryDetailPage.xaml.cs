using Centello.App.ViewModels;
using Centello.Application;

namespace Centello.App.Pages;

/// <summary>One category over a month or a year: what it spent against its budget, when, and its expenses, which the
/// tags filter.</summary>
public partial class CategoryDetailPage : ContentPage, IQueryAttributable
{
    private readonly CategoryDetailViewModel _viewModel;
    private bool _isInitialized;

    public CategoryDetailPage(CategoryDetailViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    /// <summary>The route that opens a category on a month or on a year.</summary>
    public static string Route(Guid categoryId, Period period) =>
        $"{nameof(CategoryDetailPage)}?categoryId={categoryId}&year={period.Start.Year}"
        + (period.Kind == PeriodKind.Month ? $"&month={period.Start.Month}" : "");

    // Shell applies the query again when the page comes back to the front. Only the first one opens the page: later
    // the period is the one the person moved to.
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (_isInitialized
            || !query.TryGetValue("categoryId", out var categoryValue) || !Guid.TryParse(categoryValue?.ToString(), out var categoryId)
            || !query.TryGetValue("year", out var yearValue) || !int.TryParse(yearValue?.ToString(), out var year))
            return;

        _isInitialized = true;
        var period = query.TryGetValue("month", out var monthValue) && int.TryParse(monthValue?.ToString(), out var month)
            ? Period.Month(year, month)
            : Period.Year(year);
        _viewModel.Initialize(categoryId, period);
    }

    // The page appears again when an expense or the category editor closes: what it shows is read again.
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.Load();
    }

    private async void OnBackClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");

    private async void OnEditClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync(CategoryEditorPage.Route(_viewModel.CategoryId));

    private async void OnPreviousTapped(object? sender, EventArgs e) => await _viewModel.ShowPrevious();

    private async void OnNextTapped(object? sender, EventArgs e) => await _viewModel.ShowNext();

    private async void OnPeriodToggled(object? sender, EventArgs e) => await _viewModel.TogglePeriod();

    private async void OnTagChipTapped(object? sender, EventArgs e)
    {
        if (sender is BindableObject { BindingContext: TagChip { IsSelected: false } chip })
            await _viewModel.Filter(chip.Filter);
    }

    private async void OnExpenseOpened(object? sender, Guid expenseId) =>
        await Shell.Current.GoToAsync(ExpenseEditorPage.Route(expenseId));
}
