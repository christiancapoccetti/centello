using Microsoft.Maui.Graphics.Platform;
using AndroidBitmap = Android.Graphics.Bitmap;
using AndroidCanvas = Android.Graphics.Canvas;

namespace Centello.App.Controls.Recap;

internal static class SpendingMapPng
{
    // Limit the actual ARGB allocation, independently of how many categories this period contains.
    private const int PixelBudget = 8_000_000;
    private const int MaxSide = 8192;

    public static byte[] Render(SpendingMapArt art)
    {
        var size = art.DesignSize;
        var scale = Math.Min(2.7, Math.Sqrt(PixelBudget / (size.Width * size.Height)));
        scale = Math.Min(scale, MaxSide / Math.Max(size.Width, size.Height));
        if (scale < 1)
            throw new InvalidOperationException("The full map exceeds the readable PNG size.");

        var width = (int)Math.Floor(size.Width * scale);
        var height = (int)Math.Floor(size.Height * scale);
        using var bitmap = AndroidBitmap.CreateBitmap(width, height, AndroidBitmap.Config.Argb8888!)!;
        using var native = new AndroidCanvas(bitmap);
        using var platform = new PlatformCanvas(Android.App.Application.Context) { Canvas = native };
        var canvas = new ScalingCanvas(platform);
        art.Draw(canvas, new RectF(0, 0, width, height));
        using var stream = new MemoryStream();
        if (!bitmap.Compress(AndroidBitmap.CompressFormat.Png!, 100, stream))
            throw new IOException("Android could not encode the spending map.");
        return stream.ToArray();
    }
}
