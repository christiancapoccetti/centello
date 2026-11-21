using Centesimo.Domain;

namespace Centesimo.Application;

/// <summary>The ground, or a category's palette ink under tile-ink text. Coral never paints a whole card or a half:
/// coral means "add an expense". A coral category's card stays on the ground and keeps its ink in tile and bars.</summary>
public sealed record RecapSurface
{
    private RecapSurface(string paletteKey) => PaletteKey = paletteKey;

    public static RecapSurface Ground { get; } = new("");

    public string PaletteKey { get; }
    public bool IsGround => PaletteKey.IsEmpty();

    /// <summary>A block of the category stack: every slot, coral included.</summary>
    public static RecapSurface Block(string colorKey) => new(CategoryPalette.Resolve(colorKey).Key);

    /// <summary>A card or half card about one category.</summary>
    public static RecapSurface ForCategory(string colorKey) =>
        CategoryPalette.Resolve(colorKey).Key == "coral" ? Ground : Block(colorKey);
}
