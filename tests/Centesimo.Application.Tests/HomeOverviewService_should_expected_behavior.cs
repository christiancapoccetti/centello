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

    private static Category NewCategory(string name, long? budgetCents = null, bool archived = false)
    {
        var category = new Category(Guid.NewGuid(), name, "cart", "mint", budgetCents.HasValue ? new Money(budgetCents.Value) : null);
        if (archived)
            category.Archive();

        return category;
    }

    private static Expense Spend(Category category, long cents, DateOnly on) =>
        new(Guid.NewGuid(), category.CategoryId, new Money(cents), on);
}
