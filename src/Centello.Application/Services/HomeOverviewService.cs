using Centello.Domain;

namespace Centello.Application;

/// <summary>Everything the home shows for one period, ready to bind. Strings are Italian.</summary>
public sealed record HomeOverview(
    Period Period,
    HomeNavigation Navigation,
    string Total,                    // "1.284,60" (the page draws "€")
    BudgetSummary Budget,
    PeriodComparison? Comparison,
    CategoryLine Categories,
    SpendChart Chart,
    IReadOnlyList<LatestExpense> Latest,
    bool HasCategories)
{
    /// <summary>True until the first category exists: there is nothing an expense could go to.</summary>
    public bool IsFirstRun => !HasCategories;
    public bool HasComparison => Comparison is not null;
    public bool HasLatest => Latest.Count > 0;
    public bool HasNoLatest => Latest.Count == 0;
}

/// <summary>Navigation targets computed with the clock, so the view model never reads the date.</summary>
public sealed record HomeNavigation(Period Previous, Period? Next, Period Toggle)
{
    public bool HasNext => Next is not null;

    internal static HomeNavigation For(Period period, DateOnly today) =>
        new(period.Previous(), period.End >= today ? null : period.Next(), ToggleOf(period, today));

    private static Period ToggleOf(Period period, DateOnly today)
    {
        if (period.Kind == PeriodKind.Month)
            return Period.Year(period.Start.Year);

        return period.Contains(today) ? Period.MonthOf(today) : Period.Month(period.Start.Year, 12);
    }
}

/// <summary>Three runs so the amount can be bold: Lead + Emphasis + Tail.</summary>
public sealed record BudgetSummary(string Lead, string Emphasis, string Tail);

public enum ComparisonTrend { Lower, Higher, Same }

/// <summary>Text "12% in meno di ottobre" | "In linea con ottobre". Short "12% su ottobre" | "20% sul 2025", and the same as
/// Text when the trend is Same.</summary>
public sealed record PeriodComparison(string Text, string Short, ComparisonTrend Trend, string Description);

/// <summary>Key holds every category of the line, over-budget ones first (the furthest over first), then the rest by
/// spending. The screen shows as many whole items as fit on one line.</summary>
public sealed record CategoryLine(IReadOnlyList<CategorySegment> Segments, IReadOnlyList<CategoryKeyItem> Key, string Description)
{
    public bool HasSegments => Segments.Count > 0;
}

/// <summary>One flat segment; its width is proportional to Cents. Archived or missing categories fold into one grey segment.</summary>
public sealed record CategorySegment(string ColorKey, long Cents);

/// <summary>Text is the category name. An over-budget item says so for a screen reader and draws a glyph, so the
/// meaning never rests on its color alone.</summary>
public sealed record CategoryKeyItem(Guid CategoryId, string Text, string ColorKey, bool IsOverBudget)
{
    public string Description => IsOverBudget ? $"{Text}, oltre il budget" : Text;
}

public enum SpendBarKind { Past, Current, Future }

/// <summary>Fraction is 0..1 of the chart height. Future bars carry 0: the chart draws them as a stub.</summary>
public sealed record SpendBar(double Fraction, SpendBarKind Kind);

public sealed record SpendChart(
    IReadOnlyList<SpendBar> Bars,      // days of the month, or 12 months
    double? PaceFraction,              // null when no budget is set
    string StartLabel,                 // "1 nov" | "gen"
    string EndLabel,                   // "30 nov" | "dic"
    string HighlightLabel,             // "oggi 27,00 €" | "nov 1.284,60 €" | "" outside the current period
    double HighlightPosition,          // centre of the Current bar, 0..1
    string Description,                // screen-reader text alternative
    string PaceLabel)                  // "media per restare nel budget" | "" without a budget
{
    public bool HasPace => PaceFraction.HasValue;
    public bool HasHighlight => HighlightLabel.HasValue();
}

