using Android.Content.Res;
using Android.Widget;
using Centello.App.Theming;
using Centello.Application;
using Google.Android.Material.Dialog;
using Google.Android.Material.Shape;
using Microsoft.Maui.Platform;

namespace Centello.App;

/// <summary>The dialog of the design on the native Material dialog. The Android theme of MAUI draws a square card in
/// the system font, so the surface, the type and the button colors are set here from the tokens.</summary>
internal sealed class MaterialConfirmation(IFontManager fonts) : IConfirmation
{
    private const double CornerRadius = 28;

    public async Task<bool> Ask(string title, string message, string confirm, string cancel, bool isDestructive = false)
    {
        if (Platform.CurrentActivity is not { } activity)
            return false;

        var answer = new TaskCompletionSource<bool>();
        var surface = new MaterialShapeDrawable();
        surface.SetCornerSize(activity.ToPixels(CornerRadius));
        surface.FillColor = ColorStateList.ValueOf(ThemeColors.Current(ThemeColor.SurfaceRaised).ToPlatform());
        var dialog = new MaterialAlertDialogBuilder(activity)
            .SetBackground(surface)!
            .SetTitle(title)!
            .SetMessage(message)!
            .SetPositiveButton(confirm, (_, _) => answer.TrySetResult(true))!
            .SetNegativeButton(cancel, (_, _) => answer.TrySetResult(false))!
            .Create();
        // Also covers the back button and a tap outside: both leave without confirming.
        dialog.DismissEvent += (_, _) => answer.TrySetResult(false);
        dialog.Show();
        Style(dialog.FindViewById<TextView>(Resource.Id.alertTitle), Fonts.Bold, 22, ThemeColor.Ink);
        Style(dialog.FindViewById<TextView>(Android.Resource.Id.Message), Fonts.Regular, 15, ThemeColor.Mute);
        Style(dialog.GetButton((int)Android.Content.DialogButtonType.Positive), Fonts.Bold, 16, isDestructive ? ThemeColor.Alert : ThemeColor.Ink);
        Style(dialog.GetButton((int)Android.Content.DialogButtonType.Negative), Fonts.Bold, 16, ThemeColor.Ink);
        return await answer.Task;
    }

    private void Style(TextView? text, string fontFamily, double size, ThemeColor color)
    {
        if (text is null)
            return;

        text.Typeface = fonts.GetTypeface(Microsoft.Maui.Font.OfSize(fontFamily, size));
        text.SetTextSize(Android.Util.ComplexUnitType.Sp, (float)size);
        text.SetTextColor(ThemeColors.Current(color).ToPlatform());
        text.LetterSpacing = 0;
    }
}
