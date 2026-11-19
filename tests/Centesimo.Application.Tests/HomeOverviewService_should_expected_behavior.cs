using Centesimo.Domain;

namespace Centesimo.Application.Tests;

public sealed class HomeOverviewService_should_expected_behavior
{
    private static readonly DateOnly Today = new(2026, 11, 19);
    private static readonly Error Failed = new("Test.Failed", "Operazione non riuscita.");

    [Fact]
    public async Task Should_resolve_the_current_month_when_no_period_is_given()
    {
        var overview = await Overview();

        Assert.Equal(Period.Month(2026, 11), overview.Period);
        Assert.Equal("Novembre", overview.Title);
        Assert.Equal("2026", overview.Subtitle);
        Assert.Null(overview.Navigation.Next);
    }

    [Fact]
    public async Task Should_follow_the_clock_when_the_month_changes()
    {
        var overview = await Overview(today: new DateOnly(2026, 12, 1));

        Assert.Equal("Dicembre", overview.Title);
    }

    [Fact]
    public async Task Should_include_archived_and_missing_categories_in_the_total()
    {
        var active = NewCategory("Spesa");
        var archived = NewCategory("Vecchia", archived: true);
        var expenses = new[]
        {
            Spend(active, 10_000, Today),
            Spend(archived, 2_500, Today),
            new Expense(Guid.NewGuid(), Guid.NewGuid(), new Money(1_234), Today)
        };

        var overview = await Overview([active, archived], expenses);

        Assert.Equal("137,34", overview.Total);
    }

    [Fact]
    public async Task Should_only_count_expenses_inside_the_period()
    {
        var category = NewCategory("Spesa");
        var expenses = new[]
        {
            Spend(category, 1_000, new DateOnly(2026, 10, 31)),
            Spend(category, 2_000, new DateOnly(2026, 11, 1)),
            Spend(category, 3_000, new DateOnly(2026, 11, 30)),
            Spend(category, 4_000, new DateOnly(2026, 12, 1))
        };

        var overview = await Overview([category], expenses);

        Assert.Equal("50,00", overview.Total);
    }

    [Fact]
    public async Task Should_say_what_is_left_of_the_budget()
    {
        var category = NewCategory("Spesa", budgetCents: 160_000);

        var overview = await Overview([category], [Spend(category, 128_460, Today)]);

        Assert.Equal("1.284,60", overview.Total);
        Assert.Equal(new BudgetSummary("Ne restano ", "315,40 €", " su 1.600 di budget"), overview.Budget);
    }

    [Fact]
    public async Task Should_say_when_no_budget_is_set()
    {
        var withoutBudget = NewCategory("Spesa");
        var zeroBudget = NewCategory("Zero", budgetCents: 0);
        var archivedBudget = NewCategory("Vecchia", budgetCents: 50_000, archived: true);

        var overview = await Overview([withoutBudget, zeroBudget, archivedBudget]);

        Assert.Equal(new BudgetSummary("Nessun budget impostato", "", ""), overview.Budget);
    }

    [Fact]
    public async Task Should_say_when_the_budget_is_exceeded()
    {
        var category = NewCategory("Spesa", budgetCents: 160_000);

        var overview = await Overview([category], [Spend(category, 164_010, Today)]);

        Assert.Equal(new BudgetSummary("Hai superato il budget di ", "40,10 €", " su 1.600"), overview.Budget);
    }

    [Fact]
    public async Task Should_say_nothing_is_left_when_the_budget_is_spent_exactly()
    {
        var category = NewCategory("Spesa", budgetCents: 50_000);

        var overview = await Overview([category], [Spend(category, 50_000, Today)]);

        Assert.Equal(new BudgetSummary("Ne restano ", "0,00 €", " su 500 di budget"), overview.Budget);
    }