/// <summary>The title is the category. Detail is "Ieri · Netflix · automatica": the day, the note (or else the tag), then
/// "automatica".</summary>
public sealed record LatestExpense(Guid ExpenseId, string Title, string Detail, string Amount, string IconKey, string ColorKey)
{
    public string Description => RowLabels.Description(Title, Detail, Amount);
}

/// <summary>The month of today as the home screen widgets read it: the figures of the home, worded for a small
/// surface. Strings are Italian.</summary>
public sealed record MonthGlance(
    string Month,                          // "Novembre"
    string Year,                           // "2026"
    string Total,                          // "486,20" (the widget draws "€" smaller)
    BudgetSummary Budget,                  // "Ne restano " + "313,80 €" + " su 800"
    PeriodComparison? Comparison,          // Il mese shows Short, Il ritmo shows Text
    IReadOnlyList<CategorySegment> Segments,
    string OverBudget,                     // "Ristoranti oltre il budget", the furthest over; "" when none is
    SpendChart Chart,                      // the home's bars and pace, with the widget's labels
    string Description)                    // the whole summary for a screen reader
{
    public bool HasOverBudget => OverBudget.HasValue();
}

public sealed class HomeOverviewService(ICategoryRepository categories, ITagRepository tags,
    IExpenseRepository expenses, Func<DateOnly>? today = null)
{
    private const int LatestCount = 5;
    private const string PaceLabelText = "media per restare nel budget";
    private readonly Func<DateOnly> _today = today ?? (() => DateOnly.FromDateTime(DateTime.Today));

    /// <summary>Builds the overview for <paramref name="period"/>, or for the month containing today when null.</summary>
    public async Task<Result<HomeOverview>> Get(Period? period = null, CancellationToken cancellationToken = default)
    {
        var day = _today();
        var reading = await Read(period ?? Period.MonthOf(day), day, cancellationToken);
        if (reading.IsFailure)
            return Result<HomeOverview>.Failure(reading.Error);

        var tagResult = await tags.GetAll(cancellationToken);
        if (tagResult.IsFailure)
            return Result<HomeOverview>.Failure(tagResult.Error);

        return Result<HomeOverview>.Success(Build(reading.Value, tagResult.Value));
    }

    /// <summary>The month of today, worded for a home screen widget. Same reads and figures as Get(null), without the
    /// tags: a widget has no latest rows.</summary>
    public async Task<Result<MonthGlance>> Glance(CancellationToken cancellationToken = default)
    {
        var day = _today();
        var reading = await Read(Period.MonthOf(day), day, cancellationToken);
        if (reading.IsFailure)
            return Result<MonthGlance>.Failure(reading.Error);

        return Result<MonthGlance>.Success(Glimpse(reading.Value));
    }

    // One range covers the comparison window and the period. The reads are awaited one after the other: the
    // repositories share a scoped DbContext, which never allows two operations at once, and RepositoryBase would turn
    // that violation into a silent "Unexpected".
    private async Task<Result<Reading>> Read(Period period, DateOnly day, CancellationToken cancellationToken)
    {
        var window = ComparisonWindow.For(period, day);
        var categoryResult = await categories.GetAll(cancellationToken);
        if (categoryResult.IsFailure)
            return Result<Reading>.Failure(categoryResult.Error);

        var expenseResult = await expenses.GetBetween(window.PreviousFrom, period.End, cancellationToken);
        if (expenseResult.IsFailure)
            return Result<Reading>.Failure(expenseResult.Error);

        return Result<Reading>.Success(new Reading(period, day, window, categoryResult.Value, expenseResult.Value));
    }

    private sealed record Reading(Period Period, DateOnly Today, ComparisonWindow Window,
        IReadOnlyList<Category> Categories, IReadOnlyList<Expense> Expenses)
    {
        public IReadOnlyList<Expense> InPeriod { get; } =
            Expenses.Where(expense => Period.Contains(expense.OccurredOn)).ToList();

        public long Spent => InPeriod.Sum(expense => expense.Amount.Cents);
        public long? Budget { get; } = PeriodBudget.OfActive(Period, Categories);
    }

    private static HomeOverview Build(Reading reading, IReadOnlyList<Tag> tags) =>
        new(reading.Period, HomeNavigation.For(reading.Period, reading.Today), ItalianFormat.Amount(reading.Spent),
            reading.Categories.Count == 0
                ? NothingYet(reading.Period)
                : Summarize(reading.Spent, reading.Budget, " di budget"),
            Compare(reading.Period, reading.Window, reading.Expenses), Line(reading),
            Chart(reading, HomeLabels(reading.Period, reading.Today)),
            Latest(reading.InPeriod, reading.Categories, tags, reading.Today), reading.Categories.Count > 0);

    private static MonthGlance Glimpse(Reading reading)
    {
        var line = Line(reading);
        var budget = reading.Categories.Count == 0 || reading.Spent == 0
            ? NothingYet(reading.Period)
            : Summarize(reading.Spent, reading.Budget, "");
        var comparison = Compare(reading.Period, reading.Window, reading.Expenses);
        var overBudget = line.Key.FirstOrDefault(item => item.IsOverBudget) is { } over
            ? $"{over.Text} oltre il budget"
            : "";
        var sentences = new[]
            {
                $"{ItalianFormat.PeriodTitle(reading.Period)}: {ItalianFormat.Money(reading.Spent)}",
                budget.Lead + budget.Emphasis + budget.Tail,
                comparison?.Text ?? "",
                overBudget
            }
            .Where(sentence => sentence.HasValue())
            .Select(sentence => $"{sentence}.");
        return new MonthGlance(ItalianFormat.PeriodName(reading.Period), reading.Period.Start.Year.ToString(),
            ItalianFormat.Amount(reading.Spent), budget, comparison, line.Segments, overBudget,
            Chart(reading, GlanceLabels(reading.Period)), string.Join(" ", sentences));
    }

    // The first run has no budget to speak of: the line under the total says there is nothing to count yet.
    private static BudgetSummary NothingYet(Period period) =>
        new($"Ancora nessuna spesa {ItalianFormat.InPeriod(period)}", "", "");

    private static long SpentBetween(IReadOnlyList<Expense> expenses, DateOnly from, DateOnly to) => expenses
        .Where(expense => expense.OccurredOn >= from && expense.OccurredOn <= to)
        .Sum(expense => expense.Amount.Cents);

    private static BudgetSummary Summarize(long spent, long? budget, string remainingSuffix)
    {
        if (budget is not { } total)
            return new BudgetSummary("Nessun budget impostato", "", "");

        if (spent > total)
            return new BudgetSummary("Hai superato il budget di ", ItalianFormat.Money(spent - total),
                $" su {ItalianFormat.Budget(total)}");

        return new BudgetSummary("Ne restano ", ItalianFormat.Money(total - spent),
            $" su {ItalianFormat.Budget(total)}{remainingSuffix}");
    }

    // The percentage is rounded first, so a change that rounds to zero reads as "in linea".
    private static PeriodComparison? Compare(Period period, ComparisonWindow window, IReadOnlyList<Expense> expenses)
    {
        var previous = SpentBetween(expenses, window.PreviousFrom, window.PreviousTo);
        if (previous == 0)
            return null;

        var current = SpentBetween(expenses, window.CurrentFrom, window.CurrentTo);
        var percent = (int)Math.Round(Math.Abs(current - previous) * 100m / previous, MidpointRounding.AwayFromZero);
        var trend = percent == 0
            ? ComparisonTrend.Same
            : current < previous ? ComparisonTrend.Lower : ComparisonTrend.Higher;
        var change = trend switch
        {
            ComparisonTrend.Lower => $"{ItalianFormat.Percent(percent)} in meno",
            ComparisonTrend.Higher => $"{ItalianFormat.Percent(percent)} in più",
            _ => "in linea"
        };

        var isMonth = period.Kind == PeriodKind.Month;
        var reference = isMonth
            ? ItalianFormat.MonthName(period.Previous().Start.Month)
            : period.Previous().Start.Year.ToString();
        var (preposition, sameAs, shortPreposition) = isMonth ? ("di", "con", "su") : ("del", "con il", "sul");
        var text = trend == ComparisonTrend.Same ? $"In linea {sameAs} {reference}" : $"{change} {preposition} {reference}";
        var basis = window.IsPartial
            ? $"agli stessi giorni {preposition} {reference}"
            : isMonth ? $"al mese di {reference}" : $"al {reference}";
        var shortText = trend == ComparisonTrend.Same ? text : $"{ItalianFormat.Percent(percent)} {shortPreposition} {reference}";
        return new PeriodComparison(text, shortText, trend, $"Rispetto {basis}: {change}.");
    }

    // Active categories with spending, largest first. Whatever is left of the total (archived or missing
    // categories) becomes one grey segment, so the line always adds up to the headline total. The key lists the same
    // categories with the over-budget ones first; OrderByDescending is stable, so ties keep the spending order.
    private static CategoryLine Line(Reading reading)
    {
        var (period, categories, periodExpenses, spent) = (reading.Period, reading.Categories, reading.InPeriod, reading.Spent);
        if (spent == 0)
            return new CategoryLine([], [], "");

        var spentByCategory = periodExpenses
            .GroupBy(expense => expense.CategoryId)
            .ToDictionary(group => group.Key, group => group.Sum(expense => expense.Amount.Cents));
        var ranked = categories
            .Where(category => !category.IsArchived && spentByCategory.ContainsKey(category.CategoryId))
            .Select(category => new RankedCategory(category, spentByCategory[category.CategoryId],
                PeriodBudget.Of(period, category.MonthlyBudget) ?? 0))
            .OrderByDescending(item => item.Spent)
            .ThenBy(item => item.Category.Name)
            .ToList();
        var folded = spent - ranked.Sum(item => item.Spent);
        var segments = ranked
            .Select(item => new CategorySegment(item.ColorKey, item.Spent))
            .ToList();
        if (folded > 0)
            segments.Add(new CategorySegment(CategoryPalette.Fallback.Key, folded));

        var key = ranked
            .OrderByDescending(item => item.Overshoot)
            .Select(item => item.ToKeyItem())
            .ToList();

        var description = ranked
            .Select(item => $"{item.Category.Name} {ItalianFormat.Money(item.Spent)}")
            .Concat(folded > 0 ? [$"Altre categorie {ItalianFormat.Money(folded)}"] : []);
        return new CategoryLine(segments, key,
            $"Spesa per categoria: {string.Join(", ", description)}.");
    }

    private sealed record RankedCategory(Category Category, long Spent, long Budget)
    {
        public string ColorKey => CategoryPalette.Resolve(Category.Color).Key;
        public long Overshoot => Budget > 0 ? Math.Max(0, Spent - Budget) : 0;
        public bool IsOverBudget => Overshoot > 0;

        public CategoryKeyItem ToKeyItem() =>
            new(Category.CategoryId, Category.Name, ColorKey, IsOverBudget);
    }

    // One bar per day (month) or per month (year). The scale also covers the pace line, which keeps it on the chart.
    private static SpendChart Chart(Reading reading, ChartLabels labels)
    {
        var buckets = SpendBuckets.Of(reading.Period, reading.Today, reading.InPeriod);
        double? pace = reading.Budget.HasValue ? (double)reading.Budget.Value / buckets.Count : null;
        var scale = buckets.Scale(pace ?? 0);

        var highlight = buckets.HasToday ? labels.Highlight(buckets.Cents[buckets.Current]) : "";
        return new SpendChart(buckets.Bars(scale), pace / scale, labels.Start, labels.End,
            highlight, buckets.HasToday ? (buckets.Current + 0.5) / buckets.Count : 0,
            DescribeChart(reading.Period, buckets.Cents, buckets.Current, pace), labels.Pace(pace));
    }

    private static ChartLabels HomeLabels(Period period, DateOnly today)
    {
        var isMonth = period.Kind == PeriodKind.Month;
        return new ChartLabels(
            pace => pace.HasValue ? PaceLabelText : "",
            isMonth ? ItalianFormat.ShortDate(period.Start) : ItalianFormat.ShortMonth(1),
            isMonth ? ItalianFormat.ShortDate(period.End) : ItalianFormat.ShortMonth(12),
            cents => isMonth
                ? $"oggi {ItalianFormat.Money(cents)}"
                : $"{ItalianFormat.ShortMonth(today.Month)} {ItalianFormat.Money(cents)}");
    }

    private static ChartLabels GlanceLabels(Period period) => new(
        pace => pace is { } perDay ? $"{ItalianFormat.WholeEuros((long)Math.Round(perDay, MidpointRounding.AwayFromZero))} € al giorno" : "",
        "1", period.End.Day.ToString(), _ => "oggi");

    /// <summary>The words of the chart that differ between the home and a widget.</summary>
    private sealed record ChartLabels(Func<double?, string> Pace, string Start, string End, Func<long, string> Highlight);

    private static string DescribeChart(Period period, long[] buckets, int current, double? pace)
    {
        var isMonth = period.Kind == PeriodKind.Month;
        var year = period.Start.Year;
        var parts = new List<string>
        {
            isMonth
                ? $"Spesa giornaliera di {ItalianFormat.MonthName(period.Start.Month)} {year}."
                : $"Spesa mensile del {year}."
        };

        var visible = buckets.Take(Math.Min(current + 1, buckets.Length)).ToList();
        var peak = visible.DefaultIfEmpty(0).Max();
        if (peak == 0)
            parts.Add("Nessuna spesa registrata.");
        else if (isMonth)
            parts.Add($"Giorno più alto: {ItalianFormat.LongDate(period.Start.AddDays(visible.IndexOf(peak)))}, {ItalianFormat.Money(peak)}.");
        else
            parts.Add($"Mese più alto: {ItalianFormat.MonthName(visible.IndexOf(peak) + 1)}, {ItalianFormat.Money(peak)}.");

        if (current >= 0 && current < buckets.Length)
            parts.Add($"{(isMonth ? "Oggi" : "Questo mese")}: {ItalianFormat.Money(buckets[current])}.");

        if (pace.HasValue)
            parts.Add($"Media per restare nel budget: {ItalianFormat.Money((long)Math.Round(pace.Value, MidpointRounding.AwayFromZero))} {(isMonth ? "al giorno" : "al mese")}.");

        return string.Join(" ", parts);
    }

    // Within a day the order follows ExpenseId, which sorts by creation time for expenses created since ids became
    // version 7. Older and imported expenses carry random or hashed ids, so their order within a day is arbitrary.
    private static IReadOnlyList<LatestExpense> Latest(IReadOnlyList<Expense> periodExpenses,
        IReadOnlyList<Category> categories, IReadOnlyList<Tag> tags, DateOnly today)
    {
        var categoryById = categories.ToDictionary(category => category.CategoryId);
        var tagById = tags.ToDictionary(tag => tag.TagId);
        return periodExpenses
            .OrderByDescending(expense => expense.OccurredOn)
            .ThenByDescending(expense => expense.ExpenseId)
            .Take(LatestCount)
            .Select(expense => ToLatest(expense, categoryById.GetValueOrDefault(expense.CategoryId),
                expense.TagId.HasValue ? tagById.GetValueOrDefault(expense.TagId.Value) : null, today))
            .ToList();
    }

    private static LatestExpense ToLatest(Expense expense, Category? category, Tag? tag, DateOnly today)
    {
        var (title, detail) = RowLabels.OfExpense(ItalianFormat.RelativeDay(expense.OccurredOn, today), expense, category, tag);
        return new LatestExpense(expense.ExpenseId, title, detail, ItalianFormat.Money(expense.Amount.Cents),
            CategoryIcons.Resolve(category?.Icon).Key, CategoryPalette.Resolve(category?.Color).Key);
    }
}
