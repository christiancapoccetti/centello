using Centello.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Centello.App.ViewModels;

public sealed record InsightCategoryRow(InsightCategory Category, string ComparedPeriod, string CurrentPeriod,
    double DeltaFraction, double DeltaZero)
{
    public Guid? CategoryId => Category.CategoryId;
    public bool CanOpen => CategoryId is not null;
    public bool IsResidual => !CanOpen;
    public string Name => Category.Name;
    public string IconKey => Category.Icon;
    public string ColorKey => Category.Color;
    public string Amount => InsightTexts.Money(Category.SpentCents);
    public string ComparedAmount => Category.ComparedSpentCents is { } cents ? InsightTexts.Money(cents) : "—";
    public string Share => Category.Share is { } share ? InsightTexts.Share(share) : "—";
    public double ShareFraction => Category.Share.GetValueOrDefault();
    public double? ComparedShare => Category.ComparedShare;
    public string Change => InsightTexts.Variation(Category.SpentCents, Category.ComparedSpentCents);
    public string Delta => Category.DeltaCents is { } cents ? InsightTexts.Delta(cents) : "";
    public string ShareDescription => ComparedShare is { } prior
        ? $"{Name}, {Amount}, {Share} a confronto con {InsightTexts.Share(prior)} nel periodo precedente"
        : $"{Name}, {Amount}, quota {Share}";
    public string TableDescription => Category.ComparedSpentCents is null
        ? $"{Name}, {CurrentPeriod} {Amount}. Confronto non disponibile."
        : $"{Name}, {ComparedPeriod} {ComparedAmount}, {CurrentPeriod} {Amount}, variazione {Change}";
}

public sealed class InsightsViewModel(IServiceScopeFactory scopeFactory) : ObservableObject
{
    // Null follows the month that contains today, so a page left open moves on when the month changes.
    private Period? _selected;
    private InsightsOverview? _overview;
    private bool _isLoading;
    private string _errorMessage = "";
    private int _chartIndex;
    private bool _categoriesExpanded;

    public InsightsOverview? Overview => _overview;
    public bool HasReports => Overview is not null && !IsEmpty;
    public bool IsEmpty => Overview is { SpentCents: 0, ComparedSpentCents: null };
    public string EmptyTitle => Overview is null || !Overview.Navigation.HasNext
        ? "Ancora nessuna spesa"
        : $"Nessuna spesa {ItalianFormat.InPeriod(Overview.Period)}";
    public string Total => $"{InsightTexts.Money(Overview?.SpentCents ?? 0)} spesi";
    public string Range => Overview is { } overview ? InsightTexts.DateRange(overview.From, overview.To) + " · " : "";
    public InsightRecap? Recap => Overview?.Recap;
    public bool HasRecap => Recap is not null;
    public string CurrentPeriod => Overview is { } overview ? PeriodName(overview.Period) : "";
    public string ComparedPeriod => Overview is { } overview ? PeriodName(overview.Period.Previous()) : "";
    public bool HasPriorShares => Overview?.ComparedSpentCents is > 0;
    public string CurrentShareLabel => Overview is { } overview ? $"Quota {ItalianFormat.InPeriod(overview.Period)}" : "";
    public string ComparedShareLabel => Overview is { } overview ? $"Quota {ItalianFormat.InPeriod(overview.Period.Previous())}" : "";
    public bool HasLeadingCategories => LeadingCategories.Count > 0;
    public IReadOnlyList<InsightCategoryRow> LeadingCategories { get; private set; } = [];
    public IReadOnlyList<InsightCategoryRow> Categories { get; private set; } = [];
    public IReadOnlyList<InsightCategoryRow> Deltas { get; private set; } = [];
    public bool HasUnchangedCategories => HasPriorShares && Deltas.Count == 0;
    public string DeltaDetail { get; private set; } = "";
    public bool HasDeltaDetail => DeltaDetail.HasValue();
    public string ComparisonTitle => Overview?.Period.Kind == PeriodKind.Year ? "Confronta i due anni" : "Confronta i due mesi";
    public string TrendContext => Overview is { ComparedSpentCents: { } prior } overview
        ? $"Hai speso {InsightTexts.Money(overview.SpentCents)} nel periodo selezionato. " +
            $"Nel periodo {InsightTexts.DateRange(overview.ComparedFrom, overview.ComparedTo)} {overview.ComparedTo.Year} avevi speso {InsightTexts.Money(prior)}."
        : Overview?.Summary.Lead ?? "";
    public string ErrorMessage { get => _errorMessage; private set => SetProperty(ref _errorMessage, value); }

