using Android.Content;
using Microsoft.Maui.Graphics.Platform;
using Microsoft.Maui.Platform;
using AndroidBitmap = Android.Graphics.Bitmap;
using AndroidCanvas = Android.Graphics.Canvas;
using AndroidPaint = Android.Graphics.Paint;
using Centesimo.App.Controls;
using Centesimo.App.Theming;
using Centesimo.Application;

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
    private const string Ellipsis = "…";
    private readonly float _density = context.Resources!.DisplayMetrics!.Density;
    private readonly int _densityDpi = (int)context.Resources!.DisplayMetrics!.DensityDpi;

    /// <summary>An icon glyph centred in a square of <paramref name="sizeDp"/>, the box the Home gives it.</summary>
    public AndroidBitmap Glyph(string codepoint, float sizeDp)
    {
        using var paint = PaintOf(new WidgetTextStyle(Fonts.Icons, sizeDp));
        var size = Px(sizeDp);
        return Mask(size, size, canvas => DrawCentred(canvas, paint, codepoint, size));
    }

    /// <summary>A line of text in the app's typeface with tabular figures, as tight as its own height and width. Wider
    /// than <paramref name="maxWidthDp"/>, it ends in an ellipsis.</summary>
    public AndroidBitmap Text(string text, WidgetTextStyle style, float maxWidthDp = float.MaxValue)
    {
        using var paint = PaintOf(style);
        var shown = Fitted(paint, text, maxWidthDp * _density);
        var ascent = paint.Ascent();
        var width = (int)Math.Ceiling(paint.MeasureText(shown)) + 2;
        var height = (int)Math.Ceiling(paint.Descent() - ascent);
        return Mask(width, height, canvas => canvas.DrawText(shown, 1, -ascent, paint));
    }

    /// <summary>The category tile as the Home draws it: a square in the category's ink for the scheme, the glyph in
    /// tile ink at <see cref="CategoryTile.GlyphScale"/> of the size, centred. Only the glyph is antialiased, so the
    /// square's edges stay sharp.</summary>
    public DayNight Tile(CategoryShortcut shortcut, float sizeDp) =>
        new(Tile(shortcut, sizeDp, false), Tile(shortcut, sizeDp, true));

    /// <summary>Art in several colours: <paramref name="draw"/> runs once for each scheme, on a bitmap with the density
    /// scaling MAUI's own graphics view applies, so the Home's painters draw exactly what they draw on screen.</summary>
    internal DayNight Pair(float widthDp, float heightDp, Action<ICanvas, RectF, bool> draw) =>
        new(Painted(widthDp, heightDp, false, draw), Painted(widthDp, heightDp, true, draw));

    private AndroidBitmap Tile(CategoryShortcut shortcut, float sizeDp, bool isDark)
    {
        var bitmap = NewBitmap(sizeDp, sizeDp);
        using var canvas = new AndroidCanvas(bitmap);
        using var square = new AndroidPaint { Color = ThemeColors.ResolvePalette(shortcut.ColorKey, isDark).ToPlatform() };
        canvas.DrawRect(0, 0, bitmap.Width, bitmap.Height, square);
        using var glyph = PaintOf(new WidgetTextStyle(Fonts.Icons, (float)Math.Round(sizeDp * CategoryTile.GlyphScale)));
        glyph.Color = ThemeColors.Resolve(ThemeColor.TileInk, isDark).ToPlatform();
        DrawCentred(canvas, glyph, CategoryIcons.Resolve(shortcut.IconKey).Glyph, bitmap.Width);
        return bitmap;
    }

    private AndroidBitmap Painted(float widthDp, float heightDp, bool isDark, Action<ICanvas, RectF, bool> draw)
    {
        var bitmap = NewBitmap(widthDp, heightDp);
        using var native = new AndroidCanvas(bitmap);
        var canvas = new ScalingCanvas(new PlatformCanvas(context) { Canvas = native });
        canvas.Scale(_density, _density);
        draw(canvas, new RectF(0, 0, widthDp, heightDp), isDark);
        return bitmap;
    }

    private AndroidBitmap NewBitmap(float widthDp, float heightDp)
    {
        var bitmap = AndroidBitmap.CreateBitmap(Px(widthDp), Px(heightDp), AndroidBitmap.Config.Argb8888!)!;
        bitmap.Density = _densityDpi;
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

    private static void DrawCentred(AndroidCanvas canvas, AndroidPaint paint, string codepoint, int sizePx)
    {
        paint.TextAlign = AndroidPaint.Align.Center;
        var baseline = sizePx / 2f - (paint.Descent() + paint.Ascent()) / 2f;
        canvas.DrawText(codepoint, sizePx / 2f, baseline, paint);
    }

    private static string Fitted(AndroidPaint paint, string text, float maxWidthPx)
    {
        if (paint.MeasureText(text) <= maxWidthPx)
            return text;

        var kept = paint.BreakText(text, true, maxWidthPx - paint.MeasureText(Ellipsis), null);
        return text[..kept].TrimEnd() + Ellipsis;
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
