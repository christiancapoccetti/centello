using Android.Content;
using Centello.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Centello.App;

/// <summary>Work a broadcast starts. The process stays alive until the work is done and the widgets show its result:
/// a write made here reaches the store listener, whose refresh must finish before Android may end the process.</summary>
internal static class ReceiverWork
{
    public static void Run(BroadcastReceiver receiver, Func<IServiceProvider, Task> work)
    {
        var pending = receiver.GoAsync();
        _ = Task.Run(async () =>
        {
            try
            {
                if (IPlatformApplication.Current?.Services is not { } services
                    || !await services.GetRequiredService<SqliteDatabaseStartup>().Run())
                    return;

                await work(services);
                await services.GetRequiredService<HomeScreen>().Settled();
            }
            catch
            {
            }
            finally
            {
                pending?.Finish();
            }
        });
    }
}
