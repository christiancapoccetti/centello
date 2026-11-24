using Centello.App;

namespace Centello.Application.Tests;

public sealed class CategoryBudgetWidgetLayout_should_expected_behavior
{
    [Theory]
    [InlineData(-1, 0)]
    [InlineData(37, 0)]
    [InlineData(38, 1)]
    [InlineData(76, 2)]
    [InlineData(114, 3)]
    public void Should_fit_up_to_three_categories_at_compact_height(int availableHeightDp, int expectedRows) =>
        Assert.Equal(expectedRows, CategoryBudgetWidgetLayout.RowCapacity(availableHeightDp));

    [Theory]
    [InlineData(191, 3)]
    [InlineData(192, 4)]
    [InlineData(239, 4)]
    [InlineData(240, 5)]
    [InlineData(278, 5)]
    [InlineData(288, 6)]
    public void Should_allocate_at_least_48_dp_per_row_for_four_or_more_rows(int availableHeightDp, int expectedRows) =>
        Assert.Equal(expectedRows, CategoryBudgetWidgetLayout.RowCapacity(availableHeightDp));
}
