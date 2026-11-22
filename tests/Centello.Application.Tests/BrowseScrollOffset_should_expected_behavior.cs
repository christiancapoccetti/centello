using Centello.App.Controls;

namespace Centello.Application.Tests;

public sealed class BrowseScrollOffset_should_expected_behavior
{
    [Theory]
    [InlineData(505, 480, 80, 54, 900, 250, 105)]
    [InlineData(130, 120, 560, 54, 1250, 500, 570)]
    [InlineData(505, 480, 80, 20, 900, 250, 99)]
    [InlineData(505, 480, 180, 54, 300, 220, 80)]
    [InlineData(40, 45, 5, 45, 220, 250, 0)]
    public void preserve_the_visible_part_of_an_item_within_the_available_scroll_range(
        double previousViewportTop, double previousItemTop, double itemTop, double itemHeight,
        double contentHeight, double viewportHeight, double expected)
    {
        var anchor = BrowseScrollOffset.Capture(previousViewportTop, previousItemTop);

        Assert.Equal(expected, anchor.Restore(itemTop, itemHeight, contentHeight, viewportHeight));
    }
}
