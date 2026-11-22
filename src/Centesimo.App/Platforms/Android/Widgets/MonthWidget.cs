using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Util;
using Android.Views;
using Android.Widget;
using Centesimo.App.Theming;
using Centesimo.Application;

namespace Centesimo.App;

[BroadcastReceiver(Label = LauncherLabels.Prefix + "Il mese", Exported = false)]
[IntentFilter([AppWidgetManager.ActionAppwidgetUpdate])]
[MetaData(AppWidgetManager.MetaDataAppwidgetProvider, Resource = "@xml/widget_month_info")]
public sealed class MonthWidget : CentesimoWidgetProvider
{
    // The measures of widget_month.xml: keep them in step with MonthWidgetShapes.
    private const float MarginDp = 8;
    private const float HeaderGapDp = 6;
    private const float BudgetGapDp = 4;
    private const float YearGapDp = 4;
    private const float ChipHeightDp = 24;
    private const float LineHeightDp = 10;
    private const float MonthDp = 14;
    private const float BudgetDp = 13;

    private static readonly DayNightSlot Budget = new(Resource.Id.budget, Resource.Id.budget_day, Resource.Id.budget_night);
    private static readonly DayNightSlot Line = new(Resource.Id.line, Resource.Id.line_day, Resource.Id.line_night);
    private static WidgetTextStyle MonthStyle => new(Fonts.Bold, MonthDp);
    private static WidgetTextStyle YearStyle => new(Fonts.Regular, MonthDp);

    internal static RemoteViews Render(WidgetFrame frame)
    {
        var month = frame.Data.Month;
        var art = frame.Art;
        var shape = MonthWidgetShapes.Of(frame.Size.WidthDp, frame.Size.HeightDp);
        var style = MonthWidgetShapes.StyleOf(shape);
        var width = frame.Size.WidthDp - 2 * (MarginDp + style.PaddingDp);
        var views = new RemoteViews(frame.Context.PackageName, Resource.Layout.widget_month);
        var padding = (int)Math.Round(style.PaddingDp * frame.Context.Resources!.DisplayMetrics!.Density);
        views.SetViewPadding(Resource.Id.card, padding, padding, padding, padding);

        var monthArt = art.Text(month.Month, MonthStyle);
        views.SetImageViewBitmap(Resource.Id.month, monthArt);
        ShowYear(views, art, month, width);
        var total = art.Total(month.Total, style.TotalDp, style.EuroDp, width);
        views.SetImageViewBitmap(Resource.Id.total, total);
        var budget = art.Sentence(month.Budget.Lead, month.Budget.Emphasis, "", width, BudgetDp);
        Budget.Show(views, budget);
        var alert = WidgetViews.Alert(art, month.OverBudget, width);
        var rows = new MonthWidgetRows(
            Core: WidgetArt.HeightDp(monthArt) + HeaderGapDp + WidgetArt.HeightDp(total) + BudgetGapDp + WidgetArt.HeightDp(budget.Day),
            Chip: month.Comparison is null ? 0 : ChipHeightDp,
            Line: month.Segments.Count > 0 ? LineHeightDp : 0,
            AlertOneLine: alert.OneLine is null ? 0 : WidgetArt.HeightDp(alert.OneLine),
            AlertFull: alert.OneLine is null ? 0 : alert.Full is null ? float.PositiveInfinity : WidgetArt.HeightDp(alert.Full));
        var fit = MonthWidgetShapes.Fit(MonthWidgetShapes.RowsHeightDp(frame.Size.HeightDp, style), rows);
        Log.Debug(HomeScreen.LogTag, $"Il mese {frame.Size.WidthDp}x{frame.Size.HeightDp}dp {shape}: {fit}");

        WidgetViews.Comparison(views, art, fit.ShowChip ? month.Comparison : null, month.Comparison?.Short);
        if (fit.ShowLine)
            Line.Show(views, art.CategoryLine(month.Segments, width, LineHeightDp));
        else
            Line.Hide(views);

        WidgetViews.OverBudget(views, art, fit.Alert switch
        {
            MonthAlertFit.Full => alert.Full,
            MonthAlertFit.OneLine => alert.OneLine,
            _ => null
        });
        views.SetOnClickPendingIntent(Resource.Id.root, EntryLinks.Pending(frame.Context, new AppEntry.Screen.Today()));
        views.SetContentDescription(Resource.Id.root, month.Description);
        return views;
    }

    // A year that does not fit beside the month leaves: the widget is always the month of today.
    private static void ShowYear(RemoteViews views, WidgetArt art, MonthGlance month, float width)
    {
        var fits = art.TextWidthDp(month.Month, MonthStyle) + YearGapDp + art.TextWidthDp(month.Year, YearStyle) <= width;
        views.SetViewVisibility(Resource.Id.year, fits ? ViewStates.Visible : ViewStates.Gone);
        if (fits)
            views.SetImageViewBitmap(Resource.Id.year, art.Text(month.Year, YearStyle));
    }
}
