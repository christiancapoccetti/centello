using Centesimo.Domain;

namespace Centesimo.Application.Tests;

public sealed class ExpenseHistoryService_should_expected_behavior
{
    private static readonly DateOnly Today = new(2026, 11, 19);
    private static readonly Error Failed = new("Test.Failed", "Operazione non riuscita.");

    [Fact]
    public async Task Should_title_the_period_and_total_what_is_listed()
    {
        var category = NewCategory("Spesa");
        var expenses = new[]
        {
            Spend(category, 12_050, Today),
            Spend(category, 1_000, new DateOnly(2026, 11, 2)),
            Spend(category, 9_999, new DateOnly(2026, 10, 31))
        };

        var month = await History(Period.Month(2026, 11), ExpenseFilter.All, [category], expenses);
        var year = await History(Period.Year(2026), ExpenseFilter.All, [category], expenses);

        Assert.Equal(("Novembre 2026", "130,50 €"), (month.Title, month.Total));
        Assert.Equal(("Anno 2026", "230,49 €"), (year.Title, year.Total));
    }

    [Fact]
    public async Task Should_group_a_month_by_day_newest_first_with_relative_labels()
    {
        var category = NewCategory("Spesa");
        var expenses = new[]
        {
            Spend(category, 1_000, new DateOnly(2026, 11, 2)),
            Spend(category, 2_000, Today),
            Spend(category, 3_000, new DateOnly(2026, 11, 18)),
            Spend(category, 500, Today),
            Spend(category, 4_000, new DateOnly(2026, 11, 17))
        };

        var history = await History(Period.Month(2026, 11), ExpenseFilter.All, [category], expenses);

        Assert.Equal(["Oggi", "Ieri", "mar 17 nov", "lun 2 nov"], history.Groups.Select(group => group.Label));
        Assert.Equal(["25,00 €", "30,00 €", "40,00 €", "10,00 €"], history.Groups.Select(group => group.Total));
        Assert.Equal([2, 1, 1, 1], history.Groups.Select(group => group.Rows.Count));
    }

    [Fact]
    public async Task Should_group_a_year_by_month_newest_first()
    {
        var category = NewCategory("Spesa");
        var expenses = new[]
        {
            Spend(category, 1_000, new DateOnly(2026, 1, 31)),
            Spend(category, 2_000, Today),
            Spend(category, 3_000, new DateOnly(2026, 10, 1)),
            Spend(category, 500, new DateOnly(2026, 11, 3))
        };

        var history = await History(Period.Year(2026), ExpenseFilter.All, [category], expenses);

        Assert.Equal(["Novembre", "Ottobre", "Gennaio"], history.Groups.Select(group => group.Label));
        Assert.Equal(["25,00 €", "30,00 €", "10,00 €"], history.Groups.Select(group => group.Total));
    }

    [Fact]
    public async Task Should_order_rows_of_a_day_by_expense_id_newest_first()
    {
        var category = NewCategory("Spesa");
        var first = new Guid("00000000-0000-7000-8000-000000000001");
        var second = new Guid("00000000-0000-7000-8000-000000000002");
        var expenses = new[]
        {
            Spend(category, 100, Today, expenseId: first),
            Spend(category, 200, Today, expenseId: second)
        };

        var history = await History(Period.Month(2026, 11), ExpenseFilter.All, [category], expenses);

        Assert.Equal([second, first], history.Groups.Single().Rows.Select(row => row.ExpenseId));
    }

