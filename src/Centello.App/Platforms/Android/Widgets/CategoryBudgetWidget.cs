using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Views;
using Android.Widget;
using Centello.App.Theming;
using Centello.Application;

namespace Centello.App;

[BroadcastReceiver(Name = "crc64d73031b65ce57f40.CategoryBudgetWidget", Label = LauncherLabels.Prefix + "Budget per categoria", Exported = false)]
[IntentFilter([AppWidgetManager.ActionAppwidgetUpdate])]
[MetaData(AppWidgetManager.MetaDataAppwidgetProvider, Resource = "@xml/widget_category_budget_info")]
public sealed class CategoryBudgetWidget : CentelloWidgetProvider
{
    // These dimensions follow widget_category_budget.xml and widget_category_budget_row.xml.
    private const int FrameDp = 2 * (8 + 10);
    private const int HeaderDp = 20 + 6;
    private const int TileDp = 24;
    private const int TileGapDp = 8;
    private const int AlertDp = 14;
    private const int AlertGapDp = 3;
    private const int AmountGapDp = 6;
    private const int BarDp = 2;
    private const string Empty = "Nessuna categoria da mostrare";

    private static readonly DayNightSlot Tile = new(Resource.Id.budget_tile, Resource.Id.budget_tile_day, Resource.Id.budget_tile_night);
    private static readonly DayNightSlot Bar = new(Resource.Id.budget_progress, Resource.Id.budget_progress_day, Resource.Id.budget_progress_night);
    private static WidgetTextStyle TitleStyle => new(Fonts.Bold, 14);
    private static WidgetTextStyle NameStyle => new(Fonts.SemiBold, 13);
    private static WidgetTextStyle AmountStyle => new(Fonts.SemiBold, 12);
    private static WidgetTextStyle DetailStyle => new(Fonts.Regular, 11);

    internal static RemoteViews Render(WidgetFrame frame)
    {
        var portrait = Render(frame, frame.Size);
        return frame.Size == frame.LandscapeSize
            ? portrait
            : new RemoteViews(Render(frame, frame.LandscapeSize), portrait);
    }

    private static RemoteViews Render(WidgetFrame frame, WidgetSize size)
    {
        var month = frame.Data.Month;
        var rows = month.BudgetRows;
        var empty = rows.Count == 0;
        var width = Math.Max(0, size.WidthDp - FrameDp);
        var visibleRows = CategoryBudgetWidgetLayout.RowCapacity(size.HeightDp - FrameDp - HeaderDp);
        var views = new RemoteViews(frame.Context.PackageName, Resource.Layout.widget_category_budget);
        views.SetImageViewBitmap(Resource.Id.budget_title, frame.Art.Text($"Budget · {month.Month}", TitleStyle, width));
        views.SetViewVisibility(Resource.Id.budget_empty, empty ? ViewStates.Visible : ViewStates.Gone);
        views.RemoveAllViews(Resource.Id.budget_rows);

        var description = new List<string> { $"Budget per categoria, {month.Month} {month.Year}" };
        if (empty)
        {
            views.SetImageViewBitmap(Resource.Id.budget_empty, frame.Art.Text(Empty, DetailStyle, width));
            description.Add(Empty);
        }

        foreach (var row in rows.Take(visibleRows))
        {
            var (rowViews, rowDescription) = RenderRow(frame, row, width - TileDp - TileGapDp);
            views.AddView(Resource.Id.budget_rows, rowViews);
            description.Add(rowDescription);
        }

        description.Add("Apri Oggi");
        views.SetContentDescription(Resource.Id.root, string.Join(". ", description));
        views.SetOnClickPendingIntent(Resource.Id.root, EntryLinks.Pending(frame.Context, new AppEntry.Screen.Today()));
        return views;
    }

    private static (RemoteViews Views, string Description) RenderRow(WidgetFrame frame, CategoryBudgetRow row, float width)
    {
        var art = frame.Art;
        var views = new RemoteViews(frame.Context.PackageName, Resource.Layout.widget_category_budget_row);
        Tile.Show(views, art.Tile(row.IconKey, row.ColorKey, TileDp));

        var overBudget = row.RemainingCents is < 0;
        var alertWidth = overBudget ? AlertDp + AlertGapDp : 0;
        var minNameWidth = Math.Min(48, width * 0.36f);
        var amountWidth = width - minNameWidth - AmountGapDp - alertWidth;
        var spent = Money(row.SpentCents);
        var balance = row.RemainingCents is { } remaining ? Money(Math.Abs(remaining)) : spent;
        var balanceWithLabel = row.RemainingCents is null ? $"{spent} spesi" : $"{balance} disponibili";
        var shownBalance = overBudget ? $"−{balance}" : balance;
        if (!overBudget && art.TextWidthDp(balanceWithLabel, AmountStyle) <= amountWidth)
            shownBalance = balanceWithLabel;

        var amount = art.Text(shownBalance, AmountStyle, amountWidth);
        var name = art.Text(row.Name, NameStyle, width - WidgetArt.WidthDp(amount) - AmountGapDp - alertWidth);
        views.SetImageViewBitmap(Resource.Id.budget_name, name);
        views.SetViewVisibility(Resource.Id.budget_amount, overBudget ? ViewStates.Gone : ViewStates.Visible);
        views.SetViewVisibility(Resource.Id.budget_alert, overBudget ? ViewStates.Visible : ViewStates.Gone);
        views.SetImageViewBitmap(overBudget ? Resource.Id.budget_alert_amount : Resource.Id.budget_amount, amount);
        if (overBudget)
            views.SetImageViewBitmap(Resource.Id.budget_alert_glyph, art.Glyph(MaterialSymbols.Error, AlertDp));

        if (row.BudgetCents is not { } budget)
            return (views, $"{row.Name}, {spent} spesi");

        var limit = Money(budget);
        var detail = $"{spent} spesi su {limit}";
        var shownDetail = art.TextWidthDp(detail, DetailStyle) <= width ? detail : $"{spent} / {limit}";
        views.SetViewVisibility(Resource.Id.budget_detail, ViewStates.Visible);
        views.SetImageViewBitmap(Resource.Id.budget_detail, art.Text(shownDetail, DetailStyle, width));
        var fraction = Math.Clamp(row.SpentCents / (double)Math.Max(budget, 1), 0, 1);
        Bar.Show(views, art.Pair(width, BarDp, (canvas, bounds, dark) =>
        {
            canvas.Antialias = false;
            canvas.FillColor = ThemeColors.Resolve(ThemeColor.Outline, dark);
            canvas.FillRectangle(bounds);
            canvas.FillColor = ThemeColors.ResolvePalette(row.ColorKey, dark);
            canvas.FillRectangle(0, 0, (float)(bounds.Width * fraction), bounds.Height);
        }));
        var status = overBudget ? "oltre budget" : "disponibili";
        return (views, $"{row.Name}, {detail}, {balance} {status}");
    }

    private static string Money(long cents) => $"{ItalianFormat.Budget(cents)} €";
}
