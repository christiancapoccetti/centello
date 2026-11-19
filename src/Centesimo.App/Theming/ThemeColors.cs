using Centesimo.Application;

namespace Centesimo.App.Theming;

public enum ThemeColor
{
    Ground,
    SurfaceRaised,
    Ink,
    Mute,
    Hairline,
    Outline,
    ChartBar,
    Pace,
    Accent,
    Alert,
    PositiveContainer,
    OnPositiveContainer,
    TileInk,
    Scrim
}

/// <summary>The only color table in the App. XAML reads it through {t:Theme}, code through these helpers.</summary>
public static class ThemeColors
{
    private static readonly IReadOnlyDictionary<ThemeColor, (Color Dark, Color Light)> Pairs =
        Enum.GetValues<ThemeColor>()
            .ToDictionary(color => color, color => Parse(Values(color)));

    public static Color Dark(ThemeColor color) => Pairs[color].Dark;

    public static Color Light(ThemeColor color) => Pairs[color].Light;

    public static Color Resolve(ThemeColor color, bool isDark) => isDark ? Dark(color) : Light(color);

    /// <summary>For transient paint only (press feedback): it does not follow a later theme change.</summary>
    public static Color Current(ThemeColor color) =>
        Resolve(color, Microsoft.Maui.Controls.Application.Current?.RequestedTheme == AppTheme.Dark);

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

    // No discard arm: CS8524 is suppressed in the csproj, so a member without a row is CS8509 and fails the build.
    private static (string Dark, string Light) Values(ThemeColor color) => color switch
    {
        ThemeColor.Ground => ("#141516", "#FAFAF8"),
        ThemeColor.SurfaceRaised => ("#26282B", "#EFEFEA"),
        ThemeColor.Ink => ("#F4F4F0", "#16161A"),
        ThemeColor.Mute => ("#A0A3A9", "#62626A"),
        ThemeColor.Hairline => ("#2A2C2F", "#E4E4DF"),
        ThemeColor.Outline => ("#3A3D41", "#DCDCD6"),
        ThemeColor.ChartBar => ("#55585E", "#C9C9C3"),
        ThemeColor.Pace => ("#8FA8FF", "#1F4FD8"),
        ThemeColor.Accent => ("#FF8A7A", "#E0503D"),
        ThemeColor.Alert => ("#FF8A7A", "#C8261A"),
        ThemeColor.PositiveContainer => ("#16302A", "#E3F2EA"),
        ThemeColor.OnPositiveContainer => ("#7FD6A8", "#0B5A3D"),
        ThemeColor.TileInk => ("#141516", "#16161A"),
        ThemeColor.Scrim => ("#80000000", "#80000000")
    };
}
