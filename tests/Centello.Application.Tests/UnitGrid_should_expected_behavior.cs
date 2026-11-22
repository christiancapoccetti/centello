namespace Centello.Application.Tests;

public sealed class UnitGrid_should_expected_behavior
{
    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Should_draw_nothing_when_there_is_nothing_to_draw(int count) =>
        Assert.Equal((0.0, 0), UnitGrid.Fit(count, 300, 200, 4, 40));

    [Fact]
    public void Should_cap_the_side_at_the_maximum() =>
        Assert.Equal((40.0, 1), UnitGrid.Fit(1, 300, 200, 4, 40));

    [Fact]
    public void Should_fill_a_square_area_with_a_square_layout() =>
        Assert.Equal((10.0, 10), UnitGrid.Fit(100, 100, 100, 0, 50));

    [Fact]
    public void Should_leave_the_gap_between_squares() =>
        Assert.Equal((10.0, 2), UnitGrid.Fit(4, 21, 21, 1, 50));

    [Fact]
    public void Should_prefer_more_columns_when_the_side_is_the_same() =>
        Assert.Equal((50.0, 6), UnitGrid.Fit(6, 300, 50, 0, 100));

    [Fact]
    public void Should_stack_the_squares_in_a_tall_narrow_area() =>
        Assert.Equal((50.0, 1), UnitGrid.Fit(6, 50, 300, 0, 100));

    [Fact]
    public void Should_never_give_a_negative_side() =>
        Assert.Equal(0.0, UnitGrid.Fit(10, 5, 5, 10, 40).Side);
}
