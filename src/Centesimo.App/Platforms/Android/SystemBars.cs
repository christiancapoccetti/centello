using AndroidX.AppCompat.App;
using AndroidX.Core.View;
using AndroidX.Fragment.App;
using Centesimo.App.Theming;
using Centesimo.Application;
using Microsoft.Maui.Platform;

namespace Centesimo.App;

/// <summary>Keeps the status and navigation bars readable on the ground color: dark icons on the light ground,
/// light icons on the dark one. The Android theme alone always asks for light icons.</summary>
internal static class SystemBars
{
    /// <summary>Applies to the activity and to every open modal page: MAUI shows a modal page in a dialog, which
    /// has a window of its own.</summary>
    public static void Apply(AppCompatActivity activity, bool isDark)
    {
        var modalWindows = activity.SupportFragmentManager.Fragments
            .OfType<DialogFragment>()
            .Select(fragment => fragment.Dialog?.Window);
        foreach (var window in modalWindows.Prepend(activity.Window))
            Apply(window, isDark);
    }

    private static void Apply(Android.Views.Window? window, bool isDark)
    {
        if (window is null || WindowCompat.GetInsetsController(window, window.DecorView) is not { } controller)
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
