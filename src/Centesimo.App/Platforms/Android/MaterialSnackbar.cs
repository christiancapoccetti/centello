using Android.Views;
using Android.Widget;
using Centesimo.App.Theming;
using Google.Android.Material.BottomNavigation;
using Google.Android.Material.Snackbar;
using Microsoft.Maui.Platform;
using View = Android.Views.View;

namespace Centesimo.App;

/// <summary>The native snackbar in the inverse colors of the design. It sits above the view the page in front marks
/// with SnackbarAnchor, or above the tab bar when the page marks none.</summary>
internal sealed class MaterialSnackbar(IFontManager fonts) : ISnackbar
{
    public void Show(string message)
    {
        if (Platform.CurrentActivity?.FindViewById(Android.Resource.Id.Content) is not ViewGroup root)
            return;

        var snackbar = Snackbar.Make(root, message, Snackbar.LengthLong);
        snackbar.SetBackgroundTint(ThemeColors.Current(ThemeColor.Ink).ToPlatform());
        snackbar.SetTextColor(ThemeColors.Current(ThemeColor.Ground).ToPlatform());
        if (snackbar.View.FindViewById<TextView>(Resource.Id.snackbar_text) is { } text)
            text.Typeface = fonts.GetTypeface(Microsoft.Maui.Font.OfSize(Fonts.Regular, 14));
        if ((MarkedAnchor() ?? TabBar(root)) is { } anchor)
            snackbar.SetAnchorView(anchor);

        snackbar.Show();
    }

    private static View? MarkedAnchor() => Shell.Current?.CurrentPage?
        .GetVisualTreeDescendants()
        .OfType<VisualElement>()
        .FirstOrDefault(SnackbarAnchor.GetIsAnchor)?
        .Handler?.PlatformView as View;

    private static View? TabBar(ViewGroup parent)
    {
        for (var index = 0; index < parent.ChildCount; index++)
        {
            var child = parent.GetChildAt(index);
            if (child is BottomNavigationView { IsShown: true })
                return child;
            if (child is ViewGroup group && TabBar(group) is { } found)
                return found;
        }

        return null;
    }
}
