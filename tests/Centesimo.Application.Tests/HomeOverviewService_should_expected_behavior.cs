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
        Assert.Null(overview.Navigation.Next);
    }

    [Fact]
    public async Task Should_follow_the_clock_when_the_month_changes()
    {
        var overview = await Overview(today: new DateOnly(2026, 12, 1));

        Assert.Equal(Period.Month(2026, 12), overview.Period);
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
    public async Task Should_be_a_first_run_until_a_category_exists()
    {
        var month = await Overview();
        var year = await Overview(period: Period.Year(2026));

        Assert.True(month.IsFirstRun);
        Assert.Equal(new BudgetSummary("Ancora nessuna spesa a novembre", "", ""), month.Budget);
        Assert.Equal(new BudgetSummary("Ancora nessuna spesa nel 2026", "", ""), year.Budget);
        Assert.Equal("0,00", month.Total);
    }

    [Fact]
    public async Task Should_not_be_a_first_run_with_a_category_even_an_archived_one()
    {
        var active = await Overview([NewCategory("Spesa")]);
        var archived = await Overview([NewCategory("Vecchia", archived: true)]);

        Assert.False(active.IsFirstRun);
        Assert.False(archived.IsFirstRun);
        Assert.Equal(new BudgetSummary("Nessun budget impostato", "", ""), archived.Budget);
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
    public async Task Should_list_every_category_in_the_key_largest_first()
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
            [new CategoryKeyItem(categories[0].CategoryId, "Cibo", "mint", false),
                new CategoryKeyItem(categories[1].CategoryId, "Casa", "coral", false),
                new CategoryKeyItem(categories[2].CategoryId, "Auto", "peri", false),
                new CategoryKeyItem(categories[3].CategoryId, "Svago", "sky", false)],
            line.Key);
    }

    [Fact]
    public async Task Should_leave_archived_and_unspent_categories_out_of_the_key()
    {
        var spent = NewCategory("Cibo");
        var unused = NewCategory("Casa");
        var archived = NewCategory("Vecchia", archived: true);
        var expenses = new[] { Spend(spent, 1_000, Today), Spend(archived, 5_000, Today) };

        var line = (await Overview([spent, unused, archived], expenses)).Categories;

        Assert.Equal(["Cibo"], line.Key.Select(item => item.Text));
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
            [new CategoryKeyItem(over.CategoryId, "Cibo", "mint", true),
                new CategoryKeyItem(withoutBudget.CategoryId, "Auto", "peri", false),
                new CategoryKeyItem(within.CategoryId, "Casa", "coral", false)],
            line.Key);
    }

    [Fact]
    public async Task Should_put_over_budget_categories_first_in_the_key_the_furthest_over_first()
    {
        var top = NewCategory("Cibo", color: "mint");
        var second = NewCategory("Casa", color: "coral");
        var slightlyOver = NewCategory("Svago", budgetCents: 4_000, color: "sky");
        var farOver = NewCategory("Viaggi", budgetCents: 1_000, color: "lilac");
        var withinBudget = NewCategory("Libri", budgetCents: 9_000, color: "butter");
        var expenses = new[]
        {
            Spend(top, 100_000, Today), Spend(second, 99_000, Today), Spend(slightlyOver, 5_000, Today),
            Spend(farOver, 4_000, Today), Spend(withinBudget, 3_000, Today)
        };

        var line = (await Overview([top, second, slightlyOver, farOver, withinBudget], expenses)).Categories;

        Assert.Equal(["Viaggi", "Svago", "Cibo", "Casa", "Libri"], line.Key.Select(item => item.Text));
        Assert.Equal([true, true, false, false, false], line.Key.Select(item => item.IsOverBudget));
    }

    [Fact]
    public async Task Should_order_key_items_with_the_same_overshoot_by_spending_then_name()
    {
        var beta = NewCategory("Beta", budgetCents: 1_000);
        var alpha = NewCategory("Alfa", budgetCents: 1_000);
        var bigger = NewCategory("Zeta", budgetCents: 2_000);
        var expenses = new[] { Spend(beta, 3_000, Today), Spend(alpha, 3_000, Today), Spend(bigger, 4_000, Today) };

        var line = (await Overview([beta, alpha, bigger], expenses)).Categories;

        Assert.Equal(["Zeta", "Alfa", "Beta"], line.Key.Select(item => item.Text));
    }

    [Fact]
    public async Task Should_describe_an_over_budget_key_item_as_over_budget_for_screen_readers()
    {
        var over = NewCategory("Cibo", budgetCents: 20_000, color: "mint");
        var within = NewCategory("Casa", budgetCents: 20_000, color: "coral");
        var expenses = new[] { Spend(over, 30_000, Today), Spend(within, 10_000, Today) };

        var line = (await Overview([over, within], expenses)).Categories;

        Assert.Equal(["Cibo, oltre il budget", "Casa"], line.Key.Select(item => item.Description));
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
        Assert.Equal("media per restare nel budget", belowPace.PaceLabel);
    }

    [Fact]
    public async Task Should_hide_the_pace_line_without_a_budget()
    {
        var category = NewCategory("Spesa");

        var chart = (await Overview([category], [Spend(category, 1_000, Today)])).Chart;

        Assert.False(chart.HasPace);
        Assert.Null(chart.PaceFraction);
        Assert.Equal("", chart.PaceLabel);
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
    public async Task Should_list_the_five_latest_expenses_with_note_or_category_name()
    {
        var food = NewCategory("Spesa", color: "mint", icon: "food");
        var leisure = NewCategory("Svago", color: "coral", icon: "leisure");
        var expenses = new[]
        {
            Spend(food, 1_250, Today),
            Spend(leisure, 1_599, new DateOnly(2026, 11, 18), note: "Netflix", recurringPaymentId: Guid.NewGuid()),
            Spend(food, 4_000, new DateOnly(2026, 11, 12), note: "Mercato"),
            Spend(food, 100, new DateOnly(2026, 11, 10)),
            Spend(food, 200, new DateOnly(2026, 11, 8), recurringPaymentId: Guid.NewGuid()),
            Spend(food, 300, new DateOnly(2026, 11, 3)),
            Spend(food, 400, new DateOnly(2026, 11, 1))
        };

        var overview = await Overview([food, leisure], expenses);

        Assert.True(overview.HasLatest);
        Assert.False(overview.HasNoLatest);
        Assert.Equal(["Spesa", "Svago", "Spesa", "Spesa", "Spesa"], overview.Latest.Select(item => item.Title));
        Assert.Equal(
            ["Oggi", "Ieri · Netflix · automatica", "12 nov · Mercato", "10 nov", "8 nov · automatica"],
            overview.Latest.Select(item => item.Detail));
        Assert.Equal(
            ["12,50 €", "15,99 €", "40,00 €", "1,00 €", "2,00 €"],
            overview.Latest.Select(item => item.Amount));
        Assert.Equal(new[] { "food", "leisure", "food", "food", "food" }, overview.Latest.Select(item => item.IconKey));
        Assert.Equal(new[] { "mint", "coral", "mint", "mint", "mint" }, overview.Latest.Select(item => item.ColorKey));
        Assert.Equal("Svago, Ieri · Netflix · automatica, 15,99 €", overview.Latest[1].Description);
    }

    [Fact]
    public async Task Should_say_the_note_or_else_the_tag_after_the_day()
    {
        var food = NewCategory("Spesa");
        var pizza = new Tag(Guid.NewGuid(), food.CategoryId, "Pizza");
        var expenses = new[]
        {
            Spend(food, 1_250, Today),
            Spend(food, 900, new DateOnly(2026, 11, 18), note: "  ", tagId: pizza.TagId),
            Spend(food, 500, new DateOnly(2026, 11, 12), note: "Mercato", tagId: pizza.TagId),
            Spend(food, 400, new DateOnly(2026, 11, 10), tagId: pizza.TagId, recurringPaymentId: Guid.NewGuid())
        };

        var latest = (await Overview([food], expenses, tags: [pizza])).Latest;

        Assert.Equal([("Spesa", "Oggi"), ("Spesa", "Ieri · Pizza"), ("Spesa", "12 nov · Mercato"),
                ("Spesa", "10 nov · Pizza · automatica")],
            latest.Select(item => (item.Title, item.Detail)));
        Assert.Equal("Spesa, Oggi, 12,50 €", latest[0].Description);
    }

    [Fact]
    public async Task Should_keep_the_tag_name_of_an_archived_tag_in_the_latest_expenses()
    {
        var food = NewCategory("Spesa");
        var old = new Tag(Guid.NewGuid(), food.CategoryId, "Vecchio");
        old.Archive();

        var latest = (await Overview([food], [Spend(food, 500, Today, tagId: old.TagId)], tags: [old])).Latest;

        Assert.Equal("Oggi · Vecchio", latest.Single().Detail);
    }

    [Fact]
    public async Task Should_fall_back_to_the_generic_icon_and_color_for_a_missing_category()
    {
        var orphan = new Expense(Guid.NewGuid(), Guid.NewGuid(), new Money(500), Today);
        var legacy = NewCategory("Auto", color: "#176B5B", icon: "rocket");
        var expenses = new[] { orphan, Spend(legacy, 700, new DateOnly(2026, 11, 18)) };

        var latest = (await Overview([legacy], expenses)).Latest;

        Assert.Equal(new LatestExpense(orphan.ExpenseId, "Categoria non disponibile", "Oggi",
            "5,00 €", "more", "grey"), latest[0]);
        Assert.Equal(("more", "grey"), (latest[1].IconKey, latest[1].ColorKey));
    }

    [Fact]
    public async Task Should_list_only_expenses_inside_the_period_most_recent_first()
    {
        var category = NewCategory("Spesa");
        var expenses = new[]
        {
            Spend(category, 100, new DateOnly(2026, 10, 31)),
            Spend(category, 200, new DateOnly(2026, 11, 2)),
            Spend(category, 300, new DateOnly(2026, 11, 3))
        };

        var latest = (await Overview([category], expenses)).Latest;

        Assert.Equal(["3,00 €", "2,00 €"], latest.Select(item => item.Amount));
    }

    [Fact]
    public async Task Should_break_ties_within_a_day_by_expense_id()
    {
        var category = NewCategory("Spesa");
        var first = new Guid("00000000-0000-0000-0000-000000000001");
        var second = new Guid("00000000-0000-0000-0000-000000000002");
        var expenses = new[]
        {
            Spend(category, 100, Today, expenseId: first),
            Spend(category, 200, Today, expenseId: second)
        };

        var latest = (await Overview([category], expenses)).Latest;

        Assert.Equal([second, first], latest.Select(item => item.ExpenseId));
    }

    [Fact]
    public async Task Should_list_expenses_entered_in_sequence_newest_first_within_a_day()
    {
        var category = NewCategory("Spesa");
        var categories = new FakeCategoryRepository();
        categories.Items.Add(category);
        var expenses = new FakeExpenseRepository();
        var entered = 0;
        var service = new ExpenseService(categories, new FakeTagRepository(), expenses,
            () => Guid.CreateVersion7(new DateTimeOffset(2026, 11, 19, 9, 0, 0, TimeSpan.Zero).AddSeconds(entered++)));
        foreach (var note in new[] { "primo", "secondo", "terzo" })
            await service.Create(new SaveExpenseRequest(category.CategoryId, 100, Today, Note: note));

        var overview = await new HomeOverviewService(categories, new FakeTagRepository(), expenses, () => Today).Get();

        Assert.Equal(["Oggi · terzo", "Oggi · secondo", "Oggi · primo"], overview.Value.Latest.Select(item => item.Detail));
    }

    [Fact]
    public async Task Should_list_nothing_when_the_period_has_no_expenses()
    {
        var overview = await Overview([NewCategory("Spesa")]);

        Assert.Empty(overview.Latest);
        Assert.False(overview.HasLatest);
        Assert.True(overview.HasNoLatest);
    }

    [Fact]
    public async Task Should_return_the_repository_failure()
    {
        var failingCategories = new FakeCategoryRepository { Failure = Failed };
        var failingExpenses = new FakeExpenseRepository { Failure = Failed };

        var failingTags = new FakeTagRepository { Failure = Failed };

        var categoryFailure = await new HomeOverviewService(failingCategories, new FakeTagRepository(),
            new FakeExpenseRepository(), () => Today).Get();
        var tagFailure = await new HomeOverviewService(new FakeCategoryRepository(), failingTags,
            new FakeExpenseRepository(), () => Today).Get();
        var expenseFailure = await new HomeOverviewService(new FakeCategoryRepository(), new FakeTagRepository(),
            failingExpenses, () => Today).Get();

        Assert.Equal(Failed, categoryFailure.Error);
        Assert.Equal(Failed, tagFailure.Error);
        Assert.Equal(Failed, expenseFailure.Error);
    }

    private static async Task<HomeOverview> Overview(IEnumerable<Category>? categories = null,
        IEnumerable<Expense>? expenses = null, Period? period = null, DateOnly? today = null,
        IEnumerable<Tag>? tags = null)
    {
        var categoryRepository = new FakeCategoryRepository();
        categoryRepository.Items.AddRange(categories ?? []);
        var expenseRepository = new FakeExpenseRepository();
        expenseRepository.Items.AddRange(expenses ?? []);
        var tagRepository = new FakeTagRepository();
        tagRepository.Items.AddRange(tags ?? []);
        var service = new HomeOverviewService(categoryRepository, tagRepository, expenseRepository, () => today ?? Today);

        var result = await service.Get(period);

        Assert.True(result.IsSuccess);
        return result.Value;
    }

    private static Category NewCategory(string name, long? budgetCents = null, bool archived = false,
        string color = "mint", string icon = "cart")
    {
        var category = new Category(Guid.NewGuid(), name, icon, color, budgetCents.HasValue ? new Money(budgetCents.Value) : null);
        if (archived)
            category.Archive();

        return category;
    }

    private static Expense Spend(Category category, long cents, DateOnly on, string note = "",
        Guid? recurringPaymentId = null, Guid? expenseId = null, Guid? tagId = null) =>
        new(expenseId ?? Guid.NewGuid(), category.CategoryId, new Money(cents), on, tagId, note,
            recurringPaymentId: recurringPaymentId);
}
