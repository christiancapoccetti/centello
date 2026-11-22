namespace Centello.Application.Tests;

public sealed class TrailingValuePlacement_should_expected_behavior
{
    [Fact]
    public void Should_keep_the_value_beside_the_block_while_the_block_keeps_its_minimum() =>
        Assert.Equal(ValuePlacement.Beside, TrailingValuePlacement.Of(262, 95, 14, 120));

    [Fact]
    public void Should_keep_the_value_beside_the_block_at_exactly_the_minimum() =>
        Assert.Equal(ValuePlacement.Beside, TrailingValuePlacement.Of(120 + 14 + 95, 95, 14, 120));

    [Fact]
    public void Should_move_the_value_under_the_block_when_the_block_would_shrink_below_its_minimum() =>
        Assert.Equal(ValuePlacement.Below, TrailingValuePlacement.Of(120 + 14 + 95 - 1, 95, 14, 120));

    [Fact]
    public void Should_move_a_value_that_is_as_wide_as_the_row_under_the_block() =>
        Assert.Equal(ValuePlacement.Below, TrailingValuePlacement.Of(222, 190, 14, 120));

    [Fact]
    public void Should_keep_the_value_beside_the_block_when_the_width_has_no_limit() =>
        Assert.Equal(ValuePlacement.Beside, TrailingValuePlacement.Of(double.PositiveInfinity, 190, 14, 120));
}