    public int ChartIndex
    {
        get => _chartIndex;
        set
        {
            if (!SetProperty(ref _chartIndex, value))
                return;

            OnPropertyChanged(nameof(ShowTrend));
            OnPropertyChanged(nameof(ShowDeltas));
        }
    }

    public bool ShowTrend => ChartIndex == 0;
    public bool ShowDeltas => ChartIndex == 1;
    public bool CategoriesExpanded => _categoriesExpanded;
    public string DisclosureGlyph => CategoriesExpanded ? MaterialSymbols.ExpandLess : MaterialSymbols.ExpandMore;
    public string DisclosureTitle => CategoriesExpanded ? "Nascondi il confronto per categoria" : "Vedi tutte le categorie";

    public void ToggleCategories()
    {
        _categoriesExpanded = !_categoriesExpanded;
        OnPropertyChanged(nameof(CategoriesExpanded));
        OnPropertyChanged(nameof(DisclosureGlyph));
        OnPropertyChanged(nameof(DisclosureTitle));
    }

    public Task Load() => LoadPeriod(_selected);

    /// <summary>The only way to move Insight from outside: opens it on a period, as the arrows would.</summary>
    public Task Show(Period period)
    {
        _selected = period;
        return LoadPeriod(period);
    }

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
        ApplyOverview(result.Value);
    }

    private void ApplyOverview(InsightsOverview overview)
    {
        if (_overview?.Period != overview.Period)
        {
            _chartIndex = 0;
            _categoriesExpanded = false;
        }

        _overview = overview;
        var bounds = overview.Categories
            .Select(row => row.DeltaCents.GetValueOrDefault())
            .Append(0)
            .ToArray();
        var lower = (double)-bounds.Min();
        var upper = (double)bounds.Max();
        var zero = lower + upper > 0 ? Math.Clamp(lower / (lower + upper), 0.3, 0.7) : 0.5;
        var scale = Math.Max(1, Math.Max(lower / zero, upper / (1 - zero)));
        var comparedPeriod = ComparedPeriod;
        var currentPeriod = CurrentPeriod;
        InsightCategoryRow ToRow(InsightCategory row) =>
            new(row, comparedPeriod, currentPeriod, row.DeltaCents.GetValueOrDefault() / scale, zero);

        Categories = overview.Categories.Select(ToRow).ToArray();
        LeadingCategories = overview.Composition.Leading.Select(ToRow).ToArray();
        Deltas = Categories.Where(row => row.Category.DeltaCents is not null and not 0).ToArray();
        DeltaDetail = DescribeDeltas(Deltas);
        OnPropertyChanged("");
    }

    private static string PeriodName(Period period) => period.Kind == PeriodKind.Month
        ? ItalianFormat.MonthTitle(period.Start.Month)
        : period.Start.Year.ToString();

    private static string DescribeDeltas(IReadOnlyList<InsightCategoryRow> rows)
    {
        var largest = rows
            .OrderByDescending(row => Math.Abs(row.Category.DeltaCents!.Value))
            .Take(2)
            .ToArray();
        if (largest.Length < 2)
            return "";

        var first = largest[0];
        var delta = first.Category.DeltaCents!.Value;
        if (!first.CanOpen || Math.Abs(delta) == Math.Abs(largest[1].Category.DeltaCents!.Value))
            return "";

        var rest = rows.Where(row => row != first).Sum(row => row.Category.DeltaCents!.Value);
        return $"In {first.Name} hai speso {ChangePhrase(delta)}. Nelle altre categorie, {ChangePhrase(rest)}.";
    }

    private static string ChangePhrase(long cents) => cents switch
    {
        > 0 => $"{InsightTexts.Money(cents)} in più",
        < 0 => $"{InsightTexts.Money(-cents)} in meno",
        _ => "quanto nel periodo precedente"
    };
}
