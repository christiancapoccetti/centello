namespace Centello.App.Controls;

internal static class PhoneLandscapeSize
{
    public static bool Matches(double width, double height) =>
        width >= 560 && height > 0 && height < 600 && width > height;
}
