using System.Text.RegularExpressions;

namespace Centesimo.Application.Tests;

public sealed class CategoryPalette_should_expected_behavior
{
    private static readonly string[] SlotKeys = ["coral", "peri", "butter", "mint", "lilac", "sky", "grey"];

    [Fact]
    public void Should_offer_six_chromatic_slots_and_grey_last() =>
        Assert.Equal(SlotKeys, CategoryPalette.All.Select(color => color.Key));

    [Fact]
    public void Should_resolve_every_slot_to_itself() =>
        Assert.All(SlotKeys, key => Assert.Equal(key, CategoryPalette.Resolve(key).Key));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("magenta")]
    [InlineData("Coral")]
    [InlineData("#176B5B")]
    [InlineData("#8B4A5D")]
    public void Should_fall_back_to_grey_for_unknown_and_legacy_values(string? value) =>
        Assert.Same(CategoryPalette.Fallback, CategoryPalette.Resolve(value));

    [Fact]
    public void Should_use_unique_keys_that_fit_the_color_column()
    {
        var keys = CategoryPalette.All.Select(color => color.Key).ToList();

        Assert.Equal(keys.Count, keys.Distinct(StringComparer.Ordinal).Count());
        Assert.All(keys, key => Assert.InRange(key.Length, 1, 9));
    }

    [Fact]
    public void Should_define_both_schemes_as_hex_colors() =>
        Assert.All(CategoryPalette.All, color =>
        {
            Assert.Matches(new Regex("^#[0-9A-F]{6}$"), color.Dark);
            Assert.Matches(new Regex("^#[0-9A-F]{6}$"), color.Light);
        });

    [Fact]
    public void Should_name_every_slot() =>
        Assert.All(CategoryPalette.All, color => Assert.False(string.IsNullOrWhiteSpace(color.Name)));
}
