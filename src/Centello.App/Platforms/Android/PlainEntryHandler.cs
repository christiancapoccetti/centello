using Android.Content;
using Android.Views.InputMethods;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace Centello.App;

internal sealed class PlainEntryHandler : EntryHandler
{
    protected override MauiAppCompatEditText CreatePlatformView() =>
        new InlineEditText(Context);

    private sealed class InlineEditText(Context context) : MauiAppCompatEditText(context)
    {
        public override IInputConnection? OnCreateInputConnection(EditorInfo? outAttrs)
        {
            var connection = base.OnCreateInputConnection(outAttrs);
            // Apply the flags when the IME connects, after MAUI maps keyboard and return-key options.
            if (outAttrs is not null)
                outAttrs.ImeOptions |= ImeFlags.NoFullscreen;
            return connection;
        }
    }
}