    [Fact]
    public async Task Should_show_the_budget_cents_when_it_is_not_a_whole_amount()
    {
        var category = NewCategory("Spesa", budgetCents: 160_050);

        var overview = await Overview([category]);

        Assert.Equal(new BudgetSummary("Ne restano ", "1.600,50 €", " su 1.600,50 di budget"), overview.Budget);
    }

    [Fact]
    public async Task Should_scale_the_budget_to_twelve_months_for_a_year()
    {
        var category = NewCategory("Spesa", budgetCents: 10_000);

        var overview = await Overview([category], [Spend(category, 20_000, Today)], Period.Year(2026));

        Assert.Equal("Anno", overview.Title);
        Assert.Equal("2026", overview.Subtitle);
        Assert.Equal(new BudgetSummary("Ne restano ", "1.000,00 €", " su 1.200 di budget"), overview.Budget);
    }

    [Fact]
    public async Task Should_navigate_back_and_block_the_future()
    {
        var current = await Overview();
        var past = await Overview(period: Period.Month(2026, 9));

        Assert.Equal(Period.Month(2026, 10), current.Navigation.Previous);
        Assert.False(current.Navigation.HasNext);
        Assert.Equal(Period.Month(2026, 8), past.Navigation.Previous);
        Assert.Equal(Period.Month(2026, 10), past.Navigation.Next);
        Assert.True(past.Navigation.HasNext);
    }

    [Fact]
    public async Task Should_navigate_years_and_block_the_future()
    {
        var current = await Overview(period: Period.Year(2026));
        var past = await Overview(period: Period.Year(2024));

        Assert.Equal(Period.Year(2025), current.Navigation.Previous);
        Assert.Null(current.Navigation.Next);
        Assert.Equal(Period.Year(2025), past.Navigation.Next);
    }

    [Fact]
    public async Task Should_toggle_between_month_and_year()
    {
        var currentMonth = await Overview();
        var pastMonth = await Overview(period: Period.Month(2025, 3));
        var currentYear = await Overview(period: Period.Year(2026));
        var pastYear = await Overview(period: Period.Year(2025));

        Assert.Equal(Period.Year(2026), currentMonth.Navigation.Toggle);
        Assert.Equal(Period.Year(2025), pastMonth.Navigation.Toggle);
        Assert.Equal(Period.Month(2026, 11), currentYear.Navigation.Toggle);
        Assert.Equal(Period.Month(2025, 12), pastYear.Navigation.Toggle);
    }

    [Fact]
    public async Task Should_compare_the_current_month_with_the_same_elapsed_days_of_the_previous_month()
    {
        var category = NewCategory("Spesa");
        var expenses = new[]
        {
            Spend(category, 10_000, new DateOnly(2026, 11, 5)),
            Spend(category, 2_000, new DateOnly(2026, 11, 19)),
            Spend(category, 8_000, new DateOnly(2026, 10, 10)),
            Spend(category, 1_000, new DateOnly(2026, 10, 19)),
            Spend(category, 50_000, new DateOnly(2026, 10, 20))
        };

        var overview = await Overview([category], expenses);

        Assert.True(overview.HasComparison);
        Assert.Equal(
            new PeriodComparison("33% in più di ottobre", ComparisonTrend.Higher,
                "Rispetto agli stessi giorni di ottobre: 33% in più."),
            overview.Comparison);
    }

    [Fact]
    public async Task Should_compare_a_lower_current_month_with_the_previous_one()
    {
        var category = NewCategory("Spesa");
        var expenses = new[]
        {
            Spend(category, 9_400, new DateOnly(2026, 11, 5)),
            Spend(category, 10_000, new DateOnly(2026, 10, 5))
        };

        var overview = await Overview([category], expenses);

        Assert.Equal(
            new PeriodComparison("6% in meno di ottobre", ComparisonTrend.Lower,
                "Rispetto agli stessi giorni di ottobre: 6% in meno."),
            overview.Comparison);
    }

