using Centello.App.ViewModels;
using Centello.Domain;

namespace Centello.Application.Tests;

public sealed class InsightsViewModel_should_expected_behavior
{
    [Fact]
    public async Task Should_order_only_changed_bars_by_absolute_euro_difference()
    {
        var fixture = InsightFixtures.November();
        fixture.Expenses.Items.Add(new Expense(Guid.NewGuid(), fixture.Salute.CategoryId,
            new Money(1_300), InsightFixtures.Today));
        var viewModel = ViewModel(fixture.Service());

        await viewModel.Load();

        Assert.Equal(["Trasporti", "Svago", "Ristoranti", "Spesa", "Salute", "Altro"],
            viewModel.Deltas.Select(row => row.Name));
        Assert.Equal<long?>([-5_940, -4_620, 3_220, 1_380, -1_380, 440],
            viewModel.Deltas.Select(row => row.Category.DeltaCents));
        Assert.Equal(viewModel.Overview!.Categories.Take(3), viewModel.LeadingCategories.Select(row => row.Category));
    }

    [Fact]
    public async Task Should_omit_bars_without_a_comparison()
    {
        var fixture = InsightFixtures.November();
        fixture.Expenses.Items.RemoveAll(expense => expense.OccurredOn.Month == 10);
        var viewModel = ViewModel(fixture.Service());

        await viewModel.Load();

        Assert.NotEmpty(viewModel.LeadingCategories);
        Assert.Empty(viewModel.Deltas);
    }

    [Fact]
    public async Task Should_offer_the_remaining_category_count_and_keep_the_leading_rows_on_toggle()
    {
        var fixture = InsightFixtures.November();
        var viewModel = ViewModel(fixture.Service());
        await viewModel.Load();
        var leading = viewModel.LeadingCategories;

        Assert.True(viewModel.HasMoreCategories);
        Assert.Equal(["Spesa", "Casa", "Trasporti"], leading.Select(row => row.Name));
        Assert.Empty(viewModel.MoreCategories);
        Assert.Equal("Mostra altre 4 categorie", viewModel.DisclosureTitle);

        viewModel.ToggleCategories();

        Assert.Equal("Mostra solo le prime 3", viewModel.DisclosureTitle);
        Assert.Equal(["Ristoranti", "Svago", "Salute", "Altro"], viewModel.MoreCategories.Select(row => row.Name));
        Assert.Same(leading, viewModel.LeadingCategories);

        viewModel.ToggleCategories();

        Assert.Equal("Mostra altre 4 categorie", viewModel.DisclosureTitle);
        Assert.Empty(viewModel.MoreCategories);
        Assert.Same(leading, viewModel.LeadingCategories);
    }

    [Fact]
    public async Task Should_keep_tied_and_previous_only_categories_in_spending_order_with_the_residual()
    {
        var categories = new FakeCategoryRepository();
        var expenses = new FakeExpenseRepository();
        var today = InsightFixtures.Today;
        var previous = today.AddMonths(-1);
        foreach (var (name, amount, date) in new[]
        {
            ("Zeta", 100L, today), ("Beta", 100L, today), ("Alfa", 100L, today), ("Solo prima", 100L, previous)
        })
        {
            var category = new Category(Guid.NewGuid(), name, "cart", "mint");
            categories.Items.Add(category);
            expenses.Items.Add(new Expense(Guid.NewGuid(), category.CategoryId, new Money(amount), date));
        }
        expenses.Items.Add(new Expense(Guid.NewGuid(), Guid.NewGuid(), new Money(500), previous));
        var viewModel = ViewModel(new InsightsService(categories, expenses, () => today));

        await viewModel.Load();
        viewModel.ToggleCategories();

        Assert.Equal(["Alfa", "Beta", "Zeta"], viewModel.LeadingCategories.Select(row => row.Name));
        Assert.Collection(viewModel.MoreCategories,
            row =>
            {
                Assert.Equal("Solo prima", row.Name);
                Assert.Equal(0, row.ShareFraction);
                Assert.NotNull(row.ComparedShare);
                Assert.True(row.CanOpen);
            },
            row =>
            {
                Assert.Equal("Altre categorie", row.Name);
                Assert.Equal(0, row.ShareFraction);
                Assert.NotNull(row.ComparedShare);
                Assert.True(row.IsResidual);
            });
    }

    [Fact]
    public async Task Should_preserve_expansion_on_refresh_and_reset_it_when_changing_period()
    {
        var fixture = InsightFixtures.November();
        var viewModel = ViewModel(fixture.Service());
        await viewModel.Load();
        viewModel.ToggleCategories();

        await viewModel.Load();

        Assert.NotEmpty(viewModel.MoreCategories);
        Assert.Equal("Mostra solo le prime 3", viewModel.DisclosureTitle);

        await viewModel.ShowPrevious();

        Assert.True(viewModel.HasMoreCategories);
        Assert.Empty(viewModel.MoreCategories);
        Assert.Equal("Mostra altre 4 categorie", viewModel.DisclosureTitle);
    }

    [Fact]
    public async Task Should_hide_the_toggle_at_three_rows_and_offer_singular_text_for_one_more()
    {
        var fixture = InsightFixtures.November();
        var viewModel = ViewModel(fixture.Service());
        var firstThree = new[] { fixture.Spesa.CategoryId, fixture.Casa.CategoryId, fixture.Trasporti.CategoryId };
        var fourth = fixture.Expenses.Items.Where(expense => expense.CategoryId == fixture.Ristoranti.CategoryId).ToArray();
        await viewModel.Load();
        viewModel.ToggleCategories();

        fixture.Expenses.Items.RemoveAll(expense => !firstThree.Contains(expense.CategoryId));
        await viewModel.Load();
        viewModel.ToggleCategories();

        Assert.False(viewModel.HasMoreCategories);
        Assert.Equal(3, viewModel.LeadingCategories.Count);
        Assert.Empty(viewModel.DisclosureTitle);
        Assert.Empty(viewModel.MoreCategories);

        fixture.Expenses.Items.AddRange(fourth);
        await viewModel.Load();

        Assert.True(viewModel.HasMoreCategories);
        Assert.Equal("Mostra un’altra categoria", viewModel.DisclosureTitle);
        Assert.Empty(viewModel.MoreCategories);

        viewModel.ToggleCategories();

        Assert.Equal("Ristoranti", Assert.Single(viewModel.MoreCategories).Name);
    }

    private static InsightsViewModel ViewModel(IInsightsService service) =>
        new(new FakeScopeFactory(new FakeServiceProvider(service)));
}
