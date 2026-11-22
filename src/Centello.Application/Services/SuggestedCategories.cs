namespace Centello.Application;

/// <summary>A category the first run offers. The keys are catalog keys of <see cref="CategoryIcons"/> and
/// <see cref="CategoryPalette"/>.</summary>
public sealed record SuggestedCategory(string Name, string IconKey, string ColorKey);

public static class SuggestedCategories
{
    /// <summary>Offered in this order. None carries a budget: the user has no spending history to size one from.</summary>
    public static IReadOnlyList<SuggestedCategory> All { get; } =
    [
        new("Spesa", "cart", "coral"),
        new("Casa", "home", "peri"),
        new("Trasporti", "car", "butter"),
        new("Ristoranti", "food", "mint"),
        new("Salute", "health", "sky"),
        new("Svago", "leisure", "lilac")
    ];
}
