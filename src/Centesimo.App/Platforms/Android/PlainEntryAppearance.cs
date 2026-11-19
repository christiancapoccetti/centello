using Centesimo.App.Controls;
using Microsoft.Maui.Handlers;

namespace Centesimo.App;

internal static class PlainEntryAppearance
{
    public static void Register() => EntryHandler.Mapper.AppendToMapping(nameof(PlainEntry), (handler, entry) =>
    {
        if (entry is not PlainEntry)
            return;

        handler.PlatformView.Background = null;
        handler.PlatformView.SetPadding(0, 0, 0, 0);
    });
}