    [Fact]
    public async Task Should_clamp_the_comparison_to_the_previous_month_length()
    {
        var category = NewCategory("Spesa");
        var expenses = new[]
        {
            Spend(category, 20_000, new DateOnly(2026, 3, 31)),
            Spend(category, 5_000, new DateOnly(2026, 3, 2)),
            Spend(category, 10_000, new DateOnly(2026, 2, 28))
        };

        var overview = await Overview([category], expenses, today: new DateOnly(2026, 3, 31));

        Assert.Equal("150% in più di febbraio", overview.Comparison?.Text);
    }

    [Fact]
    public async Task Should_compare_a_past_month_with_the_whole_previous_month()
    {
        var category = NewCategory("Spesa");
        var expenses = new[]
        {
            Spend(category, 15_000, new DateOnly(2026, 8, 31)),
            Spend(category, 10_000, new DateOnly(2026, 7, 1)),
            Spend(category, 10_000, new DateOnly(2026, 7, 31)),
            Spend(category, 90_000, new DateOnly(2026, 9, 1))
        };

        var overview = await Overview([category], expenses, Period.Month(2026, 8));

        Assert.Equal(
            new PeriodComparison("25% in meno di luglio", ComparisonTrend.Lower,
                "Rispetto al mese di luglio: 25% in meno."),
            overview.Comparison);
    }

    [Fact]
    public async Task Should_not_compare_when_the_previous_period_has_no_spending()
    {
        var category = NewCategory("Spesa");
        var onlyAfterTheElapsedDays = new[]
        {
            Spend(category, 20_000, new DateOnly(2026, 11, 5)),
            Spend(category, 10_000, new DateOnly(2026, 10, 25))
        };

        var current = await Overview([category], onlyAfterTheElapsedDays);
        var past = await Overview([category], onlyAfterTheElapsedDays, Period.Month(2026, 5));

        Assert.Null(current.Comparison);
        Assert.False(current.HasComparison);
        Assert.Null(past.Comparison);
    }

    [Theory]
    [InlineData(10_040, "In linea con ottobre", ComparisonTrend.Same)]
    [InlineData(9_960, "In linea con ottobre", ComparisonTrend.Same)]
    [InlineData(10_050, "1% in più di ottobre", ComparisonTrend.Higher)]
    [InlineData(9_950, "1% in meno di ottobre", ComparisonTrend.Lower)]
    public async Task Should_round_the_change_to_a_whole_percent_and_call_a_zero_change_in_line(
        long current, string text, ComparisonTrend trend)
    {
        var category = NewCategory("Spesa");
        var expenses = new[]
        {
            Spend(category, current, new DateOnly(2026, 11, 5)),
            Spend(category, 10_000, new DateOnly(2026, 10, 10))
        };

        var overview = await Overview([category], expenses);

        Assert.Equal(text, overview.Comparison?.Text);
        Assert.Equal(trend, overview.Comparison?.Trend);
    }

    [Fact]
    public async Task Should_compare_the_current_year_with_the_same_span_of_the_previous_year()
    {
        var category = NewCategory("Spesa");
        var expenses = new[]
        {
            Spend(category, 30_000, new DateOnly(2026, 6, 1)),
            Spend(category, 20_000, new DateOnly(2025, 1, 10)),
            Spend(category, 5_000, new DateOnly(2025, 11, 19)),
            Spend(category, 99_000, new DateOnly(2025, 11, 20))
        };

        var overview = await Overview([category], expenses, Period.Year(2026));

        Assert.Equal(
            new PeriodComparison("20% in più del 2025", ComparisonTrend.Higher,
                "Rispetto agli stessi giorni del 2025: 20% in più."),
            overview.Comparison);
    }

