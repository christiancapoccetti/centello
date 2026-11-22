using System.Text.RegularExpressions;

namespace Centello.Application.Tests;

public sealed class CategoryPalette_should_expected_behavior
{
    private static readonly string[] SlotKeys = ["coral", "peri", "butter", "mint", "lilac", "sky", "grey"];

    [Fact]
    public void Should_offer_six_chromatic_slots_and_grey_last() =>
        Assert.Equal(SlotKeys, CategoryPalette.All.Select(color => color.Key));

    [Fact]
    public void Should_hand_out_every_slot_but_grey() =>
        Assert.Equal(["coral", "peri", "butter", "mint", "lilac", "sky"], CategoryPalette.Chromatic.Select(color => color.Key));

    [Fact]
    public void Should_pick_the_first_slot_when_nothing_is_used() =>
        Assert.Equal("coral", CategoryPalette.LeastUsed([]).Key);

    [Fact]
    public void Should_pick_the_least_used_slot_and_break_ties_by_palette_order()
    {
        Assert.Equal("peri", CategoryPalette.LeastUsed(["coral"]).Key);
        Assert.Equal("sky", CategoryPalette.LeastUsed(["coral", "peri", "butter", "mint", "lilac"]).Key);
        Assert.Equal("mint", CategoryPalette.LeastUsed(["coral", "coral", "peri", "butter", "lilac", "sky", "sky"]).Key);
        Assert.Equal("coral", CategoryPalette.LeastUsed(["coral", "peri", "butter", "mint", "lilac", "sky"]).Key);
    }

    [Fact]
    public void Should_ignore_grey_and_values_that_are_not_slots() =>
        Assert.Equal("coral", CategoryPalette.LeastUsed(["grey", "grey", "#176B5B", "Coral", "magenta"]).Key);

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
