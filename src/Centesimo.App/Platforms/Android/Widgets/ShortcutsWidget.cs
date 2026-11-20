using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Views;
using Android.Widget;
using Centesimo.App.Theming;
using Centesimo.Application;

namespace Centesimo.App;

[BroadcastReceiver(Label = "Scorciatoie", Exported = false)]
[IntentFilter([AppWidgetManager.ActionAppwidgetUpdate])]
[MetaData(AppWidgetManager.MetaDataAppwidgetProvider, Resource = "@xml/widget_shortcuts_info")]
public sealed class ShortcutsWidget : CentesimoWidgetProvider
{
    private const float TileDp = 44;
    private const float PlusDp = 26;
    private const float NameDp = 12;
    // The layout's margin and padding on both sides, then five equal slots with a little air between names.
    private const float FrameDp = 36;
    private const float NameGapDp = 4;
    private const int SlotCount = 5;

    private static readonly Slot[] Slots =
    [
        new(Resource.Id.slot_1, Resource.Id.tile_1_day, Resource.Id.tile_1_night, Resource.Id.name_1),
        new(Resource.Id.slot_2, Resource.Id.tile_2_day, Resource.Id.tile_2_night, Resource.Id.name_2),
        new(Resource.Id.slot_3, Resource.Id.tile_3_day, Resource.Id.tile_3_night, Resource.Id.name_3),
        new(Resource.Id.slot_4, Resource.Id.tile_4_day, Resource.Id.tile_4_night, Resource.Id.name_4)
    ];

    private static WidgetTextStyle NameStyle => new(Fonts.SemiBold, NameDp);

    internal static RemoteViews Render(WidgetFrame frame)
    {
        var views = new RemoteViews(frame.Context.PackageName, Resource.Layout.widget_shortcuts);
        var nameWidthDp = Math.Max(0, (frame.Size.WidthDp - FrameDp) / SlotCount - NameGapDp);
        for (var index = 0; index < Slots.Length; index++)
        {
            if (index < frame.Data.Shortcuts.Count)
                Slots[index].Show(views, frame, frame.Data.Shortcuts[index], nameWidthDp);
            else
                Slots[index].Tile.Hide(views, ViewStates.Invisible);
        }

        views.SetImageViewBitmap(Resource.Id.other_plus, frame.Art.Glyph(MaterialSymbols.Add, PlusDp));
        views.SetImageViewBitmap(Resource.Id.other_name, frame.Art.Text("Altra", NameStyle, nameWidthDp));
        views.SetOnClickPendingIntent(Resource.Id.slot_other, WidgetLinks.Open(frame.Context, frame.Data.AddEntry()));
        return views;
    }

    private readonly record struct Slot(int Container, int Day, int Night, int Name)
    {
        public DayNightSlot Tile => new(Container, Day, Night);

        public void Show(RemoteViews views, WidgetFrame frame, CategoryShortcut shortcut, float nameWidthDp)
        {
            Tile.Show(views, frame.Art.Tile(shortcut, TileDp));
            views.SetImageViewBitmap(Name, frame.Art.Text(shortcut.Name, NameStyle, nameWidthDp));
            views.SetContentDescription(Container, $"{shortcut.Name}, aggiungi spesa");
            views.SetOnClickPendingIntent(Container,
                WidgetLinks.Open(frame.Context, frame.Data.AddEntry(shortcut.CategoryId)));
        }
    }
}