    [Fact]
    public async Task Should_clamp_the_year_comparison_on_a_leap_day()
    {
        var category = NewCategory("Spesa");
        var expenses = new[]
        {
            Spend(category, 10_000, new DateOnly(2028, 2, 29)),
            Spend(category, 10_000, new DateOnly(2027, 2, 28)),
            Spend(category, 40_000, new DateOnly(2027, 3, 1))
        };

        var overview = await Overview([category], expenses, Period.Year(2028), new DateOnly(2028, 2, 29));

        Assert.Equal(
            new PeriodComparison("In linea con il 2027", ComparisonTrend.Same,
                "Rispetto agli stessi giorni del 2027: in linea."),
            overview.Comparison);
    }

    [Fact]
    public async Task Should_compare_a_past_year_with_the_whole_previous_year()
    {
        var category = NewCategory("Spesa");
        var expenses = new[]
        {
            Spend(category, 12_000, new DateOnly(2025, 12, 31)),
            Spend(category, 24_000, new DateOnly(2024, 12, 31))
        };

        var overview = await Overview([category], expenses, Period.Year(2025));

        Assert.Equal(
            new PeriodComparison("50% in meno del 2024", ComparisonTrend.Lower, "Rispetto al 2024: 50% in meno."),
            overview.Comparison);
    }

    [Fact]
    public async Task Should_draw_categories_by_spending_and_fold_archived_spending_into_one_grey_segment()
    {
        var food = NewCategory("Cibo", color: "mint");
        var home = NewCategory("Casa", color: "coral");
        var unused = NewCategory("Mai usata", color: "sky");
        var archived = NewCategory("Vecchia", archived: true, color: "lilac");
        var expenses = new[]
        {
            Spend(home, 10_000, Today),
            Spend(food, 30_000, Today),
            Spend(archived, 1_500, Today),
            new Expense(Guid.NewGuid(), Guid.NewGuid(), new Money(500), Today)
        };

        var line = (await Overview([food, home, unused, archived], expenses)).Categories;

        Assert.True(line.HasSegments);
        Assert.Equal(
            [new CategorySegment("mint", 30_000), new CategorySegment("coral", 10_000), new CategorySegment("grey", 2_000)],
            line.Segments);
    }

    [Fact]
    public async Task Should_order_equal_spending_by_category_name_and_resolve_unknown_colors_to_grey()
    {
        var beta = NewCategory("Beta", color: "mint");
        var alpha = NewCategory("Alfa", color: "#176B5B");
        var expenses = new[] { Spend(beta, 1_000, Today), Spend(alpha, 1_000, Today) };

        var line = (await Overview([beta, alpha], expenses)).Categories;

        Assert.Equal([new CategorySegment("grey", 1_000), new CategorySegment("mint", 1_000)], line.Segments);
        Assert.Equal(["Alfa", "Beta"], line.Key.Select(item => item.Text));
    }

    [Fact]
    public async Task Should_not_draw_a_line_when_nothing_was_spent()
    {
        var line = (await Overview([NewCategory("Cibo")])).Categories;

        Assert.False(line.HasSegments);
        Assert.Empty(line.Segments);
        Assert.Empty(line.Key);
    }

    [Fact]
    public async Task Should_name_the_three_largest_categories_in_the_key()
    {
        var categories = new[]
        {
            NewCategory("Cibo", color: "mint"),
            NewCategory("Casa", color: "coral"),
            NewCategory("Auto", color: "peri"),
            NewCategory("Svago", color: "sky")
        };
        var expenses = categories.Select((category, index) => Spend(category, (4 - index) * 1_000, Today));

        var line = (await Overview(categories, expenses)).Categories;

        Assert.Equal(
            [new CategoryKeyItem("Cibo", "mint", false), new CategoryKeyItem("Casa", "coral", false),
                new CategoryKeyItem("Auto", "peri", false)],
            line.Key);
        Assert.Equal(4, line.Segments.Count);
    }

