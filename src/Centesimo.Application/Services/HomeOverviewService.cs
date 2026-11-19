using Centesimo.Domain;

namespace Centesimo.Application;

/// <summary>Everything the home shows for one period, ready to bind. Strings are Italian.</summary>
public sealed record HomeOverview(
    Period Period,
    HomeNavigation Navigation,
    string Title,                    // "Novembre" | "Anno"
    string Subtitle,                 // "2026"
    string Total,                    // "1.284,60" (the page draws "€")
    BudgetSummary Budget,
    PeriodComparison? Comparison,
    CategoryLine Categories,
    SpendChart Chart,
    IReadOnlyList<LatestExpense> Latest)
{
    public bool HasComparison => Comparison is not null;
    public bool HasLatest => Latest.Count > 0;
    public bool HasNoLatest => Latest.Count == 0;
}

/// <summary>Navigation targets computed with the clock, so the view model never reads the date.</summary>
public sealed record HomeNavigation(Period Previous, Period? Next, Period Toggle)
{
    public bool HasNext => Next is not null;
}

/// <summary>Three runs so the amount can be bold: Lead + Emphasis + Tail.</summary>
public sealed record BudgetSummary(string Lead, string Emphasis, string Tail);

public enum ComparisonTrend { Lower, Higher, Same }

public sealed record PeriodComparison(string Text, ComparisonTrend Trend, string Description);

public sealed record CategoryLine(IReadOnlyList<CategorySegment> Segments, IReadOnlyList<CategoryKeyItem> Key, string Description)
{
    public bool HasSegments => Segments.Count > 0;
}

/// <summary>One flat segment; its width is proportional to Cents. Archived or missing categories fold into one grey segment.</summary>
public sealed record CategorySegment(string ColorKey, long Cents);

public sealed record CategoryKeyItem(string Text, string ColorKey, bool IsOverBudget);

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
    string Description)                // screen-reader text alternative
{
    public bool HasPace => PaceFraction.HasValue;
    public bool HasHighlight => HighlightLabel.HasValue();
}

public sealed record LatestExpense(Guid ExpenseId, string Title, string Detail, string Amount, string IconKey, string ColorKey)
{
    public string Description => $"{Title}, {Detail}, {Amount}";
}

