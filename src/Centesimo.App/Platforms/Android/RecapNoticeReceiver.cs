using Android.App;
using Android.Content;
using Centesimo.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Centesimo.App;

/// <summary>The alarm itself and every event that may have lost or moved it: all end in one idempotent sync.</summary>
[BroadcastReceiver(Exported = false)]
[IntentFilter([Intent.ActionBootCompleted, Intent.ActionMyPackageReplaced, Intent.ActionTimeChanged,
    Intent.ActionTimezoneChanged])]
public sealed class RecapNoticeReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        // The zone the runtime cached may be the one before a time zone change.
        TimeZoneInfo.ClearCachedData();
        ReceiverWork.Run(this, services => services.GetRequiredService<RecapNoticeAutomation>().Sync());
    }
}
