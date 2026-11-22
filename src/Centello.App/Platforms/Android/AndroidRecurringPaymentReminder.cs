using Android.App;
using Android.Content;
using Centello.Application;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.Runtime.Versioning;

namespace Centello.App;

public sealed class AndroidRecurringPaymentReminder : IRecurringPaymentReminder
{
    private const string PreferencesName = "recurring-payment-reminders";
    private const string LastPostedDayKey = "last-posted-day";
    private const string LegacyIdsKey = "scheduled-ids";
    private const string AlarmAction = "centello.intent.action.RECURRING_PAYMENT_ALARM";
    private const string ChannelId = "upcoming-recurring-payments";
    private const int NotificationId = 1;

    public Task Resync(RecurringPaymentReminderSchedule schedule)
    {
        var context = Android.App.Application.Context;
        var notifications = context.GetSystemService(Context.NotificationService) as NotificationManager;
        var preferences = context.GetSharedPreferences(PreferencesName, FileCreationMode.Private)!;
        ClearLegacyReminders(context, preferences, notifications);

        var now = DateTime.Now;
        var next = schedule.NextDueAt;
        if (schedule.NotifyAt is { } notifyAt && notifyAt.Date == now.Date
            && notifications is not null && CanPost(notifications))
        {
            var dayKey = notifyAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var lastPostedDay = preferences.GetString(LastPostedDayKey, "") ?? "";
            if (lastPostedDay != dayKey)
            {
                if (notifyAt > now)
                    next = notifyAt;
                else
                {
                    Post(context, notifications, dayKey);
                    if (lastPostedDay.Length > 0)
                        notifications.Cancel(lastPostedDay, NotificationId);

                    preferences.Edit()!.PutString(LastPostedDayKey, dayKey)!.Apply();
                }
            }
        }

        if (next is { } local)
            Arm(context, new DateTimeOffset(local));
        else
            CancelAlarm(context, 0, AlarmIntent(context));

        return Task.CompletedTask;
    }

    private static bool CanPost(NotificationManager manager) => manager.AreNotificationsEnabled()
        && (!OperatingSystem.IsAndroidVersionAtLeast(26)
            || manager.GetNotificationChannel(ChannelId)?.Importance != NotificationImportance.None);

    private static void Post(Context context, NotificationManager manager, string dayKey)
    {
        var builder = OperatingSystem.IsAndroidVersionAtLeast(26)
            ? new Notification.Builder(context, EnsureChannel(manager))
            : new Notification.Builder(context);
        manager.Notify(dayKey, NotificationId, builder
            .SetContentTitle("Promemoria Centello")
            .SetContentText("Apri Centello per vedere i pagamenti di oggi.")
            .SetSmallIcon(Resource.Drawable.ic_stat_centello)
            .SetContentIntent(EntryLinks.Pending(context, new AppEntry.Screen.Today()))
            .SetVisibility(NotificationVisibility.Secret)
            .SetOnlyAlertOnce(true)
            .SetAutoCancel(true)
            .Build());
    }

    [SupportedOSPlatform("android26.0")]
    private static string EnsureChannel(NotificationManager manager)
    {
        if (manager.GetNotificationChannel(ChannelId) is null)
            manager.CreateNotificationChannel(new NotificationChannel(ChannelId, "Pagamenti in arrivo",
                NotificationImportance.Default));

        return ChannelId;
    }

    internal static void Arm(Context context, DateTimeOffset at)
    {
        var pending = PendingIntent.GetBroadcast(context, 0, AlarmIntent(context),
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
        if (pending is not null)
            Alarms(context)?.SetAndAllowWhileIdle(AlarmType.RtcWakeup, at.ToUnixTimeMilliseconds(), pending);
    }

    private static Intent AlarmIntent(Context context) =>
        new Intent(context, typeof(RecurringPaymentReminderReceiver)).SetAction(AlarmAction);

    private static AlarmManager? Alarms(Context context) =>
        context.GetSystemService(Context.AlarmService) as AlarmManager;

    private static void CancelAlarm(Context context, int requestCode, Intent intent)
    {
        var pending = PendingIntent.GetBroadcast(context, requestCode, intent,
            PendingIntentFlags.NoCreate | PendingIntentFlags.Immutable);
        if (pending is null)
            return;

        Alarms(context)?.Cancel(pending);
        pending.Cancel();
    }

    private static void ClearLegacyReminders(Context context, ISharedPreferences preferences,
        NotificationManager? notifications)
    {
        if (preferences.GetStringSet(LegacyIdsKey, null) is not { } ids)
            return;

        // An upgrade must remove old pending intents and notifications that contained the amount.
        foreach (var id in ids)
        {
            var requestCode = LegacyRequestCode(id);
            CancelAlarm(context, requestCode, new Intent(context, typeof(RecurringPaymentReminderReceiver)));
            notifications?.Cancel(requestCode);
        }

        preferences.Edit()!.Remove(LegacyIdsKey)!.Apply();
    }

    private static int LegacyRequestCode(string key)
    {
        unchecked
        {
            var hash = 17;
            foreach (var character in key)
                hash = hash * 31 + character;

            return hash & int.MaxValue;
        }
    }
}

[BroadcastReceiver(Name = "crc64d73031b65ce57f40.RecurringPaymentReminderReceiver", Enabled = true, Exported = false)]
[IntentFilter([Intent.ActionBootCompleted, Intent.ActionMyPackageReplaced, Intent.ActionTimeChanged,
    Intent.ActionTimezoneChanged])]
public sealed class RecurringPaymentReminderReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is null)
            return;

        TimeZoneInfo.ClearCachedData();
        // A successful pass replaces this alarm. A failed database pass can recover without reopening the app.
        AndroidRecurringPaymentReminder.Arm(context, DateTimeOffset.UtcNow.AddHours(1));
        ReceiverWork.Run(this, services => services.GetRequiredService<RecurringPaymentAutomation>()
            .ProcessDue(waitForCurrentPass: true));
    }
}
