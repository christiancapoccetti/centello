using Android.App;
using AndroidX.Core.View;
using Centesimo.App.Theming;
using Microsoft.Maui.Platform;

namespace Centesimo.App;

/// <summary>Keeps the status and navigation bars readable on the ground color: dark icons on the light ground,
/// light icons on the dark one. The Android theme alone always asks for light icons.</summary>
internal static class SystemBars
{
    public static void Apply(Activity activity, bool isDark)
    {
        if (activity.Window is not { } window ||
            WindowCompat.GetInsetsController(window, window.DecorView) is not { } controller)
            return;

        controller.AppearanceLightStatusBars = !isDark;
        controller.AppearanceLightNavigationBars = !isDark;
        // Android 15 and later draw the app edge to edge and ignore bar colors: the page ground shows through.
        if (OperatingSystem.IsAndroidVersionAtLeast(35))
            return;

        var ground = ThemeColors.Resolve(ThemeColor.Ground, isDark).ToPlatform();
        window.SetStatusBarColor(ground);
        window.SetNavigationBarColor(ground);
    }
}
