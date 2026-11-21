using Centesimo.Application;

namespace Centesimo.App.Theming;

/// <summary>The only color table in the App. XAML reads it through {t:Theme}, code through these helpers.</summary>
public static class ThemeColors
{
    private static readonly IReadOnlyDictionary<ThemeColor, (Color Dark, Color Light)> Pairs =
        Enum.GetValues<ThemeColor>()
            .ToDictionary(color => color, color => Parse(ThemeTokens.Hex(color)));

    public static Color Dark(ThemeColor color) => Pairs[color].Dark;

    public static Color Light(ThemeColor color) => Pairs[color].Light;

    public static Color Resolve(ThemeColor color, bool isDark) => isDark ? Dark(color) : Light(color);

    /// <summary>For transient paint only (press feedback): it does not follow a later theme change. Before the MAUI
    /// application exists (the voice panel can start the process) the system theme decides.</summary>
    public static Color Current(ThemeColor color) =>
        Resolve(color, (Microsoft.Maui.Controls.Application.Current?.RequestedTheme ?? AppInfo.Current.RequestedTheme) == AppTheme.Dark);

    public static Color ResolvePalette(string? colorKey, bool isDark)
    {
        var slot = CategoryPalette.Resolve(colorKey);
        return Color.FromArgb(isDark ? slot.Dark : slot.Light);
    }

    public static void SetThemeColor(this BindableObject target, BindableProperty property, ThemeColor color) =>
        target.SetAppThemeColor(property, Light(color), Dark(color));

    public static void SetPaletteColor(this BindableObject target, BindableProperty property, string? colorKey) =>
        target.SetAppThemeColor(property, ResolvePalette(colorKey, false), ResolvePalette(colorKey, true));

    private static (Color Dark, Color Light) Parse((string Dark, string Light) values) =>
        (Color.FromArgb(values.Dark), Color.FromArgb(values.Light));
}
