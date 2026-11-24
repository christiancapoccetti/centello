using Centello.Domain;

namespace Centello.Application.Tests;

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
        Assert.Equal("Nessuna spesa nel periodo selezionato.", result.Value.Composition.Statement.Lead);
    }

    [Fact]
    public async Task Should_keep_previous_only_categories_in_the_full_comparison()
    {
        var category = new Category(Guid.NewGuid(), "Spesa", "cart", "#196D61");
        var expenses = new FakeExpenseRepository();
        expenses.Items.Add(new Expense(Guid.NewGuid(), category.CategoryId, new Money(1000), new DateOnly(2026, 6, 20)));

        var result = await CreateService(new DateOnly(2026, 7, 20), category, expenses).Get(Period.Month(2026, 7));

        var row = Assert.Single(result.Value.Categories);
        Assert.Equal(category.CategoryId, row.CategoryId);
        Assert.Equal((0L, 1000L), (row.SpentCents, row.ComparedSpentCents));
        Assert.Null(row.Share);
        Assert.Equal(1d, row.ComparedShare);
        Assert.Equal(-1_000L, row.DeltaCents);
        Assert.Equal("Nessuna spesa nel periodo selezionato.", result.Value.Composition.Statement.Lead);
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
    }

    [Fact]
    public async Task Should_summarize_the_difference_in_euros()
    {
        var category = new Category(Guid.NewGuid(), "Spesa", "cart", "#196D61");
        var expenses = new FakeExpenseRepository();
        expenses.Items.AddRange([
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(128_460), new DateOnly(2026, 11, 19)),
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(136_660), new DateOnly(2026, 10, 19)),
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(22_340), new DateOnly(2026, 10, 25)),
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(150_000), new DateOnly(2026, 9, 12))
        ]);
        var service = CreateService(new DateOnly(2026, 11, 19), category, expenses);

        var current = (await service.Get(Period.Month(2026, 11))).Value;
        var past = (await service.Get(Period.Month(2026, 10))).Value;

        Assert.Equal(new BudgetSummary("Fino a oggi hai speso ", $"82,00{Nbsp}€ in meno", " degli stessi giorni di ottobre."),
            current.Summary);
        Assert.Equal(new BudgetSummary("A ottobre hai speso ", $"90,00{Nbsp}€ in più", " di settembre."), past.Summary);
    }

    [Fact]
    public async Task Should_not_compare_when_previous_equivalent_period_has_no_expenses()
    {
        var category = new Category(Guid.NewGuid(), "Spesa", "cart", "#196D61");
        var expenses = new FakeExpenseRepository();
        expenses.Items.Add(new Expense(Guid.NewGuid(), category.CategoryId, new Money(1000), new DateOnly(2026, 7, 20)));

        var result = await CreateService(new DateOnly(2026, 7, 20), category, expenses).Get(Period.Month(2026, 7));

        Assert.Null(result.Value.ComparedSpentCents);
        var row = Assert.Single(result.Value.Categories);
        Assert.Null(row.ComparedSpentCents);
        Assert.Null(row.ComparedShare);
        Assert.Null(row.DeltaCents);
        Assert.Equal("", result.Value.Composition.Context);
        Assert.Null(result.Value.Pace.Gap);
    }

    [Fact]
    public async Task Should_reproduce_the_mockup_month_from_the_fixture()
    {
        var fixture = InsightFixtures.November();

        var november = await fixture.Overview();
        var october = await fixture.Overview(Period.Month(2026, 10));

        Assert.Equal((InsightFixtures.SpentCents, InsightFixtures.ComparedCents), (november.SpentCents, november.ComparedSpentCents));
        Assert.Equal(InsightFixtures.PreviousClosedCents, october.SpentCents);
        Assert.Equal(
            ["Spesa 41230 39850", "Casa 38000 38000", "Trasporti 14620 20560", "Ristoranti 12840 9620",
             "Svago 9610 14230", "Salute 6120 8800", "Altro 6040 5600"],
            november.Categories.Select(x => $"{x.Name} {x.SpentCents} {x.ComparedSpentCents}"));
        Assert.Equal(InsightFixtures.BudgetCents, fixture.Categories.Items.Sum(x => x.MonthlyBudget!.Value.Cents));
        Assert.Equal(InsightFixtures.AutomaticCents,
            fixture.Expenses.Items.Where(x => x.IsAutomatic && x.OccurredOn.Month == 11).Sum(x => x.Amount.Cents));
        Assert.Equal(InsightFixtures.NovemberDays, SpentPerDay(fixture, 11, 19));
        Assert.Equal(InsightFixtures.OctoberDays, SpentPerDay(fixture, 10, 31));
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
        Assert.Equal(new DateOnly(2025, 7, 20), result.Value.ComparedTo);
        var row = Assert.Single(result.Value.Categories);
        Assert.Equal((1_200L, 1_000L, 200L), (row.SpentCents, row.ComparedSpentCents, row.DeltaCents));
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
    public async Task Should_reconcile_full_category_rows_with_the_window_totals_and_exact_changes()
    {
        var food = new Category(Guid.NewGuid(), "Alimentari", "cart", "mint");
        var fuel = new Category(Guid.NewGuid(), "Carburante", "fuel", "peri");
        var bills = new Category(Guid.NewGuid(), "Bollette", "bills", "lilac");
        var restaurants = new Category(Guid.NewGuid(), "Ristoranti", "food", "butter");
        var archived = new Category(Guid.NewGuid(), "Archivio", "more", "grey");
        archived.Archive();
        var current = new DateOnly(2026, 11, 24);
        var prior = current.AddMonths(-1);
        var expenses = new FakeExpenseRepository();
        expenses.Items.AddRange([
            new Expense(Guid.NewGuid(), food.CategoryId, new Money(56_001), current),
            new Expense(Guid.NewGuid(), fuel.CategoryId, new Money(13_500), current),
            new Expense(Guid.NewGuid(), bills.CategoryId, new Money(9_200), current),
            new Expense(Guid.NewGuid(), archived.CategoryId, new Money(6_800), current),
            new Expense(Guid.NewGuid(), food.CategoryId, new Money(40_000), prior),
            new Expense(Guid.NewGuid(), fuel.CategoryId, new Money(16_000), prior),
            new Expense(Guid.NewGuid(), bills.CategoryId, new Money(12_000), prior),
            new Expense(Guid.NewGuid(), restaurants.CategoryId, new Money(10_000), prior),
            new Expense(Guid.NewGuid(), Guid.NewGuid(), new Money(2_000), prior),
            new Expense(Guid.NewGuid(), food.CategoryId, new Money(9_999), prior.AddDays(1)),
            new Expense(Guid.NewGuid(), food.CategoryId, new Money(9_999), current.AddDays(1))
        ]);
        var service = CreateService(current, [food, fuel, bills, restaurants, archived], expenses);

        var overview = (await service.Get()).Value;

        Assert.Equal(85_501, overview.SpentCents);
        Assert.Equal(80_000, overview.ComparedSpentCents);
        Assert.Equal((new DateOnly(2026, 10, 1), prior), (overview.ComparedFrom, overview.ComparedTo));
        Assert.Equal<Guid?>([food.CategoryId, fuel.CategoryId, bills.CategoryId, null, restaurants.CategoryId],
            overview.Categories.Select(row => row.CategoryId));
        Assert.Equal($"2{Nbsp}€ su 3", overview.Composition.Statement.Emphasis);
        Assert.Equal("Negli stessi giorni di ottobre, la quota era la metà.", overview.Composition.Context);
        Assert.Equal(0.655, overview.Categories[0].Share!.Value, 3);
        Assert.Equal(0.5, overview.Categories[0].ComparedShare);
        Assert.Equal(16_001, overview.Categories[0].DeltaCents);
        var residual = Assert.Single(overview.Categories, row => row.CategoryId is null);
        Assert.Equal((6_800L, 2_000L, 4_800L), (residual.SpentCents, residual.ComparedSpentCents, residual.DeltaCents));
        var previousOnly = Assert.Single(overview.Categories, row => row.CategoryId == restaurants.CategoryId);
        Assert.Equal((0L, 10_000L, -10_000L), (previousOnly.SpentCents, previousOnly.ComparedSpentCents, previousOnly.DeltaCents));
        Assert.Equal(1, overview.Categories.Sum(row => row.Share!.Value), 10);
        Assert.Equal(1, overview.Categories.Sum(row => row.ComparedShare!.Value), 10);
        Assert.Equal(85_501, overview.Categories.Sum(row => row.SpentCents));
        Assert.Equal(80_000, overview.Categories.Sum(row => row.ComparedSpentCents));
        Assert.Equal(5_501, overview.Categories.Sum(row => row.DeltaCents));
    }

    [Fact]
    public async Task Should_handle_equal_leading_spend_and_keep_zero_as_a_comparable_category_share()
    {
        var food = new Category(Guid.NewGuid(), "Alimentari", "cart", "mint");
        var fuel = new Category(Guid.NewGuid(), "Carburante", "fuel", "peri");
        var today = new DateOnly(2026, 11, 24);
        var expenses = new FakeExpenseRepository();
        expenses.Items.AddRange([
            new Expense(Guid.NewGuid(), fuel.CategoryId, new Money(1_000), today),
            new Expense(Guid.NewGuid(), food.CategoryId, new Money(1_000), today),
            new Expense(Guid.NewGuid(), fuel.CategoryId, new Money(2_000), today.AddMonths(-1))
        ]);

        var overview = (await CreateService(today, [fuel, food], expenses).Get()).Value;

        Assert.Equal<Guid?>([food.CategoryId, fuel.CategoryId], overview.Categories.Select(row => row.CategoryId));
        Assert.Equal("Le voci con la spesa più alta hanno lo stesso importo.", overview.Composition.Statement.Lead);
        Assert.Equal("", overview.Composition.Context);
        Assert.Equal(0d, overview.Categories[0].ComparedShare);
        Assert.Equal(0L, overview.Categories[0].ComparedSpentCents);
        Assert.Equal([1_000L, -1_000L], overview.Categories.Select(row => row.DeltaCents));
    }

    [Fact]
    public async Task Should_show_a_composition_fallback_for_unknown_spending()
    {
        var today = new DateOnly(2026, 11, 24);
        var expenses = new FakeExpenseRepository();
        expenses.Items.Add(new Expense(Guid.NewGuid(), Guid.NewGuid(), new Money(1_000), today));

        var overview = (await CreateService(today, expenses: expenses).Get()).Value;

        var row = Assert.Single(overview.Categories);
        Assert.Null(row.CategoryId);
        Assert.Equal("100%", overview.Composition.Statement.Emphasis);
        Assert.Contains("categorie archiviate o non disponibili", overview.Composition.Statement.Tail);
    }

    private static IReadOnlyList<long> SpentPerDay(InsightFixtures fixture, int month, int days) => Enumerable
        .Range(1, days)
        .Select(day => fixture.Expenses.Items
            .Where(expense => expense.OccurredOn == new DateOnly(2026, month, day))
            .Sum(expense => expense.Amount.Cents))
        .ToList();

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

    [Fact]
    public async Task Should_offer_the_recap_of_a_closed_month_with_enough_expenses()
    {
        var overview = await Recap(Period.Month(2026, 10), SpendOnDays(new DateOnly(2026, 10, 1), 12, 1));

        Assert.Equal(new InsightRecap(Period.Month(2026, 10), "Rivedi il tuo ottobre", "Il riepilogo del mese, in poche schede"),
            overview.Recap);
    }

    [Fact]
    public async Task Should_not_offer_the_recap_of_the_month_of_today()
    {
        var overview = await Recap(Period.Month(2026, 11), SpendOnDays(new DateOnly(2026, 11, 1), 12, 1));

        Assert.Null(overview.Recap);
    }

    [Fact]
    public async Task Should_not_offer_the_recap_of_a_thin_month()
    {
        var overview = await Recap(Period.Month(2026, 10), SpendOnDays(new DateOnly(2026, 10, 1), 4, 1));

        Assert.Null(overview.Recap);
    }

    [Fact]
    public async Task Should_offer_the_recap_of_a_closed_year()
    {
        var overview = await Recap(Period.Year(2025), SpendOnDays(new DateOnly(2025, 1, 5), 36, 10));

        Assert.Equal(new InsightRecap(Period.Year(2025), "Rivedi il tuo 2025", "Il riepilogo dell'anno, in poche schede"),
            overview.Recap);
    }

    [Fact]
    public async Task Should_offer_the_year_so_far_up_to_yesterday()
    {
        var overview = await Recap(Period.Year(2026), SpendOnDays(new DateOnly(2026, 1, 5), 36, 10), new DateOnly(2026, 12, 31));

        Assert.Equal(new InsightRecap(Period.Year(2026), "Il tuo 2026 finora", "Fino al 30 dicembre, in poche schede"),
            overview.Recap);
    }

    [Fact]
    public async Task Should_not_offer_the_year_before_it_opens()
    {
        var overview = await Recap(Period.Year(2026), SpendOnDays(new DateOnly(2026, 1, 5), 36, 10), new DateOnly(2026, 12, 29));

        Assert.Null(overview.Recap);
    }

    [Fact]
    public async Task Should_count_the_year_so_far_only_up_to_yesterday()
    {
        var spending = SpendOnDays(new DateOnly(2026, 1, 5), 29, 10)
            .Append(new Expense(Guid.NewGuid(), RecapCategory.CategoryId, new Money(1000), new DateOnly(2026, 12, 31)));

        var overview = await Recap(Period.Year(2026), spending, new DateOnly(2026, 12, 31));

        Assert.Null(overview.Recap);
    }

    // A fixed category and one expense every `step` days from `first`: `count` expenses.
    private static IReadOnlyList<Expense> SpendOnDays(DateOnly first, int count, int step) => Enumerable
        .Range(0, count)
        .Select(index => new Expense(Guid.NewGuid(), RecapCategory.CategoryId, new Money(1000), first.AddDays(index * step)))
        .ToList();

    private static readonly Category RecapCategory = new(Guid.NewGuid(), "Spesa", "cart", "#196D61");

    private static async Task<InsightsOverview> Recap(Period period, IEnumerable<Expense> spending, DateOnly? today = null)
    {
        var expenses = new FakeExpenseRepository();
        expenses.Items.AddRange(spending);
        return (await CreateService(today ?? new DateOnly(2026, 11, 21), RecapCategory, expenses).Get(period)).Value;
    }
}
