using AndroidX.Lifecycle;
using Centello.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Centello.App;

internal sealed class AiAccessForegroundObserver(IServiceProvider services) : Java.Lang.Object, ILifecycleEventObserver
{
    public async void OnStateChanged(ILifecycleOwner source, Lifecycle.Event @event)
    {
        if (@event != Lifecycle.Event.OnStart)
            return;

        try
        {
            await services.GetRequiredService<AiAccess>().BeginSession().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            global::Android.Util.Log.Warn("Centello", $"Could not start the AI access session: {exception.GetType().Name}");
        }
    }
}
