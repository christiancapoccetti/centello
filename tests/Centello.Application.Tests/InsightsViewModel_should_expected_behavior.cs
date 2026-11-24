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
        var viewModel = new InsightsViewModel(new FakeScopeFactory(new FakeServiceProvider(fixture.Service())));

        await viewModel.Load();

        Assert.Equal(["Trasporti", "Svago", "Ristoranti", "Spesa", "Salute", "Altro"],
            viewModel.Deltas.Select(row => row.Name));
        Assert.Equal<long?>([-5_940, -4_620, 3_220, 1_380, -1_380, 440],
            viewModel.Deltas.Select(row => row.Category.DeltaCents));
        Assert.Equal(viewModel.Overview!.Categories, viewModel.Categories.Select(row => row.Category));
        Assert.Equal(viewModel.Overview.Composition.Leading, viewModel.LeadingCategories.Select(row => row.Category));
    }

    [Fact]
    public async Task Should_omit_bars_without_a_comparison()
    {
        var fixture = InsightFixtures.November();
        fixture.Expenses.Items.RemoveAll(expense => expense.OccurredOn.Month == 10);
        var viewModel = new InsightsViewModel(new FakeScopeFactory(new FakeServiceProvider(fixture.Service())));

        await viewModel.Load();

        Assert.NotEmpty(viewModel.Categories);
        Assert.Empty(viewModel.Deltas);
    }
}
