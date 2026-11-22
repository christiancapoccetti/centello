namespace Centello.Application;

public enum ThemeColor
{
    Ground,
    SurfaceRaised,
    Selected,
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

/// <summary>The dark and light value of every interface token. The App's ThemeColors parses this table for MAUI, and
/// values/theme_colors.xml and values-night/theme_colors.xml carry it for the views the launcher inflates. A test
/// fails when either file differs from it.</summary>
public static class ThemeTokens
{
    // No discard arm: CS8524 is suppressed in the csproj, so a member without a row is CS8509 and fails the build.
    public static (string Dark, string Light) Hex(ThemeColor color) => color switch
    {
        ThemeColor.Ground => ("#141516", "#FAFAF8"),
        ThemeColor.SurfaceRaised => ("#26282B", "#EFEFEA"),
        ThemeColor.Selected => ("#333435", "#E3E3E2"),
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
