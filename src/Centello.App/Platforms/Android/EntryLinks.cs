using Android.App;
using Android.Content;
using Centello.Application;
using Uri = Android.Net.Uri;

namespace Centello.App;

/// <summary>The only place that turns an entry into what Android starts: the widgets, the launcher shortcuts and the
/// hand-off of the panels. The link is the intent's data, so no two entries share a PendingIntent.</summary>
internal static class EntryLinks
{
    public static Intent Intent(Context context, AppEntry entry)
    {
        var intent = new Intent(Android.Content.Intent.ActionView, Uri.Parse(entry.ToLink()));
        if (entry is AppEntry.Panel)
            return intent
                .SetClass(context, typeof(PanelActivity))
                .AddFlags(ActivityFlags.NewTask);

        return intent
            .SetClass(context, typeof(MainActivity))
            .AddFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);
    }

    public static PendingIntent Pending(Context context, AppEntry entry) =>
        PendingIntent.GetActivity(context, 0, Intent(context, entry), PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent)!;
}
