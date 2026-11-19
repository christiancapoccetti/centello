namespace Centesimo.Application.Tests;

public sealed class SuggestedCategories_should_expected_behavior
{
    [Fact]
    public void Offer_the_six_default_categories_in_order()
    {
        Assert.Equal(
            [
                new SuggestedCategory("Spesa", "cart", "coral"),
                new SuggestedCategory("Casa", "home", "peri"),
                new SuggestedCategory("Trasporti", "car", "butter"),
                new SuggestedCategory("Ristoranti", "food", "mint"),
                new SuggestedCategory("Salute", "health", "sky"),
                new SuggestedCategory("Svago", "leisure", "lilac")
            ],
            SuggestedCategories.All);
    }

    [Fact]
    public void Use_only_icons_and_colors_of_the_catalogs()
    {
        Assert.All(SuggestedCategories.All, suggestion =>
        {
            Assert.NotEqual(CategoryIcons.Fallback, CategoryIcons.Resolve(suggestion.IconKey));
            Assert.NotEqual(CategoryPalette.Fallback, CategoryPalette.Resolve(suggestion.ColorKey));
        });
    }

    [Fact]
    public void Use_a_different_name_for_each_suggestion()
    {
        Assert.Equal(SuggestedCategories.All.Count,
            SuggestedCategories.All.Select(suggestion => suggestion.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }
}
