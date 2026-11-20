using Centesimo.App.ViewModels;
using Centesimo.Application;

namespace Centesimo.App.Pages;

public partial class InsightsPage : ContentPage
{
    private readonly InsightsViewModel _viewModel;

    public InsightsPage(InsightsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.Load();
    }

    private async void OnPreviousTapped(object? sender, EventArgs e) => await _viewModel.ShowPrevious();

    private async void OnNextTapped(object? sender, EventArgs e) => await _viewModel.ShowNext();

    private async void OnPeriodToggled(object? sender, EventArgs e) => await _viewModel.TogglePeriod();

    private async void OnAddExpenseClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync(ExpenseEditorPage.Route());

    // An observation about one expense opens it. One about a category leads to its expenses: the history of the
    // month with the category as filter, or the category itself when the period is a year.
    private async void OnInsightTapped(object? sender, EventArgs e)
    {
        if (sender is not BindableObject { BindingContext: InsightRow insight })
            return;

        var route = insight.Target switch
        {
            InsightTarget.ExpenseTarget expense => ExpenseEditorPage.Route(expense.ExpenseId),
            InsightTarget.CategoryTarget { Period.Kind: PeriodKind.Month } category => ExpensesPage.Route(category.Period, category.CategoryId),
            InsightTarget.CategoryTarget category => CategoryDetailPage.Route(category.CategoryId, category.Period),
            _ => ""
        };
        if (route.Length > 0)
            await Shell.Current.GoToAsync(route);
    }

    private async void OnCategoryClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: InsightCategoryRow category } && _viewModel.Overview is { } overview)
            await Shell.Current.GoToAsync(CategoryDetailPage.Route(category.CategoryId, overview.Period));
    }
}