public sealed class HomeOverviewService(ICategoryRepository categories, IExpenseRepository expenses,
    Func<DateOnly>? today = null)
{
    private const int LatestCount = 5;
    private const int KeySize = 3;
    private readonly Func<DateOnly> _today = today ?? (() => DateOnly.FromDateTime(DateTime.Today));

    /// <summary>Builds the overview for <paramref name="period"/>, or for the month containing today when null.</summary>
    public async Task<Result<HomeOverview>> Get(Period? period = null, CancellationToken cancellationToken = default)
    {
        var day = _today();
        var selected = period ?? Period.MonthOf(day);
        var window = ComparisonWindow.For(selected, day);
        var categoryResult = await categories.GetAll(cancellationToken);
        if (categoryResult.IsFailure)
            return Result<HomeOverview>.Failure(categoryResult.Error);

        // One range covers the comparison window and the period. The two reads are awaited one after the other:
        // both repositories share a scoped DbContext, which never allows two operations at once, and
        // RepositoryBase would turn that violation into a silent "Unexpected".
        var expenseResult = await expenses.GetBetween(window.PreviousFrom, selected.End, cancellationToken);
        if (expenseResult.IsFailure)
            return Result<HomeOverview>.Failure(expenseResult.Error);

        return Result<HomeOverview>.Success(Build(selected, day, window, categoryResult.Value, expenseResult.Value));
    }

    private static HomeOverview Build(Period period, DateOnly today, ComparisonWindow window,
        IReadOnlyList<Category> categories, IReadOnlyList<Expense> expenses)
    {
        var periodExpenses = expenses.Where(expense => period.Contains(expense.OccurredOn)).ToList();
        var spent = periodExpenses.Sum(expense => expense.Amount.Cents);
        var year = period.Start.Year.ToString();
        var (title, subtitle) = period.Kind == PeriodKind.Month
            ? (ItalianFormat.MonthTitle(period.Start.Month), year)
            : ("Anno", year);
        var budget = BudgetOf(period, categories);
        return new HomeOverview(period, Navigate(period, today), title, subtitle, ItalianFormat.Amount(spent),
            Summarize(spent, budget), Compare(period, window, expenses),
            Line(period, categories, periodExpenses, spent), Chart(period, today, periodExpenses, budget),
            Latest(periodExpenses, categories, today));
    }

    private static long SpentBetween(IReadOnlyList<Expense> expenses, DateOnly from, DateOnly to) => expenses
        .Where(expense => expense.OccurredOn >= from && expense.OccurredOn <= to)
        .Sum(expense => expense.Amount.Cents);

    // A year budget is twelve monthly budgets. A total of zero is "no budget": nothing to measure against.
    private static long? BudgetOf(Period period, IReadOnlyList<Category> categories)
    {
        var monthly = categories
            .Where(category => !category.IsArchived)
            .Sum(category => category.MonthlyBudget?.Cents ?? 0);
        var total = period.Kind == PeriodKind.Year ? monthly * 12 : monthly;
        return total > 0 ? total : null;
    }

    private static HomeNavigation Navigate(Period period, DateOnly today) =>
        new(period.Previous(), period.End >= today ? null : period.Next(), Toggle(period, today));

    private static Period Toggle(Period period, DateOnly today)
    {
        if (period.Kind == PeriodKind.Month)
            return Period.Year(period.Start.Year);

        return period.Contains(today) ? Period.MonthOf(today) : Period.Month(period.Start.Year, 12);
    }

    private static BudgetSummary Summarize(long spent, long? budget)
    {
        if (budget is not { } total)
            return new BudgetSummary("Nessun budget impostato", "", "");

        if (spent > total)
            return new BudgetSummary("Hai superato il budget di ", ItalianFormat.Money(spent - total),
                $" su {ItalianFormat.Budget(total)}");

        return new BudgetSummary("Ne restano ", ItalianFormat.Money(total - spent),
            $" su {ItalianFormat.Budget(total)} di budget");
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
        var (preposition, sameAs) = isMonth ? ("di", "con") : ("del", "con il");
        var text = trend == ComparisonTrend.Same ? $"In linea {sameAs} {reference}" : $"{change} {preposition} {reference}";
        var basis = window.IsPartial
            ? $"agli stessi giorni {preposition} {reference}"
            : isMonth ? $"al mese di {reference}" : $"al {reference}";
        return new PeriodComparison(text, trend, $"Rispetto {basis}: {change}.");
    }

    // Active categories with spending, largest first. Whatever is left of the total (archived or missing
    // categories) becomes one grey segment, so the line always adds up to the headline total. The key names the
    // top KeySize categories and, when it is not among them, the category furthest over its budget.
    private static CategoryLine Line(Period period, IReadOnlyList<Category> categories,
        IReadOnlyList<Expense> periodExpenses, long spent)
    {
        if (spent == 0)
            return new CategoryLine([], [], "");

        var spentByCategory = periodExpenses
            .GroupBy(expense => expense.CategoryId)
            .ToDictionary(group => group.Key, group => group.Sum(expense => expense.Amount.Cents));
        var yearFactor = period.Kind == PeriodKind.Year ? 12 : 1;
        var ranked = categories
            .Where(category => !category.IsArchived && spentByCategory.ContainsKey(category.CategoryId))
            .Select(category => new RankedCategory(category, spentByCategory[category.CategoryId],
                (category.MonthlyBudget?.Cents ?? 0) * yearFactor))
            .OrderByDescending(item => item.Spent)
            .ThenBy(item => item.Category.Name)
            .ToList();
        var folded = spent - ranked.Sum(item => item.Spent);
        var segments = ranked
            .Select(item => new CategorySegment(item.ColorKey, item.Spent))
            .ToList();
        if (folded > 0)
            segments.Add(new CategorySegment(CategoryPalette.Fallback.Key, folded));

        var key = ranked.Take(KeySize).ToList();
        var furthestOver = ranked
            .Skip(KeySize)
            .Where(item => item.IsOverBudget)
            .OrderByDescending(item => item.Overshoot)
            .ThenBy(item => item.Category.Name)
            .FirstOrDefault();
        if (furthestOver is not null)
            key.Add(furthestOver);

        var description = ranked
            .Select(item => $"{item.Category.Name} {ItalianFormat.Money(item.Spent)}")
            .Concat(folded > 0 ? [$"Altre categorie {ItalianFormat.Money(folded)}"] : []);
        return new CategoryLine(segments, key.Select(item => item.ToKeyItem()).ToList(),
            $"Spesa per categoria: {string.Join(", ", description)}.");
    }

    private sealed record RankedCategory(Category Category, long Spent, long Budget)
    {
        public string ColorKey => CategoryPalette.Resolve(Category.Color).Key;
        public long Overshoot => Budget > 0 ? Math.Max(0, Spent - Budget) : 0;
        public bool IsOverBudget => Overshoot > 0;

        public CategoryKeyItem ToKeyItem() =>
            new(IsOverBudget ? $"{Category.Name} oltre" : Category.Name, ColorKey, IsOverBudget);
    }

    // One bar per day (month) or per month (year). Today's bar and those before it are drawn; later ones are stubs,
    // so they must not stretch the scale. The scale also covers the pace line, which keeps it on the chart.
    private static SpendChart Chart(Period period, DateOnly today, IReadOnlyList<Expense> periodExpenses, long? budget)
    {
        var isMonth = period.Kind == PeriodKind.Month;
        var count = isMonth ? period.End.Day : 12;
        var buckets = new long[count];
        foreach (var expense in periodExpenses)
            buckets[BucketOf(period, expense.OccurredOn)] += expense.Amount.Cents;

        // Index of today's bucket: before the period it is -1, after the period it is count.
        var current = today < period.Start ? -1 : today > period.End ? count : BucketOf(period, today);
        var kinds = Enumerable.Range(0, count)
            .Select(index => index < current ? SpendBarKind.Past : index == current ? SpendBarKind.Current : SpendBarKind.Future)
            .ToList();
        double? pace = budget.HasValue ? (double)budget.Value / count : null;
        var visibleMaximum = buckets.Where((_, index) => kinds[index] != SpendBarKind.Future).DefaultIfEmpty(0).Max();
        var scale = Math.Max(visibleMaximum, pace ?? 0);
        var bars = buckets
            .Select((cents, index) => new SpendBar(
                kinds[index] == SpendBarKind.Future || scale == 0 ? 0 : cents / scale, kinds[index]))
            .ToList();

        var hasToday = current >= 0 && current < count;
        var highlight = !hasToday ? "" : isMonth
            ? $"oggi {ItalianFormat.Money(buckets[current])}"
            : $"{ItalianFormat.ShortMonth(today.Month)} {ItalianFormat.Money(buckets[current])}";
        return new SpendChart(bars, pace / scale,
            isMonth ? ItalianFormat.ShortDate(period.Start) : ItalianFormat.ShortMonth(1),
            isMonth ? ItalianFormat.ShortDate(period.End) : ItalianFormat.ShortMonth(12),
            highlight, hasToday ? (current + 0.5) / count : 0, DescribeChart(period, buckets, current, pace));
    }

    private static int BucketOf(Period period, DateOnly day) => (period.Kind == PeriodKind.Month ? day.Day : day.Month) - 1;

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
        IReadOnlyList<Category> categories, DateOnly today)
    {
        var byId = categories.ToDictionary(category => category.CategoryId);
        return periodExpenses
            .OrderByDescending(expense => expense.OccurredOn)
            .ThenByDescending(expense => expense.ExpenseId)
            .Take(LatestCount)
            .Select(expense => ToLatest(expense, byId.GetValueOrDefault(expense.CategoryId), today))
            .ToList();
    }

    private static LatestExpense ToLatest(Expense expense, Category? category, DateOnly today)
    {
        var name = category?.Name ?? "Categoria non disponibile";
        var automatic = expense.RecurringPaymentId.HasValue ? " · automatica" : "";
        return new LatestExpense(expense.ExpenseId, expense.Note.HasValue() ? expense.Note : name,
            $"{ItalianFormat.RelativeDay(expense.OccurredOn, today)} · {name}{automatic}",
            ItalianFormat.Money(expense.Amount.Cents), CategoryIcons.Resolve(category?.Icon).Key,
            CategoryPalette.Resolve(category?.Color).Key);
    }

    /// <summary>The current period compares to date with the same position in the previous one, clamped to its
    /// last valid day (Mar 31 -> Feb 28; Feb 29 -> Feb 28 of the previous year). A past period compares whole with whole.</summary>
    private sealed record ComparisonWindow(DateOnly CurrentFrom, DateOnly CurrentTo, DateOnly PreviousFrom,
        DateOnly PreviousTo, bool IsPartial)
    {
        public static ComparisonWindow For(Period period, DateOnly today)
        {
            var previous = period.Previous();
            if (!period.Contains(today))
                return new ComparisonWindow(period.Start, period.End, previous.Start, previous.End, false);

            var monthStart = previous.Start.AddMonths(period.Kind == PeriodKind.Year ? today.Month - 1 : 0);
            var lastDay = Math.Min(today.Day, DateTime.DaysInMonth(monthStart.Year, monthStart.Month));
            return new ComparisonWindow(period.Start, today, previous.Start, monthStart.AddDays(lastDay - 1), true);
        }
    }
}
