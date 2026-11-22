namespace Centello.Application.Tests;

public sealed class RecapSurface_should_expected_behavior
{
    [Fact]
    public void Should_start_on_the_ground()
    {
        Assert.True(RecapSurface.Ground.IsGround);
        Assert.Equal("", RecapSurface.Ground.PaletteKey);
    }

    [Theory]
    [InlineData("peri")]
    [InlineData("butter")]
    [InlineData("mint")]
    [InlineData("lilac")]
    [InlineData("sky")]
    public void Should_paint_a_category_card_in_its_palette_slot(string colorKey)
    {
        var surface = RecapSurface.ForCategory(colorKey);

        Assert.Equal(colorKey, surface.PaletteKey);
        Assert.False(surface.IsGround);
    }

    [Fact]
    public void Should_keep_a_coral_category_card_on_the_ground() =>
        Assert.Equal(RecapSurface.Ground, RecapSurface.ForCategory("coral"));

    [Fact]
    public void Should_keep_coral_in_a_block_of_the_category_stack()
    {
        var block = RecapSurface.Block("coral");

        Assert.Equal("coral", block.PaletteKey);
        Assert.False(block.IsGround);
    }

    [Theory]
    [InlineData("not-a-color")]
    [InlineData("")]
    [InlineData("#FF8A7A")]
    public void Should_resolve_an_unknown_key_to_grey(string colorKey) =>
        Assert.Equal("grey", RecapSurface.Block(colorKey).PaletteKey);
}
