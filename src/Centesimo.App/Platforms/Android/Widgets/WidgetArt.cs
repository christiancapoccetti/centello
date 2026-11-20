using Android.Content;
using Microsoft.Maui.Graphics.Platform;
using Microsoft.Maui.Platform;
using AndroidBitmap = Android.Graphics.Bitmap;
using AndroidCanvas = Android.Graphics.Canvas;
using AndroidPaint = Android.Graphics.Paint;
using Centesimo.App.Controls;
using Centesimo.App.Drawing;
using Centesimo.App.Theming;
using Centesimo.Application;
using Centesimo.Domain;

namespace Centesimo.App;

internal readonly record struct WidgetTextStyle(string Font, float SizeDp);

internal readonly record struct DayNight(AndroidBitmap Day, AndroidBitmap Night);

internal readonly record struct Word(string Text, AndroidPaint Paint, ThemeColor Color);

/// <summary>The pixels a widget layout cannot produce: every word, every icon glyph and everything that wears a
/// category's ink. A launcher inflates the layout without the app's fonts, so words are drawn here with the app's own
/// typefaces. Single-colour art is an alpha mask that the layout tints with a theme colour resource, so it follows the
/// launcher's scheme with no refresh. Art in several colours comes twice, painted for the light and for the dark
/// scheme, into a <see cref="DayNightSlot"/>.</summary>
internal sealed class WidgetArt(Context context, IFontManager fonts)
{
    private const string Ellipsis = "…";
    private const float TotalGapDp = 3;
    private const float TotalTracking = -0.02f;
    private const float MinTotalScale = 0.5f;
    private const float ShrinkStep = 0.96f;
    private const float BodyLineHeight = 1.35f;
    private const float ParagraphLineHeight = 1.25f;

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

    /// <summary>The month's total in the condensed face with its smaller euro sign on the same baseline. Both sizes
    /// shrink together until the whole fits <paramref name="maxWidthDp"/>, so a five-digit total never clips.</summary>
    public AndroidBitmap Total(string amount, float sizeDp, float euroDp, float maxWidthDp)
    {
        var scale = 1f;
        var maxPx = maxWidthDp * _density;
        while (true)
        {
            using var digits = TotalPaint(sizeDp * scale);
            using var euro = TotalPaint(euroDp * scale);
            var euroX = digits.MeasureText(amount) + TotalGapDp * scale * _density;
            var width = euroX + euro.MeasureText("€");
            if (width > maxPx && scale > MinTotalScale)
            {
                scale *= Math.Min(ShrinkStep, maxPx / width);
                continue;
            }

            var bounds = new Android.Graphics.Rect();
            digits.GetTextBounds(amount, 0, amount.Length, bounds);
            return Mask((int)Math.Ceiling(width) + 2, bounds.Height() + 2, canvas =>
            {
                canvas.DrawText(amount, 1, 1 - bounds.Top, digits);
                canvas.DrawText("€", 1 + euroX, 1 - bounds.Top, euro);
            });
        }
    }

    public DayNight CategoryLine(IReadOnlyList<CategorySegment> segments, float widthDp, float heightDp) =>
        Pair(widthDp, heightDp, (canvas, bounds, isDark) => CategoryLinePainter.Draw(canvas, bounds, segments, isDark));

    /// <summary>A sentence in the mute colour with one bold ink amount, wrapped to <paramref name="widthDp"/>. The
    /// amount stays whole: it never breaks between its figures and the euro sign.</summary>
    public DayNight Sentence(string lead, string emphasis, float widthDp, float sizeDp)
    {
        using var regular = PaintOf(new WidgetTextStyle(Fonts.Regular, sizeDp));
        using var bold = PaintOf(new WidgetTextStyle(Fonts.Bold, sizeDp));
        var words = lead
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(word => new Word(word, regular, ThemeColor.Mute))
            .ToList();
        if (emphasis.HasValue())
            words.Add(new Word(emphasis, bold, ThemeColor.Ink));

        var space = regular.MeasureText(" ");
        var lines = Wrap(words, widthDp * _density, space);
        var lineHeight = sizeDp * BodyLineHeight * _density;
        var height = (int)Math.Ceiling(lines.Count * lineHeight);
        AndroidBitmap Painted(bool isDark) => Argb(Px(widthDp), height, canvas =>
            DrawLines(canvas, lines, lineHeight, space, word => ThemeColors.Resolve(word.Color, isDark).ToPlatform()));
        return new DayNight(Painted(false), Painted(true));
    }

    /// <summary>One colour of text wrapped to <paramref name="maxWidthDp"/>, as wide as its longest line.</summary>
    public AndroidBitmap Paragraph(string text, WidgetTextStyle style, float maxWidthDp)
    {
        using var paint = PaintOf(style);
        var space = paint.MeasureText(" ");
        var lines = Wrap(
            text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(word => new Word(word, paint, ThemeColor.Ink)),
            maxWidthDp * _density, space);
        var lineHeight = style.SizeDp * ParagraphLineHeight * _density;
        var width = (int)Math.Ceiling(lines.Max(line => LineWidth(line, space))) + 2;
        return Mask(width, (int)Math.Ceiling(lines.Count * lineHeight), canvas =>
            DrawLines(canvas, lines, lineHeight, space, _ => Android.Graphics.Color.Black));
    }

    private AndroidPaint TotalPaint(float sizeDp)
    {
        var paint = PaintOf(new WidgetTextStyle(Fonts.CondensedExtraBold, sizeDp));
        paint.LetterSpacing = TotalTracking;
        return paint;
    }

    private static List<List<Word>> Wrap(IEnumerable<Word> words, float maxPx, float space)
    {
        var lines = new List<List<Word>> { new() };
        foreach (var word in words)
        {
            var line = lines[^1];
            if (line.Count > 0 && LineWidth(line, space) + space + word.Paint.MeasureText(word.Text) > maxPx)
            {
                line = [];
                lines.Add(line);
            }

            line.Add(word);
        }

        return lines;
    }

    private static float LineWidth(List<Word> line, float space) =>
        line.Sum(word => word.Paint.MeasureText(word.Text)) + space * (line.Count - 1);

    private static void DrawLines(AndroidCanvas canvas, List<List<Word>> lines, float lineHeight, float space,
        Func<Word, Android.Graphics.Color> colorOf)
    {
        for (var index = 0; index < lines.Count; index++)
        {
            var x = 1f;
            foreach (var word in lines[index])
            {
                var paint = word.Paint;
                paint.Color = colorOf(word);
                var baseline = index * lineHeight + (lineHeight - (paint.Descent() - paint.Ascent())) / 2f - paint.Ascent();
                canvas.DrawText(word.Text, x, baseline, paint);
                x += paint.MeasureText(word.Text) + space;
            }
        }
    }

    private AndroidBitmap Argb(int width, int height, Action<AndroidCanvas> draw)
    {
        var bitmap = AndroidBitmap.CreateBitmap(width, height, AndroidBitmap.Config.Argb8888!)!;
        bitmap.Density = _densityDpi;
        using var canvas = new AndroidCanvas(bitmap);
        draw(canvas);
        return bitmap;
    }

    private int Px(float dp) => (int)Math.Ceiling(dp * _density);
}