    [Fact]
    public async Task Should_describe_a_row_with_note_category_tag_and_automatic_mark()
    {
        var food = NewCategory("Ristoranti", color: "mint", icon: "food");
        var pizza = new Tag(Guid.NewGuid(), food.CategoryId, "Pizza");
        var expenses = new[]
        {
            Spend(food, 1_250, Today, note: "Da Mario", tagId: pizza.TagId),
            Spend(food, 4_000, new DateOnly(2026, 11, 18)),
            Spend(food, 1_599, new DateOnly(2026, 11, 17), tagId: pizza.TagId, recurringPaymentId: Guid.NewGuid())
        };

        var history = await History(Period.Month(2026, 11), ExpenseFilter.All, [food], expenses, [pizza]);

        var rows = history.Groups.SelectMany(group => group.Rows).ToList();
        Assert.Equal(["Da Mario", "Ristoranti", "Ristoranti"], rows.Select(row => row.Title));
        Assert.Equal(["Ristoranti · Pizza", "Ristoranti", "Ristoranti · Pizza · automatica"], rows.Select(row => row.Detail));
        Assert.Equal(["12,50 €", "40,00 €", "15,99 €"], rows.Select(row => row.Amount));
        Assert.All(rows, row => Assert.Equal(("food", "mint"), (row.IconKey, row.ColorKey)));
        Assert.Equal("Da Mario, Ristoranti · Pizza, 12,50 €", rows[0].Description);
    }

    [Fact]
    public async Task Should_keep_expenses_of_archived_and_missing_categories_and_tags()
    {
        var archived = NewCategory("Vecchia", color: "coral", icon: "home", archived: true);
        var oldTag = new Tag(Guid.NewGuid(), archived.CategoryId, "Affitto");
        oldTag.Archive();
        var orphan = new Expense(Guid.NewGuid(), Guid.NewGuid(), new Money(500), Today);
        var expenses = new[] { Spend(archived, 700, new DateOnly(2026, 11, 18), tagId: oldTag.TagId), orphan };

        var history = await History(Period.Month(2026, 11), ExpenseFilter.All, [archived], expenses, [oldTag]);

        var rows = history.Groups.SelectMany(group => group.Rows).ToList();
        Assert.Equal(new HistoryRow(orphan.ExpenseId, "Categoria non disponibile", "Categoria non disponibile",
            "5,00 €", "more", "grey"), rows[0]);
        Assert.Equal(("Vecchia · Affitto", "home", "coral"), (rows[1].Detail, rows[1].IconKey, rows[1].ColorKey));
    }

    [Fact]
    public async Task Should_list_only_the_selected_category_but_keep_every_chip()
    {
        var food = NewCategory("Ristoranti");
        var home = NewCategory("Casa");
        var expenses = new[] { Spend(food, 1_000, Today), Spend(home, 2_000, Today), Spend(food, 500, Today) };

        var history = await History(Period.Month(2026, 11), ExpenseFilter.ForCategory(food.CategoryId), [food, home],
            expenses);

        Assert.Equal(["10,00 €", "5,00 €"], history.Groups.Single().Rows.Select(row => row.Amount).Order(StringComparer.Ordinal));
        Assert.Equal("15,00 €", history.Total);
        Assert.Equal(["Casa", "Ristoranti"], history.CategoryChips.Select(chip => chip.Name));
    }

    [Fact]
    public async Task Should_filter_a_category_by_tag()
    {
        var food = NewCategory("Ristoranti");
        var pizza = new Tag(Guid.NewGuid(), food.CategoryId, "Pizza");
        var expenses = new[]
        {
            Spend(food, 1_000, Today, tagId: pizza.TagId),
            Spend(food, 2_000, Today),
            Spend(food, 3_000, Today, tagId: Guid.NewGuid())
        };

        var all = await History(Period.Month(2026, 11), ExpenseFilter.ForCategory(food.CategoryId), [food], expenses, [pizza]);
        var tagged = await History(Period.Month(2026, 11),
            ExpenseFilter.ForCategory(food.CategoryId, TagFilter.Tag(pizza.TagId)), [food], expenses, [pizza]);
        var untagged = await History(Period.Month(2026, 11),
            ExpenseFilter.ForCategory(food.CategoryId, TagFilter.Untagged), [food], expenses, [pizza]);

        Assert.Equal("60,00 €", all.Total);
        Assert.Equal("10,00 €", tagged.Total);
        Assert.Equal("20,00 €", untagged.Total);
    }

