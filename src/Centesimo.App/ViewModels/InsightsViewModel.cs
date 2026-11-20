using Centesimo.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Centesimo.App.ViewModels;

/// <summary>One observation of "Da tenere d'occhio". The action names where its target leads.</summary>
public sealed record InsightRow(string Glyph, string Title, string Description, string Action, InsightTarget Target);

/// <summary>One row of "Per categoria". ShareOfTop is the spending against the category that spent most, which is
/// what the small bar draws.</summary>
public sealed record InsightCategoryRow(Guid CategoryId, string Name, string IconKey, string ColorKey, string Amount,
    string Share, double ShareOfTop, InsightChange Change)
{
    public string Description => $"{Name}, {Amount}, {Share}, {Change.Text}";
}

public sealed class InsightsViewModel(IServiceScopeFactory scopeFactory) : ObservableObject
{
    // Null follows the month that contains today, so a page left open moves on when the month changes.
    private Period? _selected;
    private InsightsOverview? _overview;
    private bool _isLoading;
    private string _errorMessage = "";

    public InsightsOverview? Overview
    {
        get => _overview;
        private set
        {
            if (SetProperty(ref _overview, value))
                // Everything the page shows is read from the overview.
                OnPropertyChanged("");
        }
    }
    public bool HasSpending => Overview is { SpentCents: > 0 };
    public bool IsEmpty => Overview is { SpentCents: 0 };
    /// <summary>The period of today can still fill up. A past one without expenses stays empty.</summary>
    public string EmptyTitle => Overview is null || !Overview.Navigation.HasNext
        ? "Ancora nessuna spesa"
        : $"Nessuna spesa {ItalianFormat.InPeriod(Overview.Period)}";
    public string Total => ItalianFormat.Amount(Overview?.SpentCents ?? 0);
    public BudgetSummary? Summary => Overview is null ? null : InsightTexts.Summary(Overview);
    public IReadOnlyList<InsightRow> Insights => Overview?.Insights.Select(ToRow).ToList() ?? [];
    public bool HasInsights => Overview is { Insights.Count: > 0 };
    public IReadOnlyList<InsightCategoryRow> Categories => Overview is null ? [] : Overview.Categories.Select(ToRow).ToList();
    public string ErrorMessage { get => _errorMessage; private set => SetProperty(ref _errorMessage, value); }

    public Task Load() => LoadPeriod(_selected);

    public Task ShowPrevious() => Navigate(Overview?.Navigation.Previous);

    public Task ShowNext() => Navigate(Overview?.Navigation.Next);

    public Task TogglePeriod() => Navigate(Overview?.Navigation.Toggle);

    private async Task Navigate(Period? target)
    {
        if (target is not null)
            await LoadPeriod(target);
    }

    private async Task LoadPeriod(Period? period)
    {
        if (_isLoading)
            return;

        _isLoading = true;
        ErrorMessage = "";
        Result<InsightsOverview> result;
        using (var scope = scopeFactory.CreateScope())
            result = await scope.ServiceProvider.GetRequiredService<IInsightsService>().Get(period);

        _isLoading = false;
        if (result.IsFailure)
        {
            ErrorMessage = result.Error.Message;
            return;
        }

        // The current month has no next period. Reaching it again with the arrows must not pin the page to it.
        var followsToday = result.Value.Period.Kind == PeriodKind.Month && !result.Value.Navigation.HasNext;
        _selected = followsToday ? null : result.Value.Period;
        Overview = result.Value;
    }

    private static InsightRow ToRow(LocalInsight insight) => new(GlyphOf(insight.Kind), insight.Title, insight.Description,
        insight.Target is InsightTarget.ExpenseTarget ? "Apri la spesa" : "Vedi le spese", insight.Target);

    // No discard arm: CS8524 is suppressed in the csproj, so a kind without a glyph fails the build.
    private static string GlyphOf(InsightKind kind) => kind switch
    {
        InsightKind.NewHabit => MaterialSymbols.EventRepeat,
        InsightKind.UnusualExpense => MaterialSymbols.Flag
    };

    private InsightCategoryRow ToRow(InsightCategory category) => new(category.CategoryId, category.Name,
        CategoryIcons.Resolve(category.Icon).Key, CategoryPalette.Resolve(category.Color).Key,
        ItalianFormat.Money(category.SpentCents), InsightTexts.Share(category.Percentage),
        // The categories come largest first, so the first one is the measure of the others.
        (double)category.SpentCents / Overview!.Categories[0].SpentCents, InsightTexts.Change(category.ChangePercentage));
}
