using Android.App;
using Android.Content;
using Android.OS;
using Centesimo.Application;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.Runtime.Versioning;

namespace Centesimo.App;

public sealed class AndroidRecurringPaymentReminder : IRecurringPaymentReminder
{
    private const string PreferencesName = "recurring-payment-reminders";
    private const string ScheduledIdsKey = "scheduled-ids";

    public Task Resync(IReadOnlyList<UpcomingRecurringPaymentReminder> reminders)
    {
        var context = Android.App.Application.Context;
        CancelPreviouslyScheduled(context);
        var ids = reminders.Select(item => item.NotificationId).ToArray();
        foreach (var reminder in reminders)
            Schedule(context, reminder);

        context.GetSharedPreferences(PreferencesName, FileCreationMode.Private)!
            .Edit()!
            .PutStringSet(ScheduledIdsKey, ids)!
            .Apply();
        return Task.CompletedTask;
    }

    private static void Schedule(Android.Content.Context context,
        UpcomingRecurringPaymentReminder reminder)
    {
        var trigger = reminder.DueOn.ToDateTime(new TimeOnly(9, 0)).ToUniversalTime();
        if (trigger <= DateTime.UtcNow)
            return;

        var intent = CreateIntent(context, reminder.NotificationId, reminder.AmountCents, reminder.DueOn);
        var pendingIntent = PendingIntent.GetBroadcast(context, GetNotificationId(reminder.NotificationId), intent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
        if (pendingIntent is null)
            return;

        var manager = context.GetSystemService(Android.Content.Context.AlarmService) as AlarmManager;
        manager?.SetAndAllowWhileIdle(AlarmType.RtcWakeup,
            new DateTimeOffset(trigger).ToUnixTimeMilliseconds(), pendingIntent);
    }

    private static void CancelPreviouslyScheduled(Android.Content.Context context)
    {
        var preferences = context.GetSharedPreferences(PreferencesName, FileCreationMode.Private);
        var ids = preferences?.GetStringSet(ScheduledIdsKey, null) ?? [];
        var manager = context.GetSystemService(Android.Content.Context.AlarmService) as AlarmManager;
        var notifications = context.GetSystemService(Android.Content.Context.NotificationService) as NotificationManager;
        foreach (var id in ids)
        {
            var intent = CreateIntent(context, id, 0, DateOnly.MinValue);
            var pendingIntent = PendingIntent.GetBroadcast(context, GetNotificationId(id), intent,
                PendingIntentFlags.NoCreate | PendingIntentFlags.Immutable);
            if (pendingIntent is not null)
            {
                manager?.Cancel(pendingIntent);
                pendingIntent.Cancel();
            }

            notifications?.Cancel(GetNotificationId(id));
        }
    }

    private static Intent CreateIntent(Android.Content.Context context, string id, long amountCents,
        DateOnly dueOn) => new Intent(context, typeof(RecurringPaymentReminderReceiver))
        .PutExtra(RecurringPaymentReminderReceiver.NotificationIdExtra, id)
        .PutExtra(RecurringPaymentReminderReceiver.AmountCentsExtra, amountCents)
        .PutExtra(RecurringPaymentReminderReceiver.DueOnExtra, dueOn.ToString("O", CultureInfo.InvariantCulture));

    internal static int GetNotificationId(string key)
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

[BroadcastReceiver(Enabled = true, Exported = false)]
public sealed class RecurringPaymentReminderReceiver : BroadcastReceiver
{
    public const string NotificationIdExtra = "notification-id";
    public const string AmountCentsExtra = "amount-cents";
    public const string DueOnExtra = "due-on";
    private const string ChannelId = "upcoming-recurring-payments";

    public override void OnReceive(Android.Content.Context? context, Intent? intent)
    {
        if (context is null || intent is null)
            return;

        var id = intent.GetStringExtra(NotificationIdExtra);
        if (id is null)
            return;

        var amountCents = intent.GetLongExtra(AmountCentsExtra, 0);
        var dueOnText = intent.GetStringExtra(DueOnExtra);
        var manager = context.GetSystemService(Android.Content.Context.NotificationService) as NotificationManager;
        if (manager is null || !DateOnly.TryParseExact(dueOnText, "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dueOn))
        {
            ProcessDueInBackground();
            return;
        }

        if (OperatingSystem.IsAndroidVersionAtLeast(26))
            CreateChannel(manager);

        var builder = OperatingSystem.IsAndroidVersionAtLeast(26)
            ? CreateChannelBuilder(context)
            : new Notification.Builder(context);
        manager.Notify(AndroidRecurringPaymentReminder.GetNotificationId(id), builder
            .SetContentTitle("Pagamento in arrivo")
            .SetContentText(ItalianFormat.PaymentDue(amountCents, dueOn))
            .SetSmallIcon(Resource.Drawable.ic_stat_centesimo)
            .SetAutoCancel(true)
            .Build());

        ProcessDueInBackground();
    }

    private void ProcessDueInBackground() =>
        ReceiverWork.Run(this, services => services.GetRequiredService<RecurringPaymentAutomation>().ProcessDue());

    [SupportedOSPlatform("android26.0")]
    private static void CreateChannel(NotificationManager manager) => manager.CreateNotificationChannel(
        new NotificationChannel(ChannelId, "Pagamenti in arrivo", NotificationImportance.Default));

    [SupportedOSPlatform("android26.0")]
    private static Notification.Builder CreateChannelBuilder(Android.Content.Context context) =>
        new(context, ChannelId);
}
