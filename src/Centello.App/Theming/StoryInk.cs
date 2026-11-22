using Centello.Application;

namespace Centello.App.Theming;

/// <summary>The single mapping from a card's surface to pixels. On the ground the story reads like the rest of the
/// app. On a palette surface everything is tile ink, at 74%, 22% and 30% for the quieter parts, over the category's
/// own colour. Resolved for one scheme: the page builds a card again when the theme flips.</summary>
public sealed record StoryInk(Color Ground, Color Ink, Color Mute, Color Hairline, Color Bar, bool IsGround, bool IsDark)
{
    private const float MuteAlpha = 0.74f;
    private const float HairlineAlpha = 0.22f;
    private const float BarAlpha = 0.30f;

    public static StoryInk Of(RecapSurface surface, bool isDark)
    {
        if (surface.IsGround)
            return new StoryInk(
                ThemeColors.Resolve(ThemeColor.Ground, isDark),
                ThemeColors.Resolve(ThemeColor.Ink, isDark),
                ThemeColors.Resolve(ThemeColor.Mute, isDark),
                ThemeColors.Resolve(ThemeColor.Hairline, isDark),
                ThemeColors.Resolve(ThemeColor.ChartBar, isDark),
                true,
                isDark);

        var tileInk = ThemeColors.Resolve(ThemeColor.TileInk, isDark);
        return new StoryInk(
            ThemeColors.ResolvePalette(surface.PaletteKey, isDark),
            tileInk,
            tileInk.WithAlpha(MuteAlpha),
            tileInk.WithAlpha(HairlineAlpha),
            tileInk.WithAlpha(BarAlpha),
            false,
            isDark);
    }

    public Color Palette(string colorKey) => ThemeColors.ResolvePalette(colorKey, IsDark);

    public Color TileInk => ThemeColors.Resolve(ThemeColor.TileInk, IsDark);
}
