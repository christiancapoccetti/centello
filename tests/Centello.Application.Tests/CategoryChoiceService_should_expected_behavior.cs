using Centello.Domain;

namespace Centello.Application.Tests;

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

    [Fact]
    public async Task Should_offer_the_four_categories_used_most_in_the_last_thirty_days_as_shortcuts()
    {
        var home = NewCategory("Casa", "home", "sky");
        var food = NewCategory("Spesa", "cart", "mint");
        var leisure = NewCategory("Svago");
        var travel = NewCategory("Viaggi");
        var gifts = NewCategory("Regali");
        var expenses = new[]
        {
            Spend(food, 500, Today),
            Spend(food, 500, Today.AddDays(-2)),
            Spend(home, 500, Today.AddDays(-3)),
            Spend(leisure, 500, Today.AddDays(-4)),
            Spend(travel, 500, Today.AddDays(-5)),
            Spend(travel, 500, Today.AddDays(-6)),
            Spend(travel, 500, Today.AddDays(-7))
        };

        var shortcuts = await Shortcuts([home, food, leisure, travel, gifts], expenses);

        Assert.Equal(["Viaggi", "Spesa", "Casa", "Svago"], shortcuts.Select(shortcut => shortcut.Name));
        Assert.Equal(new CategoryShortcut(food.CategoryId, "Spesa", "cart", "mint"), shortcuts[1]);
        Assert.Equal(new CategoryShortcut(home.CategoryId, "Casa", "home", "sky"), shortcuts[2]);
    }

    [Fact]
    public async Task Should_count_only_the_thirty_days_that_end_today_for_the_shortcuts()
    {
        var recent = NewCategory("Spesa");
        var old = NewCategory("Viaggi");
        var expenses = new[]
        {
            Spend(recent, 100, Today.AddDays(-29)),
            Spend(old, 100, Today.AddDays(-30)),
            Spend(old, 100, Today.AddDays(-45)),
            Spend(old, 100, Today.AddDays(-60))
        };

        var shortcuts = await Shortcuts([old, recent], expenses);

        Assert.Equal(["Spesa", "Viaggi"], shortcuts.Select(shortcut => shortcut.Name));
    }

    [Fact]
    public async Task Should_rank_the_shortcuts_by_how_often_and_not_by_how_much()
    {
        var home = NewCategory("Casa");
        var coffee = NewCategory("Caffè");
        var expenses = new[]
        {
            Spend(home, 90_000, Today),
            Spend(coffee, 120, Today),
            Spend(coffee, 120, Today.AddDays(-1))
        };

        var shortcuts = await Shortcuts([home, coffee], expenses);

        Assert.Equal(["Caffè", "Casa"], shortcuts.Select(shortcut => shortcut.Name));
    }

    [Fact]
    public async Task Should_order_equal_counts_of_the_shortcuts_by_name()
    {
        var health = NewCategory("Salute");
        var other = NewCategory("Altro");
        var travel = NewCategory("Viaggi");

        var shortcuts = await Shortcuts([travel, health, other], [Spend(health, 700, Today), Spend(other, 90, Today)]);

        Assert.Equal(["Altro", "Salute", "Viaggi"], shortcuts.Select(shortcut => shortcut.Name));
    }

    [Fact]
    public async Task Should_fill_the_free_shortcuts_with_unused_categories_by_name()
    {
        var food = NewCategory("Spesa");
        var travel = NewCategory("Viaggi");
        var gifts = NewCategory("Regali");
        var home = NewCategory("Casa");
        var health = NewCategory("Salute");

        var shortcuts = await Shortcuts([travel, gifts, food, home, health], [Spend(travel, 500, Today.AddDays(-40)), Spend(food, 500, Today)]);

        Assert.Equal(["Spesa", "Casa", "Regali", "Salute"], shortcuts.Select(shortcut => shortcut.Name));
    }

    [Fact]
    public async Task Should_leave_archived_categories_out_of_the_shortcuts()
    {
        var gym = NewCategory("Palestra", archived: true);
        var food = NewCategory("Spesa");

        var shortcuts = await Shortcuts([gym, food], [Spend(gym, 9_000, Today), Spend(gym, 9_000, Today)]);

        Assert.Equal(["Spesa"], shortcuts.Select(shortcut => shortcut.Name));
    }

    [Fact]
    public async Task Should_offer_fewer_than_four_shortcuts_with_fewer_than_four_active_categories()
    {
        var food = NewCategory("Spesa");
        var home = NewCategory("Casa");

        var two = await Shortcuts([food, home]);
        var none = await Shortcuts([NewCategory("Palestra", archived: true)]);

        Assert.Equal(["Casa", "Spesa"], two.Select(shortcut => shortcut.Name));
        Assert.Empty(none);
    }

    [Fact]
    public async Task Should_resolve_unknown_keys_of_a_shortcut_to_the_fallback()
    {
        var odd = NewCategory("Strana", "no-such-icon", "no-such-color");

        var shortcut = Assert.Single(await Shortcuts([odd]));

        Assert.Equal(CategoryIcons.Resolve(null).Key, shortcut.IconKey);
        Assert.Equal(CategoryPalette.Resolve(null).Key, shortcut.ColorKey);
    }

    [Fact]
    public async Task Should_return_the_repository_failure_for_the_shortcuts()
    {
        var categoryFailure = await new CategoryChoiceService(new FakeCategoryRepository { Failure = Failed },
            new FakeExpenseRepository(), () => Today).Shortcuts();
        var expenseFailure = await new CategoryChoiceService(new FakeCategoryRepository(),
            new FakeExpenseRepository { Failure = Failed }, () => Today).Shortcuts();

        Assert.Equal(Failed, categoryFailure.Error);
        Assert.Equal(Failed, expenseFailure.Error);
    }

    private static async Task<IReadOnlyList<CategoryShortcut>> Shortcuts(IEnumerable<Category> categories,
        IEnumerable<Expense>? expenses = null)
    {
        var categoryRepository = new FakeCategoryRepository();
        categoryRepository.Items.AddRange(categories);
        var expenseRepository = new FakeExpenseRepository();
        expenseRepository.Items.AddRange(expenses ?? []);
        var service = new CategoryChoiceService(categoryRepository, expenseRepository, () => Today);

        var result = await service.Shortcuts();

        Assert.True(result.IsSuccess);
        return result.Value;
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

    private static Category NewCategory(string name, string icon = "cart", string color = "mint", bool archived = false)
    {
        var category = new Category(Guid.NewGuid(), name, icon, color);
        if (archived)
            category.Archive();

        return category;
    }

    private static Expense Spend(Category category, long cents, DateOnly on) =>
        new(Guid.NewGuid(), category.CategoryId, new Money(cents), on);
}
