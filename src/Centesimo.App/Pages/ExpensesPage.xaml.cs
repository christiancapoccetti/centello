using Centesimo.App.Controls;
using Centesimo.App.ViewModels;
using Centesimo.Application;

namespace Centesimo.App.Pages;

/// <summary>The expense history: one month at a time, grouped by day, with a filter by category.</summary>
public partial class ExpensesPage : ContentPage, IQueryAttributable
{
    private readonly ExpensesViewModel _viewModel;
    private bool _isInitialized;

    public ExpensesPage(ExpensesViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    /// <summary>The route that opens the history on a month, listing one category or all of them.</summary>
    public static string Route(Period month, Guid? categoryId = null) =>
        $"{nameof(ExpensesPage)}?year={month.Start.Year}&month={month.Start.Month}"
        + (categoryId.HasValue ? $"&categoryId={categoryId}" : "");

    // Shell applies the query again when the page comes back to the front. Only the first one opens the page: later
    // the month and the filter are the ones the person moved to.
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (_isInitialized)
            return;

        _isInitialized = true;
        var month = query.TryGetValue("year", out var yearValue) && int.TryParse(yearValue?.ToString(), out var year)
            && query.TryGetValue("month", out var monthValue) && int.TryParse(monthValue?.ToString(), out var number)
                ? Period.Month(year, number)
                : Period.MonthOf(DateOnly.FromDateTime(DateTime.Today));
        Guid? categoryId = query.TryGetValue("categoryId", out var categoryValue)
            && Guid.TryParse(categoryValue?.ToString(), out var id)
                ? id
                : null;
        _viewModel.Initialize(month, categoryId);
    }

    // The page appears again when an expense closes: the list is read again, on the month and the filter it was left on.
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.Load();
        ShowSelectedChip();
    }

    private async void OnBackClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");

    private async void OnPreviousTapped(object? sender, EventArgs e) => await _viewModel.ShowPrevious();

    private async void OnNextTapped(object? sender, EventArgs e) => await _viewModel.ShowNext();

    private async void OnChipTapped(object? sender, EventArgs e)
    {
        if (sender is not BindableObject { BindingContext: CategoryFilterChip { IsSelected: false } chip })
            return;

        await _viewModel.Filter(chip.CategoryId);
        ShowSelectedChip();
    }

    private async void OnShowAllClicked(object? sender, EventArgs e) => await _viewModel.Filter(null);

    private async void OnExpenseOpened(object? sender, Guid expenseId) =>
        await Shell.Current.GoToAsync($"{nameof(ExpenseEditorPage)}?expenseId={expenseId}");

    // The selected chip can be beyond the edge of the row: a filter that comes from another page, or a chip tapped
    // while half in view. The chips are drawn again on every load, so the scroll waits for their layout.
    private void ShowSelectedChip() => Dispatcher.Dispatch(async () =>
    {
        var selected = ChipScroll.GetVisualTreeDescendants().OfType<Chip>().FirstOrDefault(chip => chip.IsSelected);
        if (selected is not null)
            await ChipScroll.ScrollToAsync(selected, ScrollToPosition.Center, true);
    });
}
