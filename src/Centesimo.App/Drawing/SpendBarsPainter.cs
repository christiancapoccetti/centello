using Centesimo.App.Theming;
using Centesimo.Application;

namespace Centesimo.App.Drawing;

/// <summary>Draws the bars of the daily spend chart and its dashed budget pace line inside <c>plot</c>. The plot is
/// the area where a fraction of 1 reaches the top: the caller keeps its own headroom above it for the pace label.
/// The home and the widgets both draw through it, so they cannot differ.</summary>
internal static class SpendBarsPainter
{
    private const float Gap = 3;
    private const float FutureStub = 4;
    private const float MinimumBar = 2;
    private const float PaceStroke = 1.5f;
    private const float PaceDash = 4;
    private const float PaceDashGap = 3;

    internal static void Draw(ICanvas canvas, RectF plot, SpendChart chart, bool isDark)
    {
        if (chart.Bars.Count == 0)
            return;

        // Hard edges: antialiasing would blur the 3dp gaps between bars.
        canvas.Antialias = false;
        var width = (plot.Width - Gap * (chart.Bars.Count - 1)) / chart.Bars.Count;
        for (var index = 0; index < chart.Bars.Count; index++)
        {
            var bar = chart.Bars[index];
            var height = HeightOf(bar, plot.Height);
            if (height == 0)
                continue;

            canvas.FillColor = ThemeColors.Resolve(ColorOf(bar.Kind), isDark);
            canvas.FillRectangle(plot.Left + index * (width + Gap), plot.Bottom - height, width, height);
        }

        if (chart.PaceFraction is not { } pace)
            return;

        var y = PaceY(plot, pace);
        canvas.StrokeColor = ThemeColors.Resolve(ThemeColor.Pace, isDark);
        canvas.StrokeSize = PaceStroke;
        // The canvas reads dash lengths as multiples of the stroke size.
        canvas.StrokeDashPattern = [PaceDash / PaceStroke, PaceDashGap / PaceStroke];
        canvas.DrawLine(plot.Left, y, plot.Right, y);
    }

    internal static float PaceY(RectF plot, double paceFraction) => plot.Bottom - (float)paceFraction * plot.Height;

    // Today is always marked and a day to come is a stub. A past day draws what was spent, never less than a
    // visible sliver, and nothing when nothing was spent.
    private static float HeightOf(SpendBar bar, float full) => bar.Kind switch
    {
        SpendBarKind.Future => FutureStub,
        SpendBarKind.Current => Math.Max(MinimumBar, (float)bar.Fraction * full),
        SpendBarKind.Past => bar.Fraction > 0 ? Math.Max(MinimumBar, (float)bar.Fraction * full) : 0
    };

    private static ThemeColor ColorOf(SpendBarKind kind) => kind switch
    {
        SpendBarKind.Future => ThemeColor.Hairline,
        SpendBarKind.Current => ThemeColor.Ink,
        SpendBarKind.Past => ThemeColor.ChartBar
    };
}
