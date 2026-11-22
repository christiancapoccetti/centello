using AndroidX.AppCompat.App;
using AndroidX.Core.View;
using AndroidX.Fragment.App;
using Centello.App.Theming;
using Centello.Application;
using Microsoft.Maui.Platform;

namespace Centello.App;

/// <summary>The single owner of what the status and navigation bars look like. The theme decides: dark icons on the
/// light ground, light icons on the dark one, since the Android theme alone always asks for light icons. A recap card
/// can hold the activity's bars on its own surfaces, and the theme policy comes back with Release. A held pair
/// survives a theme flip and a modal push, because both go through Apply.</summary>
internal static class SystemBars
{
    private static (RecapSurface Top, RecapSurface Bottom)? _held;

    /// <summary>Applies to the activity and to every open modal page: MAUI shows a modal page in a dialog, which
    /// has a window of its own and always follows the theme.</summary>
    public static void Apply(AppCompatActivity activity, bool isDark)
    {
        var modalWindows = activity.SupportFragmentManager.Fragments
            .OfType<DialogFragment>()
            .Select(fragment => fragment.Dialog?.Window);
        foreach (var window in modalWindows)
            Apply(window, isDark);

        if (_held is { } held)
            Apply(activity.Window, held.Top, held.Bottom, isDark);
        else
            Apply(activity.Window, isDark);
    }

    public static void Hold(AppCompatActivity activity, RecapSurface top, RecapSurface bottom, bool isDark)
    {
        _held = (top, bottom);
        Apply(activity, isDark);
    }

    public static void Release(AppCompatActivity activity, bool isDark)
    {
        _held = null;
        Apply(activity, isDark);
    }

    private static void Apply(Android.Views.Window? window, bool isDark) =>
        Apply(window, RecapSurface.Ground, RecapSurface.Ground, isDark);

    private static void Apply(Android.Views.Window? window, RecapSurface top, RecapSurface bottom, bool isDark)
    {
        if (window is null || WindowCompat.GetInsetsController(window, window.DecorView) is not { } controller)
            return;

        controller.AppearanceLightStatusBars = HasDarkIcons(top, isDark);
        controller.AppearanceLightNavigationBars = HasDarkIcons(bottom, isDark);
        // Android 15 and later draw the app edge to edge and ignore bar colors: the page shows through.
        if (OperatingSystem.IsAndroidVersionAtLeast(35))
            return;

        window.SetStatusBarColor(ColorOf(top, isDark).ToPlatform());
        window.SetNavigationBarColor(ColorOf(bottom, isDark).ToPlatform());
    }

    // A palette surface is a pastel in both schemes: only the ground follows the theme.
    private static bool HasDarkIcons(RecapSurface surface, bool isDark) => !surface.IsGround || !isDark;

    private static Color ColorOf(RecapSurface surface, bool isDark) => surface.IsGround
        ? ThemeColors.Resolve(ThemeColor.Ground, isDark)
        : ThemeColors.ResolvePalette(surface.PaletteKey, isDark);
}