    [Fact]
    public async Task Should_mark_a_category_over_its_budget_in_the_key()
    {
        var over = NewCategory("Cibo", budgetCents: 20_000, color: "mint");
        var within = NewCategory("Casa", budgetCents: 20_000, color: "coral");
        var withoutBudget = NewCategory("Auto", budgetCents: 0, color: "peri");
        var expenses = new[] { Spend(over, 30_000, Today), Spend(within, 20_000, Today), Spend(withoutBudget, 90_000, Today) };

        var line = (await Overview([over, within, withoutBudget], expenses)).Categories;

        Assert.Equal(
            [new CategoryKeyItem("Auto", "peri", false), new CategoryKeyItem("Cibo oltre", "mint", true),
                new CategoryKeyItem("Casa", "coral", false)],
            line.Key);
    }

    [Fact]
    public async Task Should_name_the_over_budget_category_in_the_key()
    {
        var top = new[]
        {
            NewCategory("Cibo", color: "mint"),
            NewCategory("Casa", color: "coral"),
            NewCategory("Auto", color: "peri")
        };
        var slightlyOver = NewCategory("Svago", budgetCents: 4_000, color: "sky");
        var farOver = NewCategory("Viaggi", budgetCents: 1_000, color: "lilac");
        var withinBudget = NewCategory("Libri", budgetCents: 9_000, color: "butter");
        var expenses = top
            .Select((category, index) => Spend(category, 100_000 - index * 1_000, Today))
            .Concat([Spend(slightlyOver, 5_000, Today), Spend(farOver, 4_000, Today), Spend(withinBudget, 3_000, Today)]);

        var line = (await Overview([.. top, slightlyOver, farOver, withinBudget], expenses)).Categories;

        Assert.Equal(["Cibo", "Casa", "Auto", "Viaggi oltre"], line.Key.Select(item => item.Text));
        Assert.Equal([false, false, false, true], line.Key.Select(item => item.IsOverBudget));
        Assert.Equal("lilac", line.Key[^1].ColorKey);
    }

    [Fact]
    public async Task Should_scale_category_budgets_by_twelve_for_a_year()
    {
        var category = NewCategory("Cibo", budgetCents: 10_000);
        var expenses = new[] { Spend(category, 119_000, new DateOnly(2026, 3, 1)) };

        var month = (await Overview([category], expenses, Period.Month(2026, 3))).Categories;
        var year = (await Overview([category], expenses, Period.Year(2026))).Categories;
        var overYear = (await Overview([category], [.. expenses, Spend(category, 2_000, new DateOnly(2026, 4, 1))], Period.Year(2026))).Categories;

        Assert.True(month.Key[0].IsOverBudget);
        Assert.False(year.Key[0].IsOverBudget);
        Assert.True(overYear.Key[0].IsOverBudget);
    }

    [Fact]
    public async Task Should_describe_the_line_for_screen_readers()
    {
        var food = NewCategory("Cibo");
        var home = NewCategory("Casa");
        var archived = NewCategory("Vecchia", archived: true);
        var expenses = new[] { Spend(food, 30_000, Today), Spend(home, 10_000, Today), Spend(archived, 2_000, Today) };

        var line = (await Overview([food, home, archived], expenses)).Categories;

        Assert.Equal("Spesa per categoria: Cibo 300,00 €, Casa 100,00 €, Altre categorie 20,00 €.", line.Description);
    }

    [Fact]
    public async Task Should_chart_one_bar_per_day_with_today_highlighted_and_future_days_as_stubs()
    {
        var category = NewCategory("Spesa");
        var expenses = new[]
        {
            Spend(category, 7_450, new DateOnly(2026, 11, 1)),
            Spend(category, 2_700, Today),
            Spend(category, 99_900, new DateOnly(2026, 11, 25))
        };

        var chart = (await Overview([category], expenses)).Chart;

        Assert.Equal(30, chart.Bars.Count);
        Assert.All(chart.Bars.Take(18), bar => Assert.Equal(SpendBarKind.Past, bar.Kind));
        Assert.Equal(SpendBarKind.Current, chart.Bars[18].Kind);
        Assert.All(chart.Bars.Skip(19), bar => Assert.Equal(new SpendBar(0, SpendBarKind.Future), bar));
        Assert.Equal(1.0, chart.Bars[0].Fraction, 6);
        Assert.Equal(2_700d / 7_450, chart.Bars[18].Fraction, 6);
        Assert.Equal(0, chart.Bars[1].Fraction);
        Assert.Equal("1 nov", chart.StartLabel);
        Assert.Equal("30 nov", chart.EndLabel);
        Assert.True(chart.HasHighlight);
        Assert.Equal("oggi 27,00 €", chart.HighlightLabel);
        Assert.Equal(18.5 / 30, chart.HighlightPosition, 6);
    }

