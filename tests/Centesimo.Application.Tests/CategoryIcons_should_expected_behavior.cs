using System.Reflection;

namespace Centesimo.Application.Tests;

public sealed class CategoryIcons_should_expected_behavior
{
    private static readonly string[] StoredKeys =
    [
        "cart", "home", "car", "heart", "sport", "shopping-bag", "food", "gift",
        "beach", "tech", "school", "family", "gym", "gear", "more"
    ];

    [Fact]
    public void Should_keep_every_key_already_stored_on_devices() =>
        Assert.All(StoredKeys, key => Assert.Equal(key, CategoryIcons.Resolve(key).Key));

    [Fact]
    public void Should_offer_thirty_one_icons_with_the_fallback_last()
    {
        Assert.Equal(31, CategoryIcons.All.Count);
        Assert.Same(CategoryIcons.Fallback, CategoryIcons.All[^1]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("rocket")]
    [InlineData("Cart")]
    public void Should_fall_back_to_more_for_unknown_keys(string? key) =>
        Assert.Same(CategoryIcons.Fallback, CategoryIcons.Resolve(key));

    [Fact]
    public void Should_use_unique_keys_that_fit_the_icon_column()
    {
        var keys = CategoryIcons.All.Select(icon => icon.Key).ToList();

        Assert.Equal(keys.Count, keys.Distinct(StringComparer.Ordinal).Count());
        Assert.All(keys, key => Assert.InRange(key.Length, 1, 40));
    }

    [Fact]
    public void Should_draw_every_icon_with_a_glyph_from_the_symbol_table()
    {
        var symbols = typeof(MaterialSymbols)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral)
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToHashSet();

        Assert.All(CategoryIcons.All, icon => Assert.Contains(icon.Glyph, symbols));
    }

    [Fact]
    public void Should_label_every_icon() =>
        Assert.All(CategoryIcons.All, icon => Assert.False(string.IsNullOrWhiteSpace(icon.Label)));
}
