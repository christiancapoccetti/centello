using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Views;
using Android.Widget;
using Centesimo.App.Theming;
using Centesimo.Application;

namespace Centesimo.App;

[BroadcastReceiver(Label = LauncherLabels.Prefix + "Aggiungi spesa", Exported = false)]
[IntentFilter([AppWidgetManager.ActionAppwidgetUpdate])]
[MetaData(AppWidgetManager.MetaDataAppwidgetProvider, Resource = "@xml/widget_add_info")]
public sealed class AddWidget : CentesimoWidgetProvider
{
    private const string AddText = "Aggiungi spesa";
    private const string VoiceText = "A voce";
    private const string PhotoText = "Da foto";
    private const float GlyphDp = 26;
    private const float LabelDp = 17;
    private const float FirstRunAlpha = 0.38f;

    private static WidgetTextStyle LabelStyle => new(Fonts.Bold, LabelDp);

    internal static RemoteViews Render(WidgetFrame frame)
    {
        var words = new AddWidgetWords(
            frame.Art.TextWidthDp(AddText, LabelStyle),
            frame.Art.TextWidthDp(VoiceText, LabelStyle),
            frame.Art.TextWidthDp(PhotoText, LabelStyle));
        var shape = AddWidgetShapes.Of(frame.Size.WidthDp, frame.Size.HeightDp, words);
        var views = new RemoteViews(frame.Context.PackageName, LayoutOf(shape));
        views.SetImageViewBitmap(Resource.Id.add_glyph, frame.Art.Glyph(MaterialSymbols.Add, GlyphDp));
        views.SetImageViewBitmap(Resource.Id.mic_glyph, frame.Art.Glyph(MaterialSymbols.Mic, GlyphDp));
        views.SetOnClickPendingIntent(Resource.Id.add_action, EntryLinks.Pending(frame.Context, frame.Data.Admit(new AppEntry.Screen.NewExpense(null))));
        views.SetOnClickPendingIntent(Resource.Id.mic_action,
            EntryLinks.Pending(frame.Context, frame.Data.Admit(new AppEntry.Panel.VoicePanel())));
        if (shape.HasPhoto())
        {
            views.SetImageViewBitmap(Resource.Id.photo_glyph, frame.Art.Glyph(MaterialSymbols.PhotoCamera, GlyphDp));
            views.SetOnClickPendingIntent(Resource.Id.photo_action,
                EntryLinks.Pending(frame.Context, frame.Data.Admit(new AppEntry.Panel.PhotoPanel())));
        }
        else
        {
            views.SetViewVisibility(Resource.Id.photo_divider, ViewStates.Gone);
            views.SetViewVisibility(Resource.Id.photo_action, ViewStates.Gone);
        }

        if (shape is AddWidgetShape.Bar or AddWidgetShape.Tall)
            views.SetImageViewBitmap(Resource.Id.add_label, frame.Art.Text(AddText, LabelStyle));
        if (shape == AddWidgetShape.Tall)
        {
            views.SetImageViewBitmap(Resource.Id.mic_label, frame.Art.Text(VoiceText, LabelStyle));
            views.SetImageViewBitmap(Resource.Id.photo_label, frame.Art.Text(PhotoText, LabelStyle));
        }

        if (!frame.Data.CanAdd)
            views.SetFloat(Resource.Id.add_pill, "setAlpha", FirstRunAlpha);

        return views;
    }

    private static int LayoutOf(AddWidgetShape shape) => shape switch
    {
        AddWidgetShape.Tall => Resource.Layout.widget_add_tall,
        AddWidgetShape.Bar => Resource.Layout.widget_add,
        _ => Resource.Layout.widget_add_compact
    };
}