    [Fact]
    public async Task Should_chart_every_day_of_a_past_month_as_past()
    {
        var category = NewCategory("Spesa");
        var expenses = new[]
        {
            Spend(category, 2_000, new DateOnly(2026, 9, 10)),
            Spend(category, 4_000, new DateOnly(2026, 9, 30))
        };

        var chart = (await Overview([category], expenses, Period.Month(2026, 9))).Chart;

        Assert.Equal(30, chart.Bars.Count);
        Assert.All(chart.Bars, bar => Assert.Equal(SpendBarKind.Past, bar.Kind));
        Assert.Equal(1.0, chart.Bars[29].Fraction, 6);
        Assert.Equal(0.5, chart.Bars[9].Fraction, 6);
        Assert.False(chart.HasHighlight);
        Assert.Equal("", chart.HighlightLabel);
        Assert.Equal("1 set", chart.StartLabel);
        Assert.Equal("30 set", chart.EndLabel);
    }

    [Fact]
    public async Task Should_draw_one_bar_per_day_of_a_leap_february()
    {
        var chart = (await Overview(period: Period.Month(2028, 2))).Chart;

        Assert.Equal(29, chart.Bars.Count);
        Assert.Equal("29 feb", chart.EndLabel);
    }

    [Fact]
    public async Task Should_scale_the_pace_line_to_the_daily_budget()
    {
        var category = NewCategory("Spesa", budgetCents: 300_000);
        var calm = new[] { Spend(category, 7_500, new DateOnly(2026, 11, 1)) };
        var heavy = new[] { Spend(category, 20_000, new DateOnly(2026, 11, 1)) };

        var belowPace = (await Overview([category], calm)).Chart;
        var abovePace = (await Overview([category], heavy)).Chart;

        Assert.True(belowPace.HasPace);
        Assert.Equal(1.0, belowPace.PaceFraction!.Value, 6);
        Assert.Equal(0.75, belowPace.Bars[0].Fraction, 6);
        Assert.Equal(0.5, abovePace.PaceFraction!.Value, 6);
        Assert.Equal(1.0, abovePace.Bars[0].Fraction, 6);
    }

    [Fact]
    public async Task Should_hide_the_pace_line_without_a_budget()
    {
        var category = NewCategory("Spesa");

        var chart = (await Overview([category], [Spend(category, 1_000, Today)])).Chart;

        Assert.False(chart.HasPace);
        Assert.Null(chart.PaceFraction);
    }

    [Fact]
    public async Task Should_draw_empty_bars_when_nothing_was_spent()
    {
        var chart = (await Overview()).Chart;

        Assert.All(chart.Bars, bar => Assert.Equal(0, bar.Fraction));
        Assert.Null(chart.PaceFraction);
    }

