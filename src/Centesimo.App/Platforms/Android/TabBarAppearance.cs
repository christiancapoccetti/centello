using Android.Graphics.Drawables;
using Centesimo.App.Theming;
using Centesimo.Application;
using Google.Android.Material.BottomNavigation;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;
using Microsoft.Maui.Platform;

namespace Centesimo.App;

/// <summary>The Shell renderer with the tab bar of the design. Shell exposes only the tab colors, so the rest is set
/// on the Android view: no shadow, a hairline above the bar, and labels in the text font at one size.</summary>
internal sealed class CentesimoShellRenderer : ShellRenderer
{
    protected override IShellBottomNavViewAppearanceTracker CreateBottomNavViewAppearanceTracker(ShellItem shellItem) =>
        new TabBarAppearance(this, shellItem);
}

internal sealed class TabBarAppearance(IShellContext shellContext, ShellItem shellItem)
    : ShellBottomNavViewAppearanceTracker(shellContext, shellItem)
{
    private const double BarHeight = 69;
    // MAUI draws a glyph into a bitmap as tall as the font line, so a 24dp box would show it smaller than 24dp.
    private const double IconSize = 28;
    private const double IconTop = 11;
    private const double LabelBottom = 16;

    public override void SetAppearance(BottomNavigationView bottomView, IShellAppearanceElement appearance)
    {
        base.SetAppearance(bottomView, appearance);
        bottomView.Elevation = 0;
        bottomView.SetMinimumHeight(Pixels(bottomView, BarHeight));
        bottomView.ItemIconSize = Pixels(bottomView, IconSize);
        bottomView.ItemPaddingTop = Pixels(bottomView, IconTop);
        bottomView.ItemPaddingBottom = Pixels(bottomView, LabelBottom);
        // The stock bar draws the selected label larger and in the system font.
        bottomView.ItemTextAppearanceActive = Resource.Style.CentesimoTabLabel;
        bottomView.ItemTextAppearanceInactive = Resource.Style.CentesimoTabLabel;
    }

    private static int Pixels(BottomNavigationView bottomView, double dp) => (int)Math.Round(bottomView.Context.ToPixels(dp));

    // Shell calls this again when the theme changes, so the hairline can read the current token.
    protected override void SetBackgroundColor(BottomNavigationView bottomView, Color color)
    {
        var ground = (color ?? ThemeColors.Current(ThemeColor.Ground)).ToPlatform();
        var hairline = ThemeColors.Current(ThemeColor.Hairline).ToPlatform();
        var background = new LayerDrawable([new ColorDrawable(hairline), new ColorDrawable(ground)]);
        background.SetLayerInset(1, 0, (int)Math.Ceiling(bottomView.Context.ToPixels(1)), 0, 0);
        bottomView.Background = background;
    }
}
