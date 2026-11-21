namespace Centesimo.Application.Tests;

public sealed class AddWidgetShape_should_expected_behavior
{
    private static readonly AddWidgetWords Words = new(AddDp: 115, VoiceDp: 56, PhotoDp: 60);

    [Fact]
    public void Should_keep_a_two_by_one_compact() =>
        Assert.Equal(AddWidgetShape.Compact, AddWidgetShapes.Of(170, 100, Words));

    [Fact]
    public void Should_show_three_icons_on_a_three_by_one() =>
        Assert.Equal(AddWidgetShape.Icons, AddWidgetShapes.Of(260, 100, Words));

    [Fact]
    public void Should_show_the_bar_on_a_four_by_one() =>
        Assert.Equal(AddWidgetShape.Bar, AddWidgetShapes.Of(360, 100, Words));

    [Fact]
    public void Should_show_the_tall_card_on_a_four_by_two() =>
        Assert.Equal(AddWidgetShape.Tall, AddWidgetShapes.Of(360, 220, Words));

    [Fact]
    public void Should_fall_back_to_one_row_on_a_two_by_two() =>
        Assert.Equal(AddWidgetShape.Compact, AddWidgetShapes.Of(170, 220, Words));

    [Fact]
    public void Should_fall_back_to_the_bar_when_the_height_fits_one_row_only() =>
        Assert.Equal(AddWidgetShape.Bar, AddWidgetShapes.Of(360, AddWidgetShapes.TallMinHeightDp - 1, Words));

    [Fact]
    public void Should_switch_to_tall_at_its_minimum_height() =>
        Assert.Equal(AddWidgetShape.Tall, AddWidgetShapes.Of(360, AddWidgetShapes.TallMinHeightDp, Words));

    [Fact]
    public void Should_move_the_bar_threshold_with_the_add_label()
    {
        var longer = Words with { AddDp = Words.AddDp + 40 };
        var width = AddWidgetShapes.BarMinWidthDp(Words);

        Assert.Equal(AddWidgetShape.Bar, AddWidgetShapes.Of((int)Math.Ceiling(width), 100, Words));
        Assert.Equal(AddWidgetShape.Icons, AddWidgetShapes.Of((int)Math.Ceiling(width) - 1, 100, Words));
        Assert.Equal(AddWidgetShape.Icons, AddWidgetShapes.Of((int)Math.Ceiling(width), 100, longer));
    }

    [Fact]
    public void Should_keep_the_bar_in_a_tall_widget_whose_half_pills_cannot_hold_their_words()
    {
        var wide = Words with { PhotoDp = 160 };

        Assert.Equal(AddWidgetShape.Bar, AddWidgetShapes.Of(360, 220, wide));
    }

    [Theory]
    [InlineData(AddWidgetShape.Compact, false)]
    [InlineData(AddWidgetShape.Icons, true)]
    [InlineData(AddWidgetShape.Bar, true)]
    [InlineData(AddWidgetShape.Tall, true)]
    public void Should_offer_the_camera_in_every_shape_but_compact(AddWidgetShape shape, bool expected) =>
        Assert.Equal(expected, shape.HasPhoto());
}
