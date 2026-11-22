using Centello.App.Controls;
using Centello.App.ViewModels;
using Centello.Application;

namespace Centello.App.Pages;

public partial class InsightsPage : ContentPage
{
    private readonly InsightsViewModel _viewModel;
    private readonly PhoneLandscapeTrigger _landscapeTrigger;
    private readonly BrowseScrollAnchor _scrollAnchor;

    public InsightsPage(InsightsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
        _scrollAnchor = new BrowseScrollAnchor(InsightsSummary, InsightsReports);
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
            PageLayout.Children.Remove(PeriodSelector);
            SummaryBody.Children.Insert(0, PeriodSelector);
            Spending.Children.Remove(Reports);
            InsightsReports.Content = Reports;
            return;
        }

        SummaryBody.Children.Remove(PeriodSelector);
        PageLayout.Children.Insert(1, PeriodSelector);
        InsightsReports.Content = null;
        Spending.Children.Add(Reports);
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

    private async void OnRecapClicked(object? sender, EventArgs e)
    {
        if (_viewModel.Recap is { } recap)
            await Shell.Current.GoToAsync(RecapPage.Route(recap.Period));
    }

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
