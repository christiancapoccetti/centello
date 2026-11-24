using Android.Appwidget;
using Android.Content;
using Android.OS;
using Android.Util;
using Android.Widget;
using Centello.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Centello.App;

/// <summary>The one owner of what the home screen shows from the app: the widgets and the launcher's long-press menu.
/// The store asks it to refresh after every commit, the midnight alarm after midnight, the launcher when a widget is
/// added, resized or restored. A refresh reads the store in its own scope, republishes the menu when it changed and
/// redraws every installed widget, so it can run any number of times; requests that arrive while one runs are folded
/// into one more run, which reads the store after them. Runs start on the thread pool, where the menu may be
/// published.</summary>
public sealed class HomeScreen : IStoreChangeListener
{
    internal const string LogTag = "Centello.Widgets";

    internal static readonly WidgetSpec[] Widgets =
    [
        new(typeof(AddWidget), AddWidget.Render),
        new(typeof(ShortcutsWidget), ShortcutsWidget.Render),
        new(typeof(MonthWidget), MonthWidget.Render),
        new(typeof(RhythmWidget), RhythmWidget.Render),
        new(typeof(CategoryBudgetWidget), CategoryBudgetWidget.Render)
    ];

    private readonly IServiceScopeFactory _scopes;
    private readonly IFontManager _fonts;
    private readonly RefreshCoalescer _refreshes;
    private IReadOnlyList<LauncherShortcut> _published = [];

    public HomeScreen(IServiceScopeFactory scopes, IFontManager fonts)
    {
        _scopes = scopes;
        _fonts = fonts;
        _refreshes = new RefreshCoalescer(Render);
    }

    public void StoreChanged() => _ = Refresh();

    /// <summary>Completes once the widgets show the store as it was when this was called.</summary>
    public Task Refresh() => _refreshes.Request();

    /// <summary>Completes when no refresh waits or runs. A receiver awaits it before it lets the process go.</summary>
    public Task Settled() => _refreshes.Settled();

    private async Task Render()
    {
        try
        {
            await Draw();
        }
        catch (Exception exception)
        {
            Log.Warn(LogTag, $"Refresh failed: {exception}");
        }
    }

    private async Task Draw()
    {
        var context = Android.App.Application.Context;
        var manager = AppWidgetManager.GetInstance(context)!;
        var installed = Widgets
            .SelectMany(spec => manager.GetAppWidgetIds(spec.Component(context))!.Select(id => (spec, id)))
            .ToList();
        if (await Read() is { } data)
        {
            var art = new WidgetArt(context, _fonts);
            PublishMenu(context, art, LauncherShortcuts.Of(data));
            foreach (var (spec, id) in installed)
                Push(context, manager, art, data, spec, id);
        }

        if (installed.Count == 0)
            Midnight.Cancel(context);
        else
            Midnight.Arm(context);
    }

    // Only a published list is remembered, so a refused or failed publish is retried by the next refresh.
    private void PublishMenu(Context context, WidgetArt art, IReadOnlyList<LauncherShortcut> shortcuts)
    {
        if (shortcuts.SequenceEqual(_published))
            return;

        try
        {
            if (!LauncherMenu.Publish(context, art, shortcuts))
            {
                Log.Warn(LogTag, "Launcher menu not published: Android refused the update.");
                return;
            }

            _published = shortcuts;
        }
        catch (Exception exception)
        {
            Log.Warn(LogTag, $"Launcher menu failed: {exception}");
        }
    }

    private async Task<HomeScreenData?> Read()
    {
        await using var scope = _scopes.CreateAsyncScope();
        var shortcuts = await scope.ServiceProvider.GetRequiredService<CategoryChoiceService>().Shortcuts();
        if (shortcuts.IsFailure)
            return null;

        var glance = await scope.ServiceProvider.GetRequiredService<HomeOverviewService>().Glance();
        return glance.IsFailure ? null : new HomeScreenData(shortcuts.Value, glance.Value);
    }

    private static void Push(Context context, AppWidgetManager manager, WidgetArt art, HomeScreenData data,
        WidgetSpec spec, int id)
    {
        try
        {
            var options = manager.GetAppWidgetOptions(id);
            var info = manager.GetAppWidgetInfo(id);
            var size = WidgetSize.Of(options, info);
            var landscapeSize = WidgetSize.Of(options, info, landscape: true);
            manager.UpdateAppWidget(id, spec.Render(new WidgetFrame(context, art, data, size, landscapeSize)));
        }
        catch (Exception exception)
        {
            Log.Warn(LogTag, $"Widget {id} failed: {exception}");
        }
    }
}

internal sealed record WidgetSpec(Type Provider, Func<WidgetFrame, RemoteViews> Render)
{
    public ComponentName Component(Context context) => new(context, Java.Lang.Class.FromType(Provider));
}

internal sealed record WidgetFrame(Context Context, WidgetArt Art, HomeScreenData Data, WidgetSize Size, WidgetSize LandscapeSize)
{
    private const int SidePaddingDp = 16;

    public float ContentWidthDp => Size.WidthDp - 2 * SidePaddingDp;
}

/// <summary>The size the launcher gives one widget, in dp. Portrait is narrow and tall; landscape is wide and short.</summary>
internal readonly record struct WidgetSize(int WidthDp, int HeightDp)
{
    public static WidgetSize Of(Bundle? options, AppWidgetProviderInfo? info, bool landscape = false)
    {
        var density = Android.App.Application.Context.Resources!.DisplayMetrics!.Density;
        var width = landscape ? AppWidgetManager.OptionAppwidgetMaxWidth : AppWidgetManager.OptionAppwidgetMinWidth;
        var height = landscape ? AppWidgetManager.OptionAppwidgetMinHeight : AppWidgetManager.OptionAppwidgetMaxHeight;
        return new WidgetSize(
            Known(options?.GetInt(width), info?.MinWidth, density),
            Known(options?.GetInt(height), info?.MinHeight, density));
    }

    // Some launchers report zero: the size the provider declared is then the best answer.
    private static int Known(int? reported, int? declaredPx, float density) =>
        reported is > 0 ? reported.Value : (int)Math.Round((declaredPx ?? 0) / density);
}
