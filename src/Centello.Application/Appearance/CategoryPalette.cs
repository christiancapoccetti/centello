namespace Centello.Application;

/// <summary>A category color slot. Dark and Light are the tile inks per scheme; Name is the Italian accessibility name.</summary>
public sealed record PaletteColor(string Key, string Name, string Dark, string Light);

public static class CategoryPalette
{
    public static readonly PaletteColor Fallback = new("grey", "Grigio", "#8A8D93", "#9A9DA3");

    /// <summary>Keys are persisted in Categories.Color (max 9): never rename or remove one.
    /// Legacy hex values are converted by the CategoryPaletteSlots migration, never here.</summary>
    public static IReadOnlyList<PaletteColor> All { get; } =
    [
        new("coral", "Corallo", "#FF8A7A", "#F47A68"),
        new("peri", "Pervinca", "#8EA6FF", "#7C95F2"),
        new("butter", "Burro", "#F5D06B", "#EDC14F"),
        new("mint", "Menta", "#6ED3A6", "#57C493"),
        new("lilac", "Lilla", "#D4A8F0", "#C597E6"),
        new("sky", "Celeste", "#8FD0F0", "#79C2E6"),
        Fallback
    ];

    /// <summary>The slots a category can be given. Grey is what a category without a known color shows, never a color
    /// to hand out.</summary>
    public static IReadOnlyList<PaletteColor> Chromatic { get; } = [.. All.Where(color => color != Fallback)];

    private static readonly IReadOnlyDictionary<string, PaletteColor> ByKey =
        All.ToDictionary(color => color.Key, StringComparer.Ordinal);

    /// <summary>The chromatic slot the fewest of <paramref name="usedKeys"/> hold: the first in palette order among the
    /// ones tied. Keys that are not chromatic slots count for nothing.</summary>
    public static PaletteColor LeastUsed(IEnumerable<string> usedKeys)
    {
        var uses = usedKeys
            .GroupBy(key => key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        return Chromatic.MinBy(color => uses.GetValueOrDefault(color.Key))!;
    }

    /// <summary>The slot stored under <paramref name="key"/>, or <see cref="Fallback"/> for a missing, unknown or legacy value.</summary>
    public static PaletteColor Resolve(string? key) =>
        key is not null && ByKey.TryGetValue(key, out var color) ? color : Fallback;
}
