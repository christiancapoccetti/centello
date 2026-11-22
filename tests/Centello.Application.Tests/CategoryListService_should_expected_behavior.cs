using Centello.Domain;

namespace Centello.Application.Tests;

public sealed class CategoryListService_should_expected_behavior
{
    private const char Nbsp = ' ';
    private static readonly DateOnly Today = new(2026, 11, 19);
    private static readonly Error Failed = new("Test.Failed", "Operazione non riuscita.");

    [Fact]
    public async Task Should_list_the_categories_in_use_by_name_with_the_spending_of_this_month_and_the_budget()
    {
        var food = NewCategory("Spesa", budgetCents: 50_000);
        var home = NewCategory("Casa", budgetCents: 65_050);
        var other = NewCategory("Altro");
        var expenses = new[]
        {
            Spend(food, 40_000, Today),
            Spend(food, 1_230, new DateOnly(2026, 11, 1)),
            Spend(home, 38_000, new DateOnly(2026, 11, 30)),
            Spend(other, 9_999, new DateOnly(2026, 10, 31)),
            Spend(other, 9_999, new DateOnly(2026, 12, 1))
        };

        var list = await List([food, home, other], expenses);

        Assert.Equal(
        [
            ("Altro", $"Novembre: 0,00{Nbsp}€ · Nessun budget"),
            ("Casa", $"Novembre: 380,00{Nbsp}€ · budget 650,50{Nbsp}€"),
            ("Spesa", $"Novembre: 412,30{Nbsp}€ · budget 500{Nbsp}€")
        ], list.Active.Select(row => (row.Name, row.Detail)));
        Assert.Empty(list.Archived);
    }

    [Fact]
    public async Task Should_list_the_archived_categories_apart_by_name_without_their_spending()
    {
        var gym = NewCategory("Palestra", budgetCents: 4_000, archived: true);
        var gifts = NewCategory("Regali", archived: true);
        var arts = NewCategory("Arte", archived: true);
        var food = NewCategory("Spesa");

        var list = await List([gym, gifts, arts, food], [Spend(gym, 3_900, Today)]);

        Assert.Equal(["Spesa"], list.Active.Select(row => row.Name));
        Assert.Equal([("Arte", "Archiviata"), ("Palestra", "Archiviata"), ("Regali", "Archiviata")],
            list.Archived.Select(row => (row.Name, row.Detail)));
    }

    [Fact]
    public async Task Should_read_a_budget_of_zero_as_no_budget()
    {
        var other = NewCategory("Altro", budgetCents: 0);

        var list = await List([other]);

        Assert.Equal($"Novembre: 0,00{Nbsp}€ · Nessun budget", list.Active.Single().Detail);
    }

    [Fact]
    public async Task Should_carry_the_catalog_keys_and_fall_back_for_unknown_ones()
    {
        var known = new Category(Guid.NewGuid(), "Casa", "home", "peri");
        var legacy = new Category(Guid.NewGuid(), "Vecchia", "not-an-icon", "#123456");

        var list = await List([known, legacy]);

        Assert.Equal([(known.CategoryId, "home", "peri"), (legacy.CategoryId, "more", "grey")],
            list.Active.Select(row => (row.CategoryId, row.IconKey, row.ColorKey)));
    }

    [Fact]
    public async Task Should_say_a_row_to_a_screen_reader()
    {
        var list = await List([NewCategory("Altro")]);

        Assert.Equal($"Altro, Novembre: 0,00{Nbsp}€ · Nessun budget", list.Active.Single().Description);
    }

    [Fact]
    public async Task Should_mark_a_category_that_spent_more_than_its_monthly_budget()
    {
        var over = NewCategory("Spesa", budgetCents: 10_000);
        var exact = NewCategory("Casa", budgetCents: 10_000);
        var unbudgeted = NewCategory("Altro");
        var lastMonth = NewCategory("Svago", budgetCents: 10_000);
        var expenses = new[]
        {
            Spend(over, 10_001, Today),
            Spend(exact, 10_000, Today),
            Spend(unbudgeted, 99_999, Today),
            Spend(lastMonth, 99_999, new DateOnly(2026, 10, 31))
        };

        var list = await List([over, exact, unbudgeted, lastMonth], expenses);

        Assert.Equal([("Altro", false), ("Casa", false), ("Spesa", true), ("Svago", false)],
            list.Active.Select(row => (row.Name, row.IsOverBudget)));
    }

    [Fact]
    public async Task Should_say_to_a_screen_reader_that_a_row_is_over_its_budget()
    {
        var over = NewCategory("Spesa", budgetCents: 10_000);

        var list = await List([over], [Spend(over, 12_000, Today)]);

        Assert.Equal($"Spesa, Novembre: 120,00{Nbsp}€ · budget 100{Nbsp}€, oltre il budget", list.Active.Single().Description);
    }

    [Fact]
    public async Task Should_return_the_repository_failure()
    {
        var categoryFailure = await new CategoryListService(new FakeCategoryRepository { Failure = Failed },
            new FakeExpenseRepository(), () => Today).Get();
        var expenseFailure = await new CategoryListService(new FakeCategoryRepository(),
            new FakeExpenseRepository { Failure = Failed }, () => Today).Get();

        Assert.Equal(Failed, categoryFailure.Error);
        Assert.Equal(Failed, expenseFailure.Error);
    }

    private static async Task<CategoryList> List(IEnumerable<Category> categories, IEnumerable<Expense>? expenses = null)
    {
        var categoryRepository = new FakeCategoryRepository();
        categoryRepository.Items.AddRange(categories);
        var expenseRepository = new FakeExpenseRepository();
        expenseRepository.Items.AddRange(expenses ?? []);

        var result = await new CategoryListService(categoryRepository, expenseRepository, () => Today).Get();

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
