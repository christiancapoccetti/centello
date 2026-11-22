using Android.Appwidget;
using Android.Content;
using Android.OS;
using Microsoft.Extensions.DependencyInjection;

namespace Centello.App;

/// <summary>Whatever the launcher asks (a widget added, restored after boot or an update, resized, or the last of a
/// kind removed), every widget is drawn again. A subclass only carries its attributes and its Render.</summary>
public abstract class CentelloWidgetProvider : AppWidgetProvider
{
    public override void OnUpdate(Context? context, AppWidgetManager? appWidgetManager, int[]? appWidgetIds) => Refresh();

    public override void OnAppWidgetOptionsChanged(Context? context, AppWidgetManager? appWidgetManager, int appWidgetId,
        Bundle? newOptions) => Refresh();

    public override void OnDisabled(Context? context) => Refresh();

    private void Refresh() =>
        ReceiverWork.Run(this, services => services.GetRequiredService<HomeScreen>().Refresh());
}
