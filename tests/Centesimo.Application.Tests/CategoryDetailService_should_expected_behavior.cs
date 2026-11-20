using Centesimo.Domain;

namespace Centesimo.Application.Tests;

public sealed class CategoryDetailService_should_expected_behavior
{
    private static readonly DateOnly Today = new(2026, 11, 19);
    private static readonly Error Failed = new("Test.Failed", "Operazione non riuscita.");

    [Fact]
    public async Task Should_fail_when_the_category_does_not_exist()
    {
        var service = Service(new FakeCategoryRepository(), new FakeTagRepository(), new FakeExpenseRepository());

        var result = await service.Get(Guid.NewGuid(), Period.Month(2026, 11), TagFilter.All);

        Assert.Equal("Category.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Should_describe_the_category_and_total_its_spending_for_the_period()
    {
        var category = NewCategory("Ristoranti", color: "mint", icon: "food");
        var expenses = new[]
        {
            Spend(category, 12_050, Today),
            Spend(category, 1_000, new DateOnly(2026, 11, 2)),
            Spend(category, 9_999, new DateOnly(2026, 10, 31)),
            Spend(NewCategory("Casa"), 5_000, Today)
        };

        var detail = await Detail(category, Period.Month(2026, 11), expenses: expenses);

        Assert.Equal(category.CategoryId, detail.CategoryId);
        Assert.Equal(("Ristoranti", "food", "mint"), (detail.Name, detail.IconKey, detail.ColorKey));
        Assert.Equal(Period.Month(2026, 11), detail.Period);
        Assert.Equal("130,50", detail.Total);
    }

    [Fact]
    public async Task Should_fall_back_to_the_generic_icon_and_color_for_a_legacy_category()
    {
        var category = NewCategory("Auto", color: "#176B5B", icon: "rocket");

        var detail = await Detail(category, Period.Month(2026, 11));

        Assert.Equal(("more", "grey"), (detail.IconKey, detail.ColorKey));
    }

    [Fact]
    public async Task Should_keep_the_detail_of_an_archived_category()
    {
        var category = NewCategory("Vecchia", archived: true);

        var detail = await Detail(category, Period.Month(2026, 11), expenses: [Spend(category, 700, Today)]);

        Assert.Equal("7,00", detail.Total);
        Assert.Single(detail.Groups);
    }

    [Fact]
    public async Task Should_total_a_year()
    {
        var category = NewCategory("Spesa");
        var expenses = new[] { Spend(category, 1_000, new DateOnly(2026, 1, 5)), Spend(category, 2_000, Today) };

        var detail = await Detail(category, Period.Year(2026), expenses: expenses);

        Assert.Equal("30,00", detail.Total);
    }

    [Fact]
    public async Task Should_navigate_like_the_home()
    {
        var category = NewCategory("Spesa");

        var current = await Detail(category, Period.Month(2026, 11));
        var past = await Detail(category, Period.Month(2026, 9));
        var year = await Detail(category, Period.Year(2026));

        Assert.Equal(new HomeNavigation(Period.Month(2026, 10), null, Period.Year(2026)), current.Navigation);
        Assert.Equal(new HomeNavigation(Period.Month(2026, 8), Period.Month(2026, 10), Period.Year(2026)), past.Navigation);
        Assert.Equal(new HomeNavigation(Period.Year(2025), null, Period.Month(2026, 11)), year.Navigation);
    }

    [Fact]
    public async Task Should_show_no_budget_when_the_category_has_none()
    {
        var withoutBudget = NewCategory("Spesa");
        var zeroBudget = NewCategory("Zero", budgetCents: 0);

        var none = await Detail(withoutBudget, Period.Month(2026, 11));
        var zero = await Detail(zeroBudget, Period.Month(2026, 11));

        Assert.Null(none.Budget);
        Assert.Null(zero.Budget);
        Assert.False(none.HasBudget);
        Assert.False(none.IsOverBudget);
    }

    [Fact]
    public async Task Should_say_what_is_left_of_the_budget()
    {
        var category = NewCategory("Ristoranti", budgetCents: 15_000);

        var detail = await Detail(category, Period.Month(2026, 11), expenses: [Spend(category, 12_000, Today)]);

        Assert.Equal(new BudgetSummary("Ne restano ", "30,00 €", " su 150"), detail.Budget);
        Assert.True(detail.HasBudget);
        Assert.False(detail.IsOverBudget);
    }

    [Fact]
    public async Task Should_say_nothing_is_left_when_the_budget_is_spent_exactly()
    {
        var category = NewCategory("Ristoranti", budgetCents: 15_000);

        var detail = await Detail(category, Period.Month(2026, 11), expenses: [Spend(category, 15_000, Today)]);

        Assert.False(detail.IsOverBudget);
        Assert.Equal(new BudgetSummary("Ne restano ", "0,00 €", " su 150"), detail.Budget);
    }

    [Fact]
    public async Task Should_say_when_the_budget_is_exceeded()
    {
        var category = NewCategory("Ristoranti", budgetCents: 15_000);

        var detail = await Detail(category, Period.Month(2026, 11), expenses: [Spend(category, 17_850, Today)]);

        Assert.True(detail.IsOverBudget);
        Assert.Equal(new BudgetSummary("Oltre il budget di ", "28,50 €", ""), detail.Budget);
    }

    [Fact]
    public async Task Should_show_the_budget_cents_when_it_is_not_a_whole_amount()
    {
        var category = NewCategory("Ristoranti", budgetCents: 160_050);

        var detail = await Detail(category, Period.Month(2026, 11));

        Assert.Equal(new BudgetSummary("Ne restano ", "1.600,50 €", " su 1.600,50"), detail.Budget);
    }

    [Fact]
    public async Task Should_scale_the_budget_to_twelve_months_for_a_year()
    {
        var category = NewCategory("Ristoranti", budgetCents: 10_000);

        var detail = await Detail(category, Period.Year(2026), expenses: [Spend(category, 20_000, Today)]);

        Assert.Equal(new BudgetSummary("Ne restano ", "1.000,00 €", " su 1.200"), detail.Budget);
    }

    [Fact]
    public async Task Should_draw_a_bar_per_day_scaled_to_the_largest_day_so_far()
    {
        var category = NewCategory("Spesa");
        var expenses = new[]
        {
            Spend(category, 4_000, new DateOnly(2026, 11, 1)),
            Spend(category, 1_000, new DateOnly(2026, 11, 10)),
            Spend(category, 2_000, Today),
            Spend(category, 99_000, new DateOnly(2026, 11, 25))
        };

        var bars = (await Detail(category, Period.Month(2026, 11), expenses: expenses)).Bars;

        Assert.Equal(30, bars.Count);
        Assert.Equal(new SpendBar(1, SpendBarKind.Past), bars[0]);
        Assert.Equal(new SpendBar(0.25, SpendBarKind.Past), bars[9]);
        Assert.Equal(new SpendBar(0, SpendBarKind.Past), bars[10]);
        Assert.Equal(new SpendBar(0.5, SpendBarKind.Current), bars[18]);
        Assert.Equal(new SpendBar(0, SpendBarKind.Future), bars[24]);
        Assert.Equal(11, bars.Count(bar => bar.Kind == SpendBarKind.Future));
    }

    [Fact]
    public async Task Should_draw_a_bar_per_month_for_a_year()
    {
        var category = NewCategory("Spesa");
        var expenses = new[] { Spend(category, 3_000, new DateOnly(2026, 2, 3)), Spend(category, 1_500, Today) };

        var bars = (await Detail(category, Period.Year(2026), expenses: expenses)).Bars;

        Assert.Equal(12, bars.Count);
        Assert.Equal(new SpendBar(1, SpendBarKind.Past), bars[1]);
        Assert.Equal(new SpendBar(0.5, SpendBarKind.Current), bars[10]);
        Assert.Equal(new SpendBar(0, SpendBarKind.Future), bars[11]);
    }

    [Fact]
    public async Task Should_draw_flat_bars_when_the_category_has_no_spending()
    {
        var detail = await Detail(NewCategory("Spesa"), Period.Month(2026, 9));

        Assert.All(detail.Bars, bar => Assert.Equal(new SpendBar(0, SpendBarKind.Past), bar));
    }

    [Fact]
    public async Task Should_offer_tutti_and_the_tags_with_spending_largest_first_then_by_name()
    {
        var category = NewCategory("Ristoranti");
        var pizza = new Tag(Guid.NewGuid(), category.CategoryId, "Pizza");
        var asporto = new Tag(Guid.NewGuid(), category.CategoryId, "Asporto");
        var idle = new Tag(Guid.NewGuid(), category.CategoryId, "Colazione");
        var old = new Tag(Guid.NewGuid(), category.CategoryId, "Vecchio");
        old.Archive();
        var expenses = new[]
        {
            Spend(category, 1_000, Today, tagId: pizza.TagId),
            Spend(category, 1_000, Today, tagId: asporto.TagId),
            Spend(category, 3_000, Today, tagId: old.TagId),
            Spend(category, 500, new DateOnly(2026, 10, 31), tagId: idle.TagId)
        };

        var detail = await Detail(category, Period.Month(2026, 11), tags: [pizza, asporto, idle, old], expenses: expenses);

        Assert.Equal(["Tutti", "Asporto", "Pizza"], detail.Tags.Select(chip => chip.Label));
        Assert.Equal([TagFilter.All, TagFilter.Tag(asporto.TagId), TagFilter.Tag(pizza.TagId)], detail.Tags.Select(chip => chip.Filter));
        Assert.Equal([true, false, false], detail.Tags.Select(chip => chip.IsSelected));
    }

    [Fact]
    public async Task Should_offer_senza_tag_only_when_there_is_untagged_spending()
    {
        var category = NewCategory("Ristoranti");
        var pizza = new Tag(Guid.NewGuid(), category.CategoryId, "Pizza");

        var onlyTagged = await Detail(category, Period.Month(2026, 11), tags: [pizza],
            expenses: [Spend(category, 1_000, Today, tagId: pizza.TagId)]);
        var withUntagged = await Detail(category, Period.Month(2026, 11), tags: [pizza],
            expenses: [Spend(category, 1_000, Today, tagId: pizza.TagId), Spend(category, 500, Today)]);

        Assert.Equal(["Tutti", "Pizza"], onlyTagged.Tags.Select(chip => chip.Label));
        Assert.Equal(["Tutti", "Pizza", "Senza tag"], withUntagged.Tags.Select(chip => chip.Label));
        Assert.Equal(TagFilter.Untagged, withUntagged.Tags[2].Filter);
    }

    [Fact]
    public async Task Should_mark_the_selected_chip_even_when_it_has_no_spending_or_is_archived()
    {
        var category = NewCategory("Ristoranti");
        var idle = new Tag(Guid.NewGuid(), category.CategoryId, "Colazione");
        var old = new Tag(Guid.NewGuid(), category.CategoryId, "Vecchio");
        old.Archive();

        var idleDetail = await Detail(category, Period.Month(2026, 11), TagFilter.Tag(idle.TagId), [idle, old]);
        var archivedDetail = await Detail(category, Period.Month(2026, 11), TagFilter.Tag(old.TagId), [idle, old]);
        var untaggedDetail = await Detail(category, Period.Month(2026, 11), TagFilter.Untagged, [idle, old]);

        Assert.Equal([("Tutti", false), ("Colazione", true)], idleDetail.Tags.Select(chip => (chip.Label, chip.IsSelected)));
        Assert.Equal([("Tutti", false), ("Vecchio", true)], archivedDetail.Tags.Select(chip => (chip.Label, chip.IsSelected)));
        Assert.Equal([("Tutti", false), ("Senza tag", true)], untaggedDetail.Tags.Select(chip => (chip.Label, chip.IsSelected)));
    }

    [Fact]
    public async Task Should_list_only_the_selected_tag_but_keep_the_summary_whole()
    {
        var category = NewCategory("Ristoranti", budgetCents: 15_000);
        var pizza = new Tag(Guid.NewGuid(), category.CategoryId, "Pizza");
        var expenses = new[]
        {
            Spend(category, 1_000, Today, tagId: pizza.TagId, note: "Margherita"),
            Spend(category, 2_000, Today, note: "Cena"),
            Spend(category, 4_000, new DateOnly(2026, 11, 3))
        };

        var all = await Detail(category, Period.Month(2026, 11), TagFilter.All, [pizza], expenses);
        var tagged = await Detail(category, Period.Month(2026, 11), TagFilter.Tag(pizza.TagId), [pizza], expenses);
        var untagged = await Detail(category, Period.Month(2026, 11), TagFilter.Untagged, [pizza], expenses);

        Assert.Equal(["Oggi", "mar 3 nov"], all.Groups.Select(group => group.Label));
        Assert.Equal(["Margherita"], tagged.Groups.Single().Rows.Select(row => row.Title));
        Assert.Equal(["Oggi", "mar 3 nov"], untagged.Groups.Select(group => group.Label));
        Assert.Equal("Cena", untagged.Groups[0].Rows.Single().Title);
        Assert.Equal(["70,00", "70,00", "70,00"], new[] { all.Total, tagged.Total, untagged.Total });
        Assert.Equal(all.Budget, tagged.Budget);
        Assert.Equal(all.Bars, tagged.Bars);
        Assert.Equal(all.Tags.Select(chip => chip.Label), tagged.Tags.Select(chip => chip.Label));
    }

    [Fact]
    public async Task Should_group_expenses_like_the_history_without_naming_the_category_in_every_row()
    {
        var category = NewCategory("Ristoranti", color: "mint", icon: "food");
        var pizza = new Tag(Guid.NewGuid(), category.CategoryId, "Pizza");
        var expenses = new[]
        {
            Spend(category, 1_250, Today, tagId: pizza.TagId, note: "Da Mario"),
            Spend(category, 1_599, new DateOnly(2026, 11, 18), recurringPaymentId: Guid.NewGuid())
        };

        var month = await Detail(category, Period.Month(2026, 11), tags: [pizza], expenses: expenses);
        var year = await Detail(category, Period.Year(2026), tags: [pizza], expenses: expenses);

        Assert.Equal(["Oggi", "Ieri"], month.Groups.Select(group => group.Label));
        Assert.Equal(new HistoryRow(expenses[0].ExpenseId, "Da Mario", "Pizza", "12,50 €", "food", "mint"),
            month.Groups[0].Rows.Single());
        var automatic = month.Groups[1].Rows.Single();
        Assert.Equal(("Ristoranti", "automatica"), (automatic.Title, automatic.Detail));
        Assert.Equal(["Novembre"], year.Groups.Select(group => group.Label));
        Assert.Equal("28,49 €", year.Groups.Single().Total);
    }

    [Fact]
    public async Task Should_say_what_is_missing_when_the_period_is_empty()
    {
        var category = NewCategory("Ristoranti");
        var pizza = new Tag(Guid.NewGuid(), category.CategoryId, "Pizza");

        var month = await Detail(category, Period.Month(2026, 11));
        var year = await Detail(category, Period.Year(2026));
        var tagged = await Detail(category, Period.Month(2026, 11), TagFilter.Tag(pizza.TagId), [pizza]);

        Assert.Equal("Nessuna spesa in Ristoranti a novembre", month.EmptyText);
        Assert.Equal("Nessuna spesa in Ristoranti nel 2026", year.EmptyText);
        Assert.Equal("Nessuna spesa in Ristoranti · Pizza a novembre", tagged.EmptyText);
        Assert.True(month.HasNoGroups);
        Assert.False(month.HasGroups);
    }

    [Fact]
    public async Task Should_follow_the_clock()
    {
        var category = NewCategory("Spesa");

        var detail = await Detail(category, Period.Month(2026, 11), expenses: [Spend(category, 100, Today)],
            today: new DateOnly(2026, 12, 1));

        Assert.Equal(Period.Month(2026, 12), detail.Navigation.Next);
        Assert.Equal("gio 19 nov", detail.Groups.Single().Label);
        Assert.All(detail.Bars, bar => Assert.Equal(SpendBarKind.Past, bar.Kind));
    }

    [Fact]
    public async Task Should_return_the_repository_failure()
    {
        var category = NewCategory("Spesa");
        var failingCategories = new FakeCategoryRepository { Failure = Failed };
        var failingTags = new FakeTagRepository { Failure = Failed };
        var failingExpenses = new FakeExpenseRepository { Failure = Failed };
        var categories = new FakeCategoryRepository();
        categories.Items.Add(category);

        var categoryFailure = await Service(failingCategories, new FakeTagRepository(), new FakeExpenseRepository())
            .Get(category.CategoryId, Period.Month(2026, 11), TagFilter.All);
        var tagFailure = await Service(categories, failingTags, new FakeExpenseRepository())
            .Get(category.CategoryId, Period.Month(2026, 11), TagFilter.All);
        var expenseFailure = await Service(categories, new FakeTagRepository(), failingExpenses)
            .Get(category.CategoryId, Period.Month(2026, 11), TagFilter.All);

        Assert.Equal(Failed, categoryFailure.Error);
        Assert.Equal(Failed, tagFailure.Error);
        Assert.Equal(Failed, expenseFailure.Error);
    }

    private static CategoryDetailService Service(FakeCategoryRepository categories, FakeTagRepository tags,
        FakeExpenseRepository expenses, DateOnly? today = null) =>
        new(categories, tags, expenses, () => today ?? Today);

    private static async Task<CategoryDetail> Detail(Category category, Period period, TagFilter? tag = null,
        IEnumerable<Tag>? tags = null, IEnumerable<Expense>? expenses = null, DateOnly? today = null)
    {
        var categoryRepository = new FakeCategoryRepository();
        categoryRepository.Items.Add(category);
        var tagRepository = new FakeTagRepository();
        tagRepository.Items.AddRange(tags ?? []);
        var expenseRepository = new FakeExpenseRepository();
        expenseRepository.Items.AddRange(expenses ?? []);

        var result = await Service(categoryRepository, tagRepository, expenseRepository, today)
            .Get(category.CategoryId, period, tag ?? TagFilter.All);

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

    private static Expense Spend(Category category, long cents, DateOnly on, string note = "", Guid? tagId = null,
        Guid? recurringPaymentId = null) =>
        new(Guid.NewGuid(), category.CategoryId, new Money(cents), on, tagId, note, recurringPaymentId: recurringPaymentId);
}
