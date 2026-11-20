using Android.Views;
using Android.Widget;
using Centesimo.App.Theming;
using Google.Android.Material.BottomNavigation;
using Google.Android.Material.Snackbar;
using Microsoft.Maui.Platform;
using View = Android.Views.View;

namespace Centesimo.App;

/// <summary>The native snackbar in the inverse colors of the design. It sits above the view the page in front marks
/// with SnackbarAnchor, or above the tab bar when the page marks none or the marked view is hidden.</summary>
internal sealed class MaterialSnackbar(IFontManager fonts) : ISnackbar
{
    public void Show(string message)
    {
        if (Platform.CurrentActivity?.FindViewById(Android.Resource.Id.Content) is not ViewGroup root)
            return;

        var snackbar = Snackbar.Make(root, message, Snackbar.LengthLong);
        snackbar.SetBackgroundTint(ThemeColors.Current(ThemeColor.Ink).ToPlatform());
        snackbar.SetTextColor(ThemeColors.Current(ThemeColor.Ground).ToPlatform());
        if (snackbar.View is ViewGroup layout && First<TextView>(layout) is { } text)
        {
            text.Typeface = fonts.GetTypeface(Microsoft.Maui.Font.OfSize(Fonts.Regular, 14));
            text.LetterSpacing = 0;
        }

        if ((MarkedAnchor() ?? First<BottomNavigationView>(root, bar => bar.IsShown)) is { } anchor)
        {
            snackbar.SetAnchorView(anchor);
            // An editor often closes with the keyboard still up, which holds the anchor high for a moment.
            // Following the anchor brings the snackbar down with it instead of leaving it in the middle of the page.
            snackbar.AnchorViewLayoutListenerEnabled = true;
        }

        snackbar.Show();
    }

    private static View? MarkedAnchor() => Shell.Current?.CurrentPage?
        .GetVisualTreeDescendants()
        .OfType<VisualElement>()
        .FirstOrDefault(view => SnackbarAnchor.GetIsAnchor(view) && view.IsVisible)?
        .Handler?.PlatformView as View;

    private static T? First<T>(ViewGroup parent, Func<T, bool>? matches = null) where T : View
    {
        for (var index = 0; index < parent.ChildCount; index++)
        {
            var child = parent.GetChildAt(index);
            if (child is T candidate && (matches?.Invoke(candidate) ?? true))
                return candidate;
            if (child is ViewGroup group && First(group, matches) is { } found)
                return found;
        }

        return null;
    }
}