    [Fact]
    public async Task Should_chip_active_categories_with_spending_largest_first_then_by_name()
    {
        var food = NewCategory("Ristoranti", color: "mint");
        var home = NewCategory("Casa", color: "peri");
        var fuel = NewCategory("Auto", color: "butter");
        var idle = NewCategory("Svago");
        var archived = NewCategory("Vecchia", archived: true);
        var expenses = new[]
        {
            Spend(food, 5_000, Today),
            Spend(home, 2_000, Today),
            Spend(fuel, 2_000, Today),
            Spend(archived, 9_000, Today),
            Spend(home, 9_999, new DateOnly(2026, 10, 31))
        };

        var history = await History(Period.Month(2026, 11), ExpenseFilter.All, [food, home, fuel, idle, archived], expenses);

        Assert.Equal(["Ristoranti", "Auto", "Casa"], history.CategoryChips.Select(chip => chip.Name));
        Assert.Equal(["mint", "butter", "peri"], history.CategoryChips.Select(chip => chip.ColorKey));
        Assert.All(history.CategoryChips, chip => Assert.False(chip.IsSelected));
    }

    [Fact]
    public async Task Should_chip_the_selected_category_even_without_spending_or_archived()
    {
        var food = NewCategory("Ristoranti");
        var idle = NewCategory("Svago");
        var archived = NewCategory("Vecchia", archived: true);
        var expenses = new[] { Spend(food, 5_000, Today) };

        var idleHistory = await History(Period.Month(2026, 11), ExpenseFilter.ForCategory(idle.CategoryId),
            [food, idle, archived], expenses);
        var archivedHistory = await History(Period.Month(2026, 11), ExpenseFilter.ForCategory(archived.CategoryId),
            [food, idle, archived], expenses);

        Assert.Equal([("Ristoranti", false), ("Svago", true)], idleHistory.CategoryChips.Select(chip => (chip.Name, chip.IsSelected)));
        Assert.Equal([("Ristoranti", false), ("Vecchia", true)], archivedHistory.CategoryChips.Select(chip => (chip.Name, chip.IsSelected)));
    }

    [Fact]
    public async Task Should_say_what_is_missing_when_the_month_or_year_is_empty()
    {
        var food = NewCategory("Ristoranti");

        var month = await History(Period.Month(2026, 11), ExpenseFilter.All, [food]);
        var monthCategory = await History(Period.Month(2026, 11), ExpenseFilter.ForCategory(food.CategoryId), [food]);
        var year = await History(Period.Year(2026), ExpenseFilter.All, [food]);
        var yearCategory = await History(Period.Year(2026), ExpenseFilter.ForCategory(food.CategoryId), [food]);

        Assert.Equal("Nessuna spesa a novembre", month.EmptyText);
        Assert.Equal("Nessuna spesa in Ristoranti a novembre", monthCategory.EmptyText);
        Assert.Equal("Nessuna spesa nel 2026", year.EmptyText);
        Assert.Equal("Nessuna spesa in Ristoranti nel 2026", yearCategory.EmptyText);
        Assert.False(month.HasGroups);
        Assert.True(month.HasNoGroups);
    }

    [Theory]
    [InlineData(4, "Nessuna spesa ad aprile")]
    [InlineData(8, "Nessuna spesa ad agosto")]
    [InlineData(10, "Nessuna spesa a ottobre")]
    [InlineData(12, "Nessuna spesa a dicembre")]
    public async Task Should_use_ad_before_a_month_that_starts_with_a(int month, string expected)
    {
        var history = await History(Period.Month(2026, month), ExpenseFilter.All, []);

        Assert.Equal(expected, history.EmptyText);
    }

