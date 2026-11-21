using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Util;
using Android.Widget;
using Centesimo.App.Theming;
using Centesimo.Application;

namespace Centesimo.App;

[BroadcastReceiver(Label = "Il ritmo", Exported = false)]
[IntentFilter([AppWidgetManager.ActionAppwidgetUpdate])]
[MetaData(AppWidgetManager.MetaDataAppwidgetProvider, Resource = "@xml/widget_rhythm_info")]
public sealed class RhythmWidget : CentesimoWidgetProvider
{
    // The layout's margin and padding on both sides, then the rows above the chart: the month and the total, the
    // budget and the chip. The chart takes what is left.
    private const float FrameWidthDp = 2 * 8 + 18 + 16;
    private const float FrameHeightDp = 2 * 8 + 14 + 54 + 6 + 10;
    private const float ChipHeightDp = 24;
    private const float MinChartHeightDp = 50;
    private const float MaxChartHeightDp = 100;
    private const float MonthDp = 14;
    private const float TotalDp = 44;
    private const float EuroDp = 26;
    private const float BudgetDp = 13;
    private const float PlusDp = 26;
    private const float ChipChromeDp = 4 + 16 + 3 + 8;
    private const float ChipGapDp = 10;

    private static readonly DayNightSlot Budget = new(Resource.Id.budget, Resource.Id.budget_day, Resource.Id.budget_night);
    private static readonly DayNightSlot Chart = new(Resource.Id.chart, Resource.Id.chart_day, Resource.Id.chart_night);

    internal static RemoteViews Render(WidgetFrame frame)
    {
        var month = frame.Data.Month;
        var art = frame.Art;
        var width = frame.Size.WidthDp - FrameWidthDp;
        var views = new RemoteViews(frame.Context.PackageName, Resource.Layout.widget_rhythm);
        views.SetImageViewBitmap(Resource.Id.month, art.Text(month.Month, new WidgetTextStyle(Fonts.Bold, MonthDp)));
        views.SetImageViewBitmap(Resource.Id.year, art.Text(month.Year, new WidgetTextStyle(Fonts.Regular, MonthDp)));
        views.SetImageViewBitmap(Resource.Id.total, art.Total(month.Total, TotalDp, EuroDp, width));
        views.SetImageViewBitmap(Resource.Id.fab_glyph, art.Glyph(MaterialSymbols.Add, PlusDp));
        views.SetOnClickPendingIntent(Resource.Id.fab, EntryLinks.Pending(frame.Context, frame.Data.Admit(new AppEntry.Screen.NewExpense(null))));
        var budget = art.Sentence(month.Budget.Lead, month.Budget.Emphasis, month.Budget.Tail,
            width - ChipReserve(art, month.Comparison), BudgetDp);
        Budget.Show(views, budget);
        var rowHeight = Math.Max(ChipHeightDp, budget.Day.Height * 160f / budget.Day.Density);
        var chartHeight = Math.Clamp(frame.Size.HeightDp - FrameHeightDp - rowHeight, MinChartHeightDp, MaxChartHeightDp);
        WidgetViews.Comparison(views, art, month.Comparison, month.Comparison?.Text);
        Chart.Show(views, art.Chart(month.Chart, width, chartHeight));
        views.SetOnClickPendingIntent(Resource.Id.root, EntryLinks.Pending(frame.Context, new AppEntry.Screen.Today()));
        views.SetContentDescription(Resource.Id.root, month.Description);
        Log.Debug(HomeScreenWidgets.LogTag, $"Il ritmo RemoteViews memory estimate: {MemoryEstimate(views)}");
        return views;
    }

    // estimateMemoryUsage is hidden API: the log says so when the platform refuses it.
    private static string MemoryEstimate(RemoteViews views)
    {
        try
        {
            var bytes = views.Class.GetMethod("estimateMemoryUsage")!.Invoke(views);
            return bytes?.ToString() ?? "unavailable";
        }
        catch (Exception exception)
        {
            return $"unavailable ({exception.GetType().Name})";
        }
    }

    private static float ChipReserve(WidgetArt art, PeriodComparison? comparison) =>
        comparison is null
            ? 0
            : ChipChromeDp + art.TextWidthDp(comparison.Text, WidgetViews.ChipText) + ChipGapDp;
}
