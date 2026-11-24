using Centello.App.Controls;
using Centello.App.ViewModels;
using Centello.Application;

namespace Centello.App.Pages;

public partial class InsightsPage : ContentPage
{
    private readonly InsightsViewModel _viewModel;
    private readonly IScreenReader _screenReader;
    private readonly PhoneLandscapeTrigger _landscapeTrigger;
    private readonly BrowseScrollAnchor _scrollAnchor;
    private bool _restoreRecapFocus;

    public InsightsPage(InsightsViewModel viewModel, IScreenReader screenReader)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _screenReader = screenReader;
        // The scroll anchor can be a generated row in the section that changes parents.
        Reports.BindingContext = viewModel;
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
        var restoreRecapFocus = _restoreRecapFocus;
        _restoreRecapFocus = false;
        await _viewModel.Load();
        if (restoreRecapFocus && _screenReader.IsOn && _viewModel.HasRecap)
            Dispatcher.Dispatch(() =>
            {
                if (Shell.Current.CurrentPage == this)
                    RecapButton.SetSemanticFocus();
            });
    }

    private async void OnPreviousTapped(object? sender, EventArgs e) => await _viewModel.ShowPrevious();

    private async void OnNextTapped(object? sender, EventArgs e) => await _viewModel.ShowNext();

    private async void OnPeriodToggled(object? sender, EventArgs e) => await _viewModel.TogglePeriod();

    private async void OnAddExpenseClicked(object? sender, EventArgs e)
    {
        if (_viewModel.Overview is not { } overview)
            return;

        var today = DateOnly.FromDateTime(DateTime.Today);
        var initialDate = overview.Period.Contains(today) ? today : overview.To;
        await Shell.Current.GoToAsync(ExpenseEditorPage.Route(initialDate: initialDate));
    }

    private async void OnRecapClicked(object? sender, EventArgs e)
    {
        if (_viewModel.Recap is not { } recap)
            return;

        _restoreRecapFocus = true;
        await Shell.Current.GoToAsync(RecapPage.Route(recap.Period));
    }

    private void OnDisclosureClicked(object? sender, EventArgs e) => _viewModel.ToggleCategories();

    private async void OnCategoryClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: InsightCategoryRow { CategoryId: { } categoryId } } &&
            _viewModel.Overview is { } overview)
            await Shell.Current.GoToAsync(CategoryDetailPage.Route(categoryId, overview.Period));
    }
}