    [Fact]
    public async Task Should_name_the_tag_filter_in_the_empty_text()
    {
        var food = NewCategory("Ristoranti");
        var pizza = new Tag(Guid.NewGuid(), food.CategoryId, "Pizza");

        var tagged = await History(Period.Month(2026, 11),
            ExpenseFilter.ForCategory(food.CategoryId, TagFilter.Tag(pizza.TagId)), [food], tags: [pizza]);
        var untagged = await History(Period.Month(2026, 11),
            ExpenseFilter.ForCategory(food.CategoryId, TagFilter.Untagged), [food], tags: [pizza]);

        Assert.Equal("Nessuna spesa in Ristoranti · Pizza a novembre", tagged.EmptyText);
        Assert.Equal("Nessuna spesa in Ristoranti · Senza tag a novembre", untagged.EmptyText);
    }

    [Fact]
    public async Task Should_name_a_missing_selected_category_in_the_empty_text()
    {
        var history = await History(Period.Month(2026, 11), ExpenseFilter.ForCategory(Guid.NewGuid()), []);

        Assert.Equal("Nessuna spesa in Categoria non disponibile a novembre", history.EmptyText);
        Assert.Empty(history.CategoryChips);
    }

    [Fact]
    public async Task Should_follow_the_clock_for_the_relative_labels()
    {
        var category = NewCategory("Spesa");
        var expenses = new[] { Spend(category, 100, Today), Spend(category, 100, new DateOnly(2026, 11, 18)) };

        var history = await History(Period.Month(2026, 11), ExpenseFilter.All, [category], expenses,
            today: new DateOnly(2026, 11, 20));

        Assert.Equal(["Ieri", "mer 18 nov"], history.Groups.Select(group => group.Label));
    }

    [Fact]
    public async Task Should_return_the_repository_failure()
    {
        var failingCategories = new FakeCategoryRepository { Failure = Failed };
        var failingTags = new FakeTagRepository { Failure = Failed };
        var failingExpenses = new FakeExpenseRepository { Failure = Failed };

        var categoryFailure = await new ExpenseHistoryService(failingCategories, new FakeTagRepository(),
            new FakeExpenseRepository(), () => Today).Get(Period.Month(2026, 11), ExpenseFilter.All);
        var tagFailure = await new ExpenseHistoryService(new FakeCategoryRepository(), failingTags,
            new FakeExpenseRepository(), () => Today).Get(Period.Month(2026, 11), ExpenseFilter.All);
        var expenseFailure = await new ExpenseHistoryService(new FakeCategoryRepository(), new FakeTagRepository(),
            failingExpenses, () => Today).Get(Period.Month(2026, 11), ExpenseFilter.All);

        Assert.Equal(Failed, categoryFailure.Error);
        Assert.Equal(Failed, tagFailure.Error);
        Assert.Equal(Failed, expenseFailure.Error);
    }

    private static async Task<ExpenseHistory> History(Period period, ExpenseFilter filter,
        IEnumerable<Category> categories, IEnumerable<Expense>? expenses = null, IEnumerable<Tag>? tags = null,
        DateOnly? today = null)
    {
        var categoryRepository = new FakeCategoryRepository();
        categoryRepository.Items.AddRange(categories);
        var tagRepository = new FakeTagRepository();
        tagRepository.Items.AddRange(tags ?? []);
        var expenseRepository = new FakeExpenseRepository();
        expenseRepository.Items.AddRange(expenses ?? []);
        var service = new ExpenseHistoryService(categoryRepository, tagRepository, expenseRepository, () => today ?? Today);

        var result = await service.Get(period, filter);

        Assert.True(result.IsSuccess);
        return result.Value;
    }

    private static Category NewCategory(string name, bool archived = false, string color = "mint", string icon = "cart")
    {
        var category = new Category(Guid.NewGuid(), name, icon, color);
        if (archived)
            category.Archive();

        return category;
    }

    private static Expense Spend(Category category, long cents, DateOnly on, string note = "", Guid? tagId = null,
        Guid? recurringPaymentId = null, Guid? expenseId = null) =>
        new(expenseId ?? Guid.NewGuid(), category.CategoryId, new Money(cents), on, tagId, note,
            recurringPaymentId: recurringPaymentId);
}
