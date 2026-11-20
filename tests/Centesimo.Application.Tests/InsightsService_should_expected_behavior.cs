using Centesimo.Domain;

namespace Centesimo.Application.Tests;

public sealed class InsightsService_should_expected_behavior
{
    private const char Nbsp = ' ';

    [Fact]
    public async Task Should_return_empty_overview_when_period_has_no_expenses()
    {
        var service = CreateService(new DateOnly(2026, 7, 20));

        var result = await service.Get(Period.Month(2026, 7));

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.SpentCents);
        Assert.Null(result.Value.ComparedSpentCents);
        Assert.Empty(result.Value.Categories);
        Assert.Empty(result.Value.Insights);
    }

    [Fact]
    public async Task Should_not_render_category_that_only_has_previous_period_spending()
    {
        var category = new Category(Guid.NewGuid(), "Spesa", "cart", "#196D61");
        var expenses = new FakeExpenseRepository();
        expenses.Items.Add(new Expense(Guid.NewGuid(), category.CategoryId, new Money(1000), new DateOnly(2026, 6, 20)));

        var result = await CreateService(new DateOnly(2026, 7, 20), category, expenses).Get(Period.Month(2026, 7));

        Assert.Empty(result.Value.Categories);
    }

    [Fact]
    public async Task Should_compare_current_month_with_same_elapsed_days_of_previous_month()
    {
        var category = new Category(Guid.NewGuid(), "Spesa", "cart", "#196D61");
        var expenses = new FakeExpenseRepository();
        expenses.Items.AddRange([
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(1000), new DateOnly(2026, 7, 20)),
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(800), new DateOnly(2026, 6, 20)),
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(900), new DateOnly(2026, 6, 25))
        ]);
        var service = CreateService(new DateOnly(2026, 7, 20), category, expenses);

        var result = await service.Get(Period.Month(2026, 7));

        Assert.True(result.IsSuccess);
        Assert.Equal(new DateOnly(2026, 6, 1), result.Value.From.AddMonths(-1));
        Assert.Equal(800, result.Value.ComparedSpentCents);
        Assert.Equal(0.25, result.Value.ChangePercentage!.Value, 3);
    }

    [Fact]
    public async Task Should_not_compare_when_previous_equivalent_period_has_no_expenses()
    {
        var category = new Category(Guid.NewGuid(), "Spesa", "cart", "#196D61");
        var expenses = new FakeExpenseRepository();
        expenses.Items.Add(new Expense(Guid.NewGuid(), category.CategoryId, new Money(1000), new DateOnly(2026, 7, 20)));

        var result = await CreateService(new DateOnly(2026, 7, 20), category, expenses).Get(Period.Month(2026, 7));

        Assert.Null(result.Value.ComparedSpentCents);
        Assert.DoesNotContain(result.Value.Insights, x => x.Kind is InsightKind.Increased or InsightKind.Decreased or InsightKind.Stable);
    }

    [Fact]
    public async Task Should_create_increase_decrease_top_and_stable_insights()
    {
        var food = new Category(Guid.NewGuid(), "Cibo", "food", "#196D61");
        var car = new Category(Guid.NewGuid(), "Auto", "car", "#196D61");
        var expenses = new FakeExpenseRepository();
        expenses.Items.AddRange([
            new Expense(Guid.NewGuid(), food.CategoryId, new Money(2200), new DateOnly(2026, 7, 20)),
            new Expense(Guid.NewGuid(), car.CategoryId, new Money(800), new DateOnly(2026, 7, 20)),
            new Expense(Guid.NewGuid(), food.CategoryId, new Money(1800), new DateOnly(2026, 6, 20)),
            new Expense(Guid.NewGuid(), car.CategoryId, new Money(1200), new DateOnly(2026, 6, 20))
        ]);

        var result = await CreateService(new DateOnly(2026, 7, 20), [food, car], expenses).Get(Period.Month(2026, 7));

        Assert.Contains(result.Value.Insights, x => x.Kind == InsightKind.Increased);
        Assert.Contains(result.Value.Insights, x => x.Kind == InsightKind.Decreased);
        Assert.Contains(result.Value.Insights, x => x.Kind == InsightKind.TopCategory
            && x.Target == InsightTarget.Category(food.CategoryId, Period.Month(2026, 7)));
    }

    [Fact]
    public async Task Should_compare_current_year_with_same_elapsed_days_of_previous_year()
    {
        var category = new Category(Guid.NewGuid(), "Spesa", "cart", "#196D61");
        var expenses = new FakeExpenseRepository();
        expenses.Items.AddRange([
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(1200), new DateOnly(2026, 7, 20)),
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(1000), new DateOnly(2025, 7, 20)),
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(900), new DateOnly(2025, 8, 1))
        ]);

        var result = await CreateService(new DateOnly(2026, 7, 20), category, expenses).Get(Period.Year(2026));

        Assert.Equal(1000, result.Value.ComparedSpentCents);
    }

    [Fact]
    public async Task Should_create_stable_insight_when_total_change_is_small()
    {
        var category = new Category(Guid.NewGuid(), "Spesa", "cart", "#196D61");
        var expenses = new FakeExpenseRepository();
        expenses.Items.AddRange([
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(1050), new DateOnly(2026, 7, 20)),
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(1000), new DateOnly(2026, 6, 20))
        ]);

        var result = await CreateService(new DateOnly(2026, 7, 20), category, expenses).Get(Period.Month(2026, 7));

        Assert.Contains(result.Value.Insights, x => x.Kind == InsightKind.Stable);
    }

    [Fact]
    public async Task Should_create_new_habit_and_unusual_expense_insights_from_local_history()
    {
        var category = new Category(Guid.NewGuid(), "Abbonamenti", "tech", "#196D61");
        var expenses = new FakeExpenseRepository();
        expenses.Items.AddRange([
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(100), new DateOnly(2026, 7, 2)),
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(100), new DateOnly(2026, 7, 3)),
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(1000), new DateOnly(2026, 7, 4))
        ]);

        var result = await CreateService(new DateOnly(2026, 7, 20), category, expenses).Get(Period.Month(2026, 7));

        Assert.Contains(result.Value.Insights, x => x.Kind == InsightKind.NewHabit);
        Assert.Contains(result.Value.Insights, x => x.Kind == InsightKind.UnusualExpense && x.Target is InsightTarget.ExpenseTarget);
    }

    [Fact]
    public async Task Should_compare_march_first_with_matching_calendar_day_after_leap_year()
    {
        var category = new Category(Guid.NewGuid(), "Spesa", "cart", "#196D61");
        var expenses = new FakeExpenseRepository();
        expenses.Items.AddRange([
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(1000), new DateOnly(2025, 3, 1)),
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(700), new DateOnly(2024, 3, 1)),
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(900), new DateOnly(2024, 2, 29)),
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(300), new DateOnly(2024, 3, 2))
        ]);

        var result = await CreateService(new DateOnly(2025, 3, 1), category, expenses).Get(Period.Year(2025));

        Assert.Equal(1600, result.Value.ComparedSpentCents);
    }

    [Fact]
    public async Task Should_compare_february_twenty_ninth_with_the_clamped_last_day_of_february_the_previous_year()
    {
        var category = new Category(Guid.NewGuid(), "Spesa", "cart", "#196D61");
        var expenses = new FakeExpenseRepository();
        expenses.Items.AddRange([
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(1000), new DateOnly(2024, 2, 29)),
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(500), new DateOnly(2023, 2, 28)),
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(700), new DateOnly(2023, 3, 1))
        ]);

        var result = await CreateService(new DateOnly(2024, 2, 29), category, expenses).Get(Period.Year(2024));

        Assert.Equal(500, result.Value.ComparedSpentCents);
    }

    [Theory]
    [InlineData(29)]
    [InlineData(30)]
    [InlineData(31)]
    public async Task Should_compare_march_month_end_with_the_whole_of_a_shorter_february(int day)
    {
        var category = new Category(Guid.NewGuid(), "Spesa", "cart", "#196D61");
        var expenses = new FakeExpenseRepository();
        expenses.Items.AddRange([
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(1000), new DateOnly(2025, 3, day)),
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(500), new DateOnly(2025, 2, 28)),
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(300), new DateOnly(2025, 2, 1))
        ]);

        var result = await CreateService(new DateOnly(2025, 3, day), category, expenses).Get(Period.Month(2025, 3));

        Assert.Equal(800, result.Value.ComparedSpentCents);
    }

    [Fact]
    public async Task Should_compare_a_past_month_whole_with_the_whole_previous_month()
    {
        var category = new Category(Guid.NewGuid(), "Spesa", "cart", "#196D61");
        var expenses = new FakeExpenseRepository();
        expenses.Items.AddRange([
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(1000), new DateOnly(2026, 10, 25)),
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(800), new DateOnly(2026, 9, 25)),
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(5000), new DateOnly(2026, 11, 3)),
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(5000), new DateOnly(2026, 8, 31))
        ]);

        var result = await CreateService(new DateOnly(2026, 11, 19), category, expenses).Get(Period.Month(2026, 10));

        Assert.Equal(Period.Month(2026, 10), result.Value.Period);
        Assert.Equal((new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31)), (result.Value.From, result.Value.To));
        Assert.Equal(1000, result.Value.SpentCents);
        Assert.Equal(800, result.Value.ComparedSpentCents);
        Assert.Equal(0.25, result.Value.ChangePercentage!.Value, 3);
    }

    [Fact]
    public async Task Should_compare_a_past_year_whole_with_the_whole_previous_year()
    {
        var category = new Category(Guid.NewGuid(), "Spesa", "cart", "#196D61");
        var expenses = new FakeExpenseRepository();
        expenses.Items.AddRange([
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(1000), new DateOnly(2025, 12, 31)),
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(400), new DateOnly(2024, 12, 31)),
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(5000), new DateOnly(2026, 1, 1))
        ]);

        var result = await CreateService(new DateOnly(2026, 11, 19), category, expenses).Get(Period.Year(2025));

        Assert.Equal((new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31)), (result.Value.From, result.Value.To));
        Assert.Equal(1000, result.Value.SpentCents);
        Assert.Equal(400, result.Value.ComparedSpentCents);
    }

    [Fact]
    public async Task Should_clamp_the_current_period_to_today_like_the_home()
    {
        var category = new Category(Guid.NewGuid(), "Spesa", "cart", "#196D61");
        var expenses = new FakeExpenseRepository();
        expenses.Items.AddRange([
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(1000), new DateOnly(2026, 11, 19)),
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(900), new DateOnly(2026, 10, 19)),
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(700), new DateOnly(2026, 10, 20))
        ]);

        var result = await CreateService(new DateOnly(2026, 11, 19), category, expenses).Get(Period.Month(2026, 11));

        Assert.Equal((new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 19)), (result.Value.From, result.Value.To));
        Assert.Equal(900, result.Value.ComparedSpentCents);
    }

    [Fact]
    public async Task Should_answer_the_month_that_contains_today_when_no_period_is_given()
    {
        var category = new Category(Guid.NewGuid(), "Spesa", "cart", "#196D61");
        var expenses = new FakeExpenseRepository();
        expenses.Items.AddRange([
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(1000), new DateOnly(2026, 7, 20)),
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(800), new DateOnly(2026, 6, 20))
        ]);
        var service = CreateService(new DateOnly(2026, 7, 20), category, expenses);

        var overview = (await service.Get()).Value;

        Assert.Equal(Period.Month(2026, 7), overview.Period);
        Assert.Equal(await Summary(service, Period.Month(2026, 7)), Summarize(overview));
    }

    [Fact]
    public async Task Should_say_where_the_arrows_lead_and_whether_the_period_is_compared_to_date()
    {
        var service = CreateService(new DateOnly(2026, 7, 20));

        var current = (await service.Get(Period.Month(2026, 7))).Value;
        var past = (await service.Get(Period.Month(2026, 5))).Value;
        var year = (await service.Get(Period.Year(2026))).Value;
        var pastYear = (await service.Get(Period.Year(2025))).Value;

        Assert.Equal(new HomeNavigation(Period.Month(2026, 6), null, Period.Year(2026)), current.Navigation);
        Assert.Equal(new HomeNavigation(Period.Month(2026, 4), Period.Month(2026, 6), Period.Year(2026)), past.Navigation);
        Assert.Equal(new HomeNavigation(Period.Year(2025), null, Period.Month(2026, 7)), year.Navigation);
        Assert.Equal([true, false, true, false], new[] { current.IsToDate, past.IsToDate, year.IsToDate, pastYear.IsToDate });
    }

    [Fact]
    public async Task Should_return_the_repository_failure()
    {
        var failed = new Error("Test.Failed", "Operazione non riuscita.");
        var failingCategories = new FakeCategoryRepository { Failure = failed };

        var categoryFailure = await new InsightsService(failingCategories, new FakeExpenseRepository(),
            () => new DateOnly(2026, 7, 20)).Get(Period.Month(2026, 7));
        var expenseFailure = await new InsightsService(new FakeCategoryRepository(),
            new FakeExpenseRepository { Failure = failed }, () => new DateOnly(2026, 7, 20)).Get(Period.Month(2026, 7));

        Assert.Equal(failed, categoryFailure.Error);
        Assert.Equal(failed, expenseFailure.Error);
    }

    [Fact]
    public async Task Should_point_each_insight_at_its_category_in_the_period_or_at_its_expense()
    {
        var food = new Category(Guid.NewGuid(), "Cibo", "food", "#196D61");
        var car = new Category(Guid.NewGuid(), "Auto", "car", "#196D61");
        var leisure = new Category(Guid.NewGuid(), "Svago", "leisure", "#196D61");
        var unusual = new Expense(Guid.NewGuid(), leisure.CategoryId, new Money(3000), new DateOnly(2026, 7, 4));
        var expenses = new FakeExpenseRepository();
        expenses.Items.AddRange([
            new Expense(Guid.NewGuid(), food.CategoryId, new Money(2200), new DateOnly(2026, 7, 20)),
            new Expense(Guid.NewGuid(), car.CategoryId, new Money(800), new DateOnly(2026, 7, 5)),
            new Expense(Guid.NewGuid(), leisure.CategoryId, new Money(100), new DateOnly(2026, 7, 6)),
            unusual,
            new Expense(Guid.NewGuid(), food.CategoryId, new Money(1800), new DateOnly(2026, 6, 20)),
            new Expense(Guid.NewGuid(), car.CategoryId, new Money(1200), new DateOnly(2026, 6, 20))
        ]);
        var july = Period.Month(2026, 7);

        var result = await CreateService(new DateOnly(2026, 7, 20), [food, car, leisure], expenses).Get(july);

        var byKind = result.Value.Insights.ToDictionary(x => x.Kind);
        Assert.Equal(InsightTarget.Category(food.CategoryId, july), byKind[InsightKind.Increased].Target);
        Assert.Equal(InsightTarget.Category(car.CategoryId, july), byKind[InsightKind.Decreased].Target);
        Assert.Equal(InsightTarget.Category(leisure.CategoryId, july), byKind[InsightKind.TopCategory].Target);
        Assert.Equal(InsightTarget.Expense(unusual.ExpenseId), byKind[InsightKind.UnusualExpense].Target);
        Assert.Equal("Per Cibo hai speso il 22% in più di giugno.", byKind[InsightKind.Increased].Description);
        Assert.Equal("Per Auto hai speso il 33% in meno di giugno.", byKind[InsightKind.Decreased].Description);
    }

    [Fact]
    public async Task Should_point_a_year_insight_at_the_category_in_that_year()
    {
        var category = new Category(Guid.NewGuid(), "Cibo", "food", "#196D61");
        var expenses = new FakeExpenseRepository();
        expenses.Items.Add(new Expense(Guid.NewGuid(), category.CategoryId, new Money(2200), new DateOnly(2025, 7, 20)));

        var result = await CreateService(new DateOnly(2026, 11, 19), category, expenses).Get(Period.Year(2025));

        var top = Assert.Single(result.Value.Insights, x => x.Kind == InsightKind.TopCategory);
        Assert.Equal(InsightTarget.Category(category.CategoryId, Period.Year(2025)), top.Target);
        Assert.Equal("Cibo è il 100% delle spese del 2025.", top.Description);
    }

    [Fact]
    public async Task Should_measure_a_year_against_the_same_part_of_the_year_before_or_the_whole_of_it()
    {
        var category = new Category(Guid.NewGuid(), "Cibo", "food", "#196D61");
        var expenses = new FakeExpenseRepository();
        expenses.Items.AddRange([
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(3000), new DateOnly(2026, 3, 1)),
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(2000), new DateOnly(2025, 3, 1)),
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(1000), new DateOnly(2024, 3, 1))
        ]);
        var service = CreateService(new DateOnly(2026, 11, 19), category, expenses);

        var toDate = (await service.Get(Period.Year(2026))).Value.Insights.Single(x => x.Kind == InsightKind.Increased);
        var whole = (await service.Get(Period.Year(2025))).Value.Insights.Single(x => x.Kind == InsightKind.Increased);

        Assert.Equal("Per Cibo hai speso il 50% in più dello stesso periodo del 2025.", toDate.Description);
        Assert.Equal("Per Cibo hai speso il 100% in più del 2024.", whole.Description);
    }

    [Fact]
    public async Task Should_name_an_unusual_expense_by_its_note_amount_and_day()
    {
        var home = new Category(Guid.NewGuid(), "Casa", "home", "peri");
        var expenses = new FakeExpenseRepository();
        expenses.Items.AddRange([
            new Expense(Guid.NewGuid(), home.CategoryId, new Money(18_000), new DateOnly(2026, 11, 9), note: "Idraulico"),
            new Expense(Guid.NewGuid(), home.CategoryId, new Money(2_000), new DateOnly(2026, 11, 3))
        ]);

        var result = await CreateService(new DateOnly(2026, 11, 19), home, expenses).Get(Period.Month(2026, 11));

        var unusual = Assert.Single(result.Value.Insights, x => x.Kind == InsightKind.UnusualExpense);
        Assert.Equal($"Idraulico, 180,00{Nbsp}€ il 9 novembre, è molto più alta delle altre spese di Casa.", unusual.Description);
    }

    [Fact]
    public async Task Should_leave_the_stable_insight_without_a_target()
    {
        var category = new Category(Guid.NewGuid(), "Spesa", "cart", "#196D61");
        var expenses = new FakeExpenseRepository();
        expenses.Items.AddRange([
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(1050), new DateOnly(2026, 7, 20)),
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(1000), new DateOnly(2026, 6, 20))
        ]);

        var result = await CreateService(new DateOnly(2026, 7, 20), category, expenses).Get(Period.Month(2026, 7));

        var stable = Assert.Single(result.Value.Insights, x => x.Kind == InsightKind.Stable);
        Assert.Equal(InsightTarget.None, stable.Target);
    }

    [Fact]
    public async Task Should_write_amounts_and_shares_the_italian_way()
    {
        var food = new Category(Guid.NewGuid(), "Cibo", "food", "#196D61");
        var subscriptions = new Category(Guid.NewGuid(), "Abbonamenti", "tech", "#196D61");
        var expenses = new FakeExpenseRepository();
        expenses.Items.AddRange([
            new Expense(Guid.NewGuid(), food.CategoryId, new Money(200_000), new DateOnly(2026, 7, 2)),
            new Expense(Guid.NewGuid(), subscriptions.CategoryId, new Money(120_000), new DateOnly(2026, 7, 3)),
            new Expense(Guid.NewGuid(), subscriptions.CategoryId, new Money(10_000), new DateOnly(2026, 7, 4)),
            new Expense(Guid.NewGuid(), subscriptions.CategoryId, new Money(10_000), new DateOnly(2026, 7, 5))
        ]);

        var result = await CreateService(new DateOnly(2026, 7, 20), [food, subscriptions], expenses).Get(Period.Month(2026, 7));

        var descriptions = result.Value.Insights.ToDictionary(x => x.Kind, x => x.Description);
        Assert.Equal("Cibo è il 59% delle spese di luglio.", descriptions[InsightKind.TopCategory]);
        Assert.Equal("Hai registrato 3 spese in Abbonamenti a luglio; a giugno nessuna.", descriptions[InsightKind.NewHabit]);
        Assert.Equal($"1.200,00{Nbsp}€ il 3 luglio è molto più alta delle altre spese di Abbonamenti.",
            descriptions[InsightKind.UnusualExpense]);
    }

    private static async Task<(long Spent, long? Compared, DateOnly From, DateOnly To)> Summary(InsightsService service, Period period) =>
        Summarize((await service.Get(period)).Value);

    private static (long Spent, long? Compared, DateOnly From, DateOnly To) Summarize(InsightsOverview overview) =>
        (overview.SpentCents, overview.ComparedSpentCents, overview.From, overview.To);

    private static InsightsService CreateService(DateOnly today, Category? category = null, FakeExpenseRepository? expenses = null) =>
        CreateService(today, category is null ? [] : [category], expenses);

    private static InsightsService CreateService(DateOnly today, IReadOnlyList<Category> categories, FakeExpenseRepository? expenses = null)
    {
        var categoryRepository = new FakeCategoryRepository();
        categoryRepository.Items.AddRange(categories);
        return new InsightsService(categoryRepository, expenses ?? new FakeExpenseRepository(), () => today);
    }
}
