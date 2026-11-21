using Android.App;
using Android.Content;
using Centesimo.Application;
using Uri = Android.Net.Uri;

namespace Centesimo.App;

internal static class WidgetLinks
{
    /// <summary>Opens the app on <paramref name="entry"/>. The link is the intent's data, so no two entries share a
    /// PendingIntent.</summary>
    public static PendingIntent Open(Context context, AppEntry entry)
    {
        var intent = new Intent(context, typeof(MainActivity))
            .SetData(Uri.Parse(entry.ToLink()))
            .AddFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);
        return PendingIntent.GetActivity(context, 0, intent, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent)!;
    }

    /// <summary>Opens the voice panel over the launcher, in its own task.</summary>
    public static PendingIntent Dictate(Context context) => PendingIntent.GetActivity(context, 0,
        new Intent(context, typeof(VoicePanelActivity)).AddFlags(ActivityFlags.NewTask),
        PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent)!;
}