    [Fact]
    public async Task Should_chart_twelve_months_in_year_mode_with_the_annual_pace()
    {
        var category = NewCategory("Spesa", budgetCents: 100_000);
        var expenses = new[]
        {
            Spend(category, 50_000, new DateOnly(2026, 1, 5)),
            Spend(category, 25_000, new DateOnly(2026, 1, 20)),
            Spend(category, 40_000, new DateOnly(2026, 11, 2)),
            Spend(category, 990_000, new DateOnly(2026, 12, 2))
        };

        var chart = (await Overview([category], expenses, Period.Year(2026))).Chart;

        Assert.Equal(12, chart.Bars.Count);
        Assert.All(chart.Bars.Take(10), bar => Assert.Equal(SpendBarKind.Past, bar.Kind));
        Assert.Equal(SpendBarKind.Current, chart.Bars[10].Kind);
        Assert.Equal(new SpendBar(0, SpendBarKind.Future), chart.Bars[11]);
        Assert.Equal(1.0, chart.PaceFraction!.Value, 6);
        Assert.Equal(0.75, chart.Bars[0].Fraction, 6);
        Assert.Equal("gen", chart.StartLabel);
        Assert.Equal("dic", chart.EndLabel);
        Assert.Equal("nov 400,00 €", chart.HighlightLabel);
        Assert.Equal(10.5 / 12, chart.HighlightPosition, 6);
    }

    [Fact]
    public async Task Should_describe_the_chart_for_screen_readers()
    {
        var category = NewCategory("Spesa", budgetCents: 160_000);
        var expenses = new[]
        {
            Spend(category, 7_450, new DateOnly(2026, 11, 1)),
            Spend(category, 2_700, Today)
        };

        var chart = (await Overview([category], expenses)).Chart;
        var year = (await Overview([category], expenses, Period.Year(2026))).Chart;

        Assert.Equal(
            "Spesa giornaliera di novembre 2026. Giorno più alto: 1 novembre, 74,50 €. Oggi: 27,00 €. " +
            "Media per restare nel budget: 53,33 € al giorno.",
            chart.Description);
        Assert.Equal(
            "Spesa mensile del 2026. Mese più alto: novembre, 101,50 €. Questo mese: 101,50 €. " +
            "Media per restare nel budget: 1.600,00 € al mese.",
            year.Description);
    }

    [Fact]
    public async Task Should_describe_a_chart_without_spending_or_budget()
    {
        var past = (await Overview(period: Period.Month(2026, 9))).Chart;
        var current = (await Overview()).Chart;

        Assert.Equal("Spesa giornaliera di settembre 2026. Nessuna spesa registrata.", past.Description);
        Assert.Equal("Spesa giornaliera di novembre 2026. Nessuna spesa registrata. Oggi: 0,00 €.", current.Description);
    }

    [Fact]
    public async Task Should_return_the_repository_failure()
    {
        var failingCategories = new FakeCategoryRepository { Failure = Failed };
        var failingExpenses = new FakeExpenseRepository { Failure = Failed };

        var categoryFailure = await new HomeOverviewService(failingCategories, new FakeExpenseRepository(), () => Today).Get();
        var expenseFailure = await new HomeOverviewService(new FakeCategoryRepository(), failingExpenses, () => Today).Get();

        Assert.Equal(Failed, categoryFailure.Error);
        Assert.Equal(Failed, expenseFailure.Error);
    }

    private static async Task<HomeOverview> Overview(IEnumerable<Category>? categories = null,
        IEnumerable<Expense>? expenses = null, Period? period = null, DateOnly? today = null)
    {
        var categoryRepository = new FakeCategoryRepository();
        categoryRepository.Items.AddRange(categories ?? []);
        var expenseRepository = new FakeExpenseRepository();
        expenseRepository.Items.AddRange(expenses ?? []);
        var service = new HomeOverviewService(categoryRepository, expenseRepository, () => today ?? Today);

        var result = await service.Get(period);

        Assert.True(result.IsSuccess);
        return result.Value;
    }

    private static Category NewCategory(string name, long? budgetCents = null, bool archived = false, string color = "mint")
    {
        var category = new Category(Guid.NewGuid(), name, "cart", color, budgetCents.HasValue ? new Money(budgetCents.Value) : null);
        if (archived)
            category.Archive();

        return category;
    }

    private static Expense Spend(Category category, long cents, DateOnly on) =>
        new(Guid.NewGuid(), category.CategoryId, new Money(cents), on);
}
