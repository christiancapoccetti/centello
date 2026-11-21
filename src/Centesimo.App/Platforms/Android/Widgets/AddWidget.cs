using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Widget;
using Centesimo.App.Theming;
using Centesimo.Application;

namespace Centesimo.App;

[BroadcastReceiver(Label = "Aggiungi spesa", Exported = false)]
[IntentFilter([AppWidgetManager.ActionAppwidgetUpdate])]
[MetaData(AppWidgetManager.MetaDataAppwidgetProvider, Resource = "@xml/widget_add_info")]
public sealed class AddWidget : CentesimoWidgetProvider
{
    // Narrower than this the pill loses its words: the plus and the microphone split it in two halves.
    private const int WordsMinWidthDp = 180;
    private const float GlyphDp = 26;
    private const float LabelDp = 17;
    private const float FirstRunAlpha = 0.38f;

    internal static RemoteViews Render(WidgetFrame frame)
    {
        var withWords = frame.Size.WidthDp >= WordsMinWidthDp;
        var views = new RemoteViews(frame.Context.PackageName,
            withWords ? Resource.Layout.widget_add : Resource.Layout.widget_add_compact);
        views.SetImageViewBitmap(Resource.Id.add_glyph, frame.Art.Glyph(MaterialSymbols.Add, GlyphDp));
        views.SetImageViewBitmap(Resource.Id.mic_glyph, frame.Art.Glyph(MaterialSymbols.Mic, GlyphDp));
        if (withWords)
            views.SetImageViewBitmap(Resource.Id.add_label,
                frame.Art.Text("Aggiungi spesa", new WidgetTextStyle(Fonts.Bold, LabelDp)));

        views.SetOnClickPendingIntent(Resource.Id.add_action, EntryLinks.Pending(frame.Context, frame.Data.Admit(new AppEntry.Screen.NewExpense(null))));
        views.SetOnClickPendingIntent(Resource.Id.mic_action,
            EntryLinks.Pending(frame.Context, frame.Data.Admit(new AppEntry.Panel.VoicePanel())));
        if (!frame.Data.CanAdd)
            views.SetFloat(Resource.Id.add_pill, "setAlpha", FirstRunAlpha);

        return views;
    }
}
