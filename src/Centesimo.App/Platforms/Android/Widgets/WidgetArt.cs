using Android.Content;
using Microsoft.Maui.Graphics.Platform;
using AndroidBitmap = Android.Graphics.Bitmap;
using AndroidCanvas = Android.Graphics.Canvas;
using AndroidPaint = Android.Graphics.Paint;
using Centesimo.App.Theming;

namespace Centesimo.App;

internal readonly record struct WidgetTextStyle(string Font, float SizeDp);

internal readonly record struct DayNight(AndroidBitmap Day, AndroidBitmap Night);

/// <summary>The pixels a widget layout cannot produce: every word, every icon glyph and everything that wears a
/// category's ink. A launcher inflates the layout without the app's fonts, so words are drawn here with the app's own
/// typefaces. Single-colour art is an alpha mask that the layout tints with a theme colour resource, so it follows the
/// launcher's scheme with no refresh. Art in several colours comes twice, painted for the light and for the dark
/// scheme, into a <see cref="DayNightSlot"/>.</summary>
internal sealed class WidgetArt(Context context, IFontManager fonts)
{
    private readonly float _density = context.Resources!.DisplayMetrics!.Density;
    private readonly int _densityDpi = (int)context.Resources!.DisplayMetrics!.DensityDpi;

    /// <summary>An icon glyph centred in a square of <paramref name="sizeDp"/>, the box the Home gives it.</summary>
    public AndroidBitmap Glyph(string codepoint, float sizeDp)
    {
        using var paint = PaintOf(new WidgetTextStyle(Fonts.Icons, sizeDp));
        paint.TextAlign = AndroidPaint.Align.Center;
        var size = Px(sizeDp);
        var baseline = size / 2f - (paint.Descent() + paint.Ascent()) / 2f;
        return Mask(size, size, canvas => canvas.DrawText(codepoint, size / 2f, baseline, paint));
    }

    /// <summary>A line of text in the app's typeface with tabular figures, as tight as its own height and width.</summary>
    public AndroidBitmap Text(string text, WidgetTextStyle style)
    {
        using var paint = PaintOf(style);
        var ascent = paint.Ascent();
        var width = (int)Math.Ceiling(paint.MeasureText(text)) + 2;
        var height = (int)Math.Ceiling(paint.Descent() - ascent);
        return Mask(width, height, canvas => canvas.DrawText(text, 1, -ascent, paint));
    }

    /// <summary>Art in several colours: <paramref name="draw"/> runs once for each scheme, on a bitmap with the density
    /// scaling MAUI's own graphics view applies, so the Home's painters draw exactly what they draw on screen.</summary>
    internal DayNight Pair(float widthDp, float heightDp, Action<ICanvas, RectF, bool> draw) =>
        new(Painted(widthDp, heightDp, false, draw), Painted(widthDp, heightDp, true, draw));

    private AndroidBitmap Painted(float widthDp, float heightDp, bool isDark, Action<ICanvas, RectF, bool> draw)
    {
        var bitmap = AndroidBitmap.CreateBitmap(Px(widthDp), Px(heightDp), AndroidBitmap.Config.Argb8888!)!;
        bitmap.Density = _densityDpi;
        using var native = new AndroidCanvas(bitmap);
        var canvas = new ScalingCanvas(new PlatformCanvas(context) { Canvas = native });
        canvas.Scale(_density, _density);
        draw(canvas, new RectF(0, 0, widthDp, heightDp), isDark);
        return bitmap;
    }

    private AndroidBitmap Mask(int width, int height, Action<AndroidCanvas> draw)
    {
        var bitmap = AndroidBitmap.CreateBitmap(width, height, AndroidBitmap.Config.Alpha8!)!;
        bitmap.Density = _densityDpi;
        using var canvas = new AndroidCanvas(bitmap);
        draw(canvas);
        return bitmap;
    }

    private AndroidPaint PaintOf(WidgetTextStyle style)
    {
        var paint = new AndroidPaint(Android.Graphics.PaintFlags.AntiAlias)
        {
            Color = Android.Graphics.Color.Black,
            TextSize = style.SizeDp * _density,
            FontFeatureSettings = "tnum"
        };
        paint.SetTypeface(fonts.GetTypeface(Microsoft.Maui.Font.OfSize(style.Font, style.SizeDp)));
        return paint;
    }

    private int Px(float dp) => (int)Math.Ceiling(dp * _density);
}
