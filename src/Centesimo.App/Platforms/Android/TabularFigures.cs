using Microsoft.Maui.Handlers;

namespace Centesimo.App;

/// <summary>Archivo's default digits are proportional. Its GSUB table has tnum, which Android enables per TextView,
/// and MAUI has no font-feature API, so the handlers set it.</summary>
internal static class TabularFigures
{
    private const string Feature = "tnum";

    public static void Register()
    {
        LabelHandler.Mapper.AppendToMapping(nameof(TabularFigures), (handler, _) => handler.PlatformView.FontFeatureSettings = Feature);
        EntryHandler.Mapper.AppendToMapping(nameof(TabularFigures), (handler, _) => handler.PlatformView.FontFeatureSettings = Feature);
        ButtonHandler.Mapper.AppendToMapping(nameof(TabularFigures), (handler, _) => handler.PlatformView.FontFeatureSettings = Feature);
    }
}
