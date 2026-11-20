using Android.Appwidget;
using Android.Content;
using Android.OS;
using Android.Util;
using Android.Widget;
using Centesimo.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Centesimo.App;

/// <summary>The one owner of what the home screen widgets show. The store asks it to refresh after every commit, the
/// midnight alarm after midnight, the launcher when a widget is added, resized or restored. A refresh reads the store
/// in its own scope and redraws every installed widget, so it can run any number of times; requests that arrive while
/// one runs are folded into one more run, which reads the store after them.</summary>
public sealed class HomeScreenWidgets : IStoreChangeListener
{
    private const string LogTag = "Centesimo.Widgets";

    internal static readonly WidgetSpec[] Widgets =
    [
        new(typeof(AddWidget), AddWidget.Render)
    ];

    private readonly IServiceScopeFactory _scopes;
    private readonly IFontManager _fonts;
    private readonly RefreshCoalescer _refreshes;

    public HomeScreenWidgets(IServiceScopeFactory scopes, IFontManager fonts)
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
        if (installed.Count == 0)
        {
            Midnight.Cancel(context);
            return;
        }

        if (await Read() is { } data)
        {
            var art = new WidgetArt(context, _fonts);
            foreach (var (spec, id) in installed)
                Push(context, manager, art, data, spec, id);
        }

        Midnight.Arm(context);
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
            var size = WidgetSize.Of(manager.GetAppWidgetOptions(id), manager.GetAppWidgetInfo(id));
            manager.UpdateAppWidget(id, spec.Render(new WidgetFrame(context, art, data, size)));
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

internal sealed record HomeScreenData(IReadOnlyList<CategoryShortcut> Shortcuts, MonthGlance Month)
{
    /// <summary>False until an active category exists: every add action then opens Oggi, where the first run starts.</summary>
    public bool CanAdd => Shortcuts.Count > 0;

    public AppEntry AddEntry(Guid? categoryId = null) =>
        CanAdd ? new AppEntry.NewExpense(categoryId) : new AppEntry.Today();
}

internal sealed record WidgetFrame(Context Context, WidgetArt Art, HomeScreenData Data, WidgetSize Size)
{
    private const int SidePaddingDp = 16;

    public float ContentWidthDp => Size.WidthDp - 2 * SidePaddingDp;
}

/// <summary>The size the launcher gives one widget, in dp. Portrait reads the minimum width and the maximum height.</summary>
internal readonly record struct WidgetSize(int WidthDp, int HeightDp)
{
    public static WidgetSize Of(Bundle? options, AppWidgetProviderInfo? info)
    {
        var density = Android.App.Application.Context.Resources!.DisplayMetrics!.Density;
        return new WidgetSize(
            Known(options?.GetInt(AppWidgetManager.OptionAppwidgetMinWidth), info?.MinWidth, density),
            Known(options?.GetInt(AppWidgetManager.OptionAppwidgetMaxHeight), info?.MinHeight, density));
    }

    // Some launchers report zero: the size the provider declared is then the best answer.
    private static int Known(int? reported, int? declaredPx, float density) =>
        reported is > 0 ? reported.Value : (int)Math.Round((declaredPx ?? 0) / density);
}
