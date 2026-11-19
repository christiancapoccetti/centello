using Centesimo.Domain;

namespace Centesimo.Application.Tests;

public sealed class CategoryChoiceService_should_expected_behavior
{
    private static readonly DateOnly Today = new(2026, 11, 19);
    private static readonly Error Failed = new("Test.Failed", "Operazione non riuscita.");

    [Fact]
    public async Task Should_offer_the_categories_with_more_expenses_in_the_last_ninety_days_first()
    {
        var home = NewCategory("Casa");
        var food = NewCategory("Spesa");
        var leisure = NewCategory("Svago");
        var expenses = new[]
        {
            Spend(leisure, 2_000, Today),
            Spend(food, 500, new DateOnly(2026, 11, 3)),
            Spend(food, 400, new DateOnly(2026, 10, 1)),
            Spend(food, 300, new DateOnly(2026, 9, 12)),
            Spend(home, 3_000, new DateOnly(2026, 11, 10)),
            Spend(home, 3_000, new DateOnly(2026, 10, 10))
        };

        var offered = await Offer([home, food, leisure], expenses);

        Assert.Equal(["Spesa", "Casa", "Svago"], offered.Select(category => category.Name));
    }

    [Fact]
    public async Task Should_count_the_expenses_and_not_what_they_cost()
    {
        var home = NewCategory("Casa");
        var coffee = NewCategory("Caffè");
        var expenses = new[]
        {
            Spend(home, 90_000, Today),
            Spend(coffee, 120, Today),
            Spend(coffee, 120, Today.AddDays(-1))
        };

        var offered = await Offer([home, coffee], expenses);

        Assert.Equal(["Caffè", "Casa"], offered.Select(category => category.Name));
    }

    [Fact]
    public async Task Should_count_only_the_ninety_days_that_end_today()
    {
        var travel = NewCategory("Viaggi");
        var other = NewCategory("Altro");
        var food = NewCategory("Spesa");
        var expenses = new[]
        {
            Spend(food, 100, Today.AddDays(-89)),
            Spend(travel, 100, Today.AddDays(-90)),
            Spend(travel, 100, Today.AddDays(-120)),
            Spend(other, 100, Today.AddDays(1)),
            Spend(other, 100, Today.AddDays(2))
        };

        var offered = await Offer([travel, other, food], expenses);

        Assert.Equal(["Spesa", "Altro", "Viaggi"], offered.Select(category => category.Name));
    }

    [Fact]
    public async Task Should_order_equal_counts_by_name()
    {
        var health = NewCategory("Salute");
        var other = NewCategory("Altro");
        var travel = NewCategory("Viaggi");
        var gifts = NewCategory("Regali");

        var offered = await Offer([travel, health, gifts, other], [Spend(health, 700, Today), Spend(other, 90, Today)]);

        Assert.Equal(["Altro", "Salute", "Regali", "Viaggi"], offered.Select(category => category.Name));
    }

    [Fact]
    public async Task Should_leave_archived_categories_out()
    {
        var gym = NewCategory("Palestra", archived: true);
        var food = NewCategory("Spesa");

        var offered = await Offer([gym, food], [Spend(gym, 9_000, Today)]);

        Assert.Equal(["Spesa"], offered.Select(category => category.Name));
    }

    [Fact]
    public async Task Should_also_offer_the_requested_category_last_even_when_it_is_archived()
    {
        var gym = NewCategory("Palestra", archived: true);
        var gifts = NewCategory("Regali", archived: true);
        var food = NewCategory("Spesa");

        var offered = await Offer([gym, gifts, food], [Spend(gym, 9_000, Today)], alsoOffer: gym.CategoryId);

        Assert.Equal(["Spesa", "Palestra"], offered.Select(category => category.Name));
    }

    [Fact]
    public async Task Should_not_repeat_a_requested_category_that_is_active()
    {
        var food = NewCategory("Spesa");
        var home = NewCategory("Casa");

        var offered = await Offer([food, home], alsoOffer: food.CategoryId);

        Assert.Equal(["Casa", "Spesa"], offered.Select(category => category.Name));
    }

    [Fact]
    public async Task Should_return_the_repository_failure()
    {
        var categoryFailure = await new CategoryChoiceService(new FakeCategoryRepository { Failure = Failed },
            new FakeExpenseRepository(), () => Today).Get();
        var expenseFailure = await new CategoryChoiceService(new FakeCategoryRepository(),
            new FakeExpenseRepository { Failure = Failed }, () => Today).Get();

        Assert.Equal(Failed, categoryFailure.Error);
        Assert.Equal(Failed, expenseFailure.Error);
    }

    private static async Task<IReadOnlyList<Category>> Offer(IEnumerable<Category> categories,
        IEnumerable<Expense>? expenses = null, Guid? alsoOffer = null)
    {
        var categoryRepository = new FakeCategoryRepository();
        categoryRepository.Items.AddRange(categories);
        var expenseRepository = new FakeExpenseRepository();
        expenseRepository.Items.AddRange(expenses ?? []);
        var service = new CategoryChoiceService(categoryRepository, expenseRepository, () => Today);

        var result = await service.Get(alsoOffer);

        Assert.True(result.IsSuccess);
        return result.Value;
    }

    private static Category NewCategory(string name, bool archived = false)
    {
        var category = new Category(Guid.NewGuid(), name, "cart", "mint");
        if (archived)
            category.Archive();

        return category;
    }

    private static Expense Spend(Category category, long cents, DateOnly on) =>
        new(Guid.NewGuid(), category.CategoryId, new Money(cents), on);
}
