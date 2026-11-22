using Android.App;
using Android.Content;
using Microsoft.Extensions.DependencyInjection;

namespace Centello.App;

/// <summary>Arms a non-wakeup alarm for the next local midnight: it fires when the device next wakes, when someone can
/// look at the launcher. Arming again replaces it.</summary>
internal static class Midnight
{
    public static void Arm(Context context)
    {
        // The zone the runtime cached may be the one before a time zone change.
        TimeZoneInfo.ClearCachedData();
        var midnight = new DateTimeOffset(DateTime.Today.AddDays(1));
        if (Pending(context, PendingIntentFlags.UpdateCurrent) is not { } pending)
            return;

        Manager(context)?.Set(AlarmType.Rtc, midnight.ToUnixTimeMilliseconds(), pending);
    }

    public static void Cancel(Context context)
    {
        if (Pending(context, PendingIntentFlags.NoCreate) is not { } pending)
            return;

        Manager(context)?.Cancel(pending);
        pending.Cancel();
    }

    private static AlarmManager? Manager(Context context) => context.GetSystemService(Context.AlarmService) as AlarmManager;

    private static PendingIntent? Pending(Context context, PendingIntentFlags flag) => PendingIntent.GetBroadcast(
        context, 0, new Intent(context, typeof(WidgetMidnightReceiver)), flag | PendingIntentFlags.Immutable);
}

[BroadcastReceiver(Name = "crc64d73031b65ce57f40.WidgetMidnightReceiver", Exported = false)]
[IntentFilter([Intent.ActionTimeChanged, Intent.ActionTimezoneChanged])]
public sealed class WidgetMidnightReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent) =>
        ReceiverWork.Run(this, services => services.GetRequiredService<HomeScreen>().Refresh());
}
