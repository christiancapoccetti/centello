using Android.App;
using Android.Content;
using Centello.Application;
using System.Runtime.Versioning;

namespace Centello.App;

public sealed class AndroidRecapNoticeBoard : IRecapNoticeBoard
{
    private const string ChannelId = "riepiloghi";
    private const int NotificationId = 1;

    public bool CanPost => Manager(Context) is { } manager && CanPostOn(manager);

    public Task<bool> Post(RecapNotice notice)
    {
        var context = Context;
        if (Manager(context) is not { } manager || !CanPostOn(manager))
            return Task.FromResult(false);

        var builder = OperatingSystem.IsAndroidVersionAtLeast(26)
            ? new Notification.Builder(context, EnsureChannel(manager))
            : new Notification.Builder(context);
        // One id for every recap: a newer one replaces a notice left unread.
        manager.Notify(NotificationId, builder
            .SetContentTitle(notice.Title)
            .SetContentText(notice.Text)
            .SetSmallIcon(Resource.Drawable.ic_stat_centello)
            .SetContentIntent(EntryLinks.Pending(context, notice.Entry))
            .SetAutoCancel(true)
            .SetVisibility(NotificationVisibility.Secret)
            .Build());
        return Task.FromResult(true);
    }

    public Task ArmAt(DateTime local)
    {
        var context = Context;
        if (Pending(context, PendingIntentFlags.UpdateCurrent) is { } pending)
            Alarms(context)?.SetAndAllowWhileIdle(AlarmType.RtcWakeup,
                new DateTimeOffset(local).ToUnixTimeMilliseconds(), pending);

        return Task.CompletedTask;
    }

    public Task Disarm()
    {
        var context = Context;
        Manager(context)?.Cancel(NotificationId);
        if (Pending(context, PendingIntentFlags.NoCreate) is not { } pending)
            return Task.CompletedTask;

        Alarms(context)?.Cancel(pending);
        pending.Cancel();
        return Task.CompletedTask;
    }

    private static bool CanPostOn(NotificationManager manager) => manager.AreNotificationsEnabled()
        && (!OperatingSystem.IsAndroidVersionAtLeast(26)
            || manager.GetNotificationChannel(ChannelId)?.Importance != NotificationImportance.None);

    private static Context Context => Android.App.Application.Context;

    private static NotificationManager? Manager(Context context) =>
        context.GetSystemService(Context.NotificationService) as NotificationManager;

    private static AlarmManager? Alarms(Context context) =>
        context.GetSystemService(Context.AlarmService) as AlarmManager;

    [SupportedOSPlatform("android26.0")]
    private static string EnsureChannel(NotificationManager manager)
    {
        if (manager.GetNotificationChannel(ChannelId) is null)
            manager.CreateNotificationChannel(new NotificationChannel(ChannelId, "Riepiloghi",
                NotificationImportance.Default));

        return ChannelId;
    }

    private static PendingIntent? Pending(Context context, PendingIntentFlags flag) => PendingIntent.GetBroadcast(
        context, 0, new Intent(context, typeof(RecapNoticeReceiver)), flag | PendingIntentFlags.Immutable);
}
