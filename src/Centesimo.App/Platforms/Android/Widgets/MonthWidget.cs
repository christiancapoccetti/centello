using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Widget;
using Centesimo.App.Theming;
using Centesimo.Application;

namespace Centesimo.App;

[BroadcastReceiver(Label = "Il mese", Exported = false)]
[IntentFilter([AppWidgetManager.ActionAppwidgetUpdate])]
[MetaData(AppWidgetManager.MetaDataAppwidgetProvider, Resource = "@xml/widget_month_info")]
public sealed class MonthWidget : CentesimoWidgetProvider
{
    private const int MarginAndPaddingDp = 8 + 16;
    private const float MonthDp = 14;
    private const float TotalDp = 42;
    private const float EuroDp = 24;
    private const float BudgetDp = 13;
    private const float LineHeightDp = 10;

    private static readonly DayNightSlot Budget = new(Resource.Id.budget, Resource.Id.budget_day, Resource.Id.budget_night);
    private static readonly DayNightSlot Line = new(Resource.Id.line, Resource.Id.line_day, Resource.Id.line_night);

    internal static RemoteViews Render(WidgetFrame frame)
    {
        var month = frame.Data.Month;
        var art = frame.Art;
        var width = frame.Size.WidthDp - 2 * MarginAndPaddingDp;
        var views = new RemoteViews(frame.Context.PackageName, Resource.Layout.widget_month);
        views.SetImageViewBitmap(Resource.Id.month, art.Text(month.Month, new WidgetTextStyle(Fonts.Bold, MonthDp)));
        views.SetImageViewBitmap(Resource.Id.year, art.Text(month.Year, new WidgetTextStyle(Fonts.Regular, MonthDp)));
        views.SetImageViewBitmap(Resource.Id.total, art.Total(month.Total, TotalDp, EuroDp, width));
        Budget.Show(views, art.Sentence(month.Budget.Lead, month.Budget.Emphasis, "", width, BudgetDp));
        WidgetViews.Comparison(views, art, month.Comparison, month.Comparison?.Short);
        if (month.Segments.Count > 0)
            Line.Show(views, art.CategoryLine(month.Segments, width, LineHeightDp));
        else
            Line.Hide(views);

        WidgetViews.OverBudget(views, art, month.OverBudget, width);
        views.SetOnClickPendingIntent(Resource.Id.root, WidgetLinks.Open(frame.Context, new AppEntry.Today()));
        views.SetContentDescription(Resource.Id.root, month.Description);
        return views;
    }
}
