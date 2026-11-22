using Centello.App.Theming;
using Centello.Application;
using Centello.Domain;
using Microsoft.Maui.Layouts;

namespace Centello.App.Controls.Recap;

/// <summary>The bars of a card: days of a month, months of a year, weekdays, weeks, tag ticks, the cumulative year.
/// The bars and the dashed previous line are painted on a canvas; every value above a bar and every label under it
/// is a real Label, because a canvas cannot show tabular figures. It reads fractions and finished strings only, and
/// the colors from the card's ink.</summary>
public sealed class RecapBarsView : ContentView
{
    private const double ValueRise = 20;
    private const double ValueHeight = 18;
    private const double ValueLift = 3;
    private const double AxisRoom = 22;
    private const double AxisGap = 5;
    private const int ManyBars = 31;
    private const int SparseBars = 12;
    private const double MinimumBar = 2;

    private readonly int _count;
    private readonly float _gap;
    private readonly AbsoluteLayout _plane = new();
    private readonly List<(Label Label, int Slot)> _placed = [];

    public RecapBarsView(RecapBars chart, StoryInk ink, double plotHeight)
    {
        _count = chart.Bars.Count;
        _gap = GapOf(chart);
        var hasValues = chart.Bars.Any(bar => bar.Value.HasValue());
        var hasLabels = chart.Bars.Any(bar => bar.Label.HasValue());
        var rise = hasValues ? ValueRise : 0;
        var canvas = new BarsCanvas(chart, ink, plotHeight, rise, _gap);
        SemanticProperties.SetDescription(canvas, chart.Description);
        AbsoluteLayout.SetLayoutBounds(canvas, new Rect(0, 0, 1, 1));
        AbsoluteLayout.SetLayoutFlags(canvas, AbsoluteLayoutFlags.All);
        _plane.Children.Add(canvas);
        HeightRequest = rise + plotHeight + (hasLabels ? AxisRoom : 0);
        for (var slot = 0; slot < _count; slot++)
        {
            var bar = chart.Bars[slot];
            var isLoud = bar.Kind is RecapBarKind.Strong or RecapBarKind.Outlined;
            var color = isLoud ? ink.Ink : ink.Mute;
            if (bar.Value.HasValue())
                Add(Label(bar.Value, color, isLoud ? Fonts.Bold : Fonts.SemiBold), slot,
                    rise + plotHeight - HeightOf(bar, plotHeight) - ValueHeight - ValueLift);

            if (bar.Label.HasValue())
                Add(Label(bar.Label, color, isLoud ? Fonts.Bold : Fonts.Regular), slot, rise + plotHeight + AxisGap);
        }

        Content = _plane;
        SizeChanged += (_, _) => Place();
    }

    private static float GapOf(RecapBars chart) => chart.Bars.Count > ManyBars || chart.Previous.Count > 0 ? 2 : 3;

    private static double HeightOf(RecapBar bar, double plotHeight) =>
        bar.Kind == RecapBarKind.Stub || bar.Height <= 0 ? 0 : Math.Max(MinimumBar, bar.Height * plotHeight);

    private static Label Label(string text, Color color, string fontFamily) => new()
    {
        Text = text,
        TextColor = color,
        FontFamily = fontFamily,
        FontSize = 12,
        LineBreakMode = LineBreakMode.NoWrap,
        HorizontalOptions = LayoutOptions.Start,
        VerticalOptions = LayoutOptions.Start,
        InputTransparent = true
    };

    private void Add(Label label, int slot, double top)
    {
        AutomationProperties.SetIsInAccessibleTree(label, false);
        AbsoluteLayout.SetLayoutBounds(label, new Rect(0, top, AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize));
        label.SizeChanged += (_, _) => Place();
        _placed.Add((label, slot));
        _plane.Children.Add(label);
    }

    private void Place()
    {
        if (Width <= 0)
            return;

        var slotWidth = (Width - _gap * (_count - 1)) / _count;
        foreach (var (label, slot) in _placed)
        {
            var center = slot * (slotWidth + _gap) + slotWidth / 2;
            label.TranslationX = Math.Clamp(center - label.Width / 2, 0, Math.Max(0, Width - label.Width));
        }
    }

    private sealed class BarsCanvas(RecapBars chart, StoryInk ink, double plotHeight, double rise, float gap)
        : ThemedDrawingView
    {
        private const float Outline = 2;
        private const float LineStroke = 2;
        private const float Dash = 4;
        private const float DashGap = 3;
        private const float SparseStub = 4;
        private const float DenseStub = 2;

        protected override void Draw(ICanvas canvas, RectF bounds)
        {
            var count = chart.Bars.Count;
            if (count == 0)
                return;

            // Hard edges: antialiasing would blur the gaps between bars.
            canvas.Antialias = false;
            var width = (bounds.Width - gap * (count - 1)) / count;
            var baseline = (float)(rise + plotHeight);
            for (var slot = 0; slot < count; slot++)
                DrawBar(canvas, chart.Bars[slot], slot * (width + gap), width, baseline, count);

            if (chart.Previous.Count > 0)
                DrawPrevious(canvas, bounds.Width, width, baseline);
        }

        private void DrawBar(ICanvas canvas, RecapBar bar, float left, float width, float baseline, int count)
        {
            if (bar.Kind == RecapBarKind.Stub)
            {
                var stub = count > SparseBars ? DenseStub : SparseStub;
                canvas.FillColor = ink.Hairline;
                canvas.FillRectangle(left, baseline - stub, width, stub);
                return;
            }

            var height = (float)HeightOf(bar, plotHeight);
            if (height == 0)
                return;

            var top = baseline - height;
            canvas.FillColor = bar.Kind switch
            {
                RecapBarKind.Strong => ink.Ink,
                RecapBarKind.Palette => ink.Palette(bar.ColorKey),
                RecapBarKind.Bar => ink.Bar,
                RecapBarKind.Outlined => ink.Bar,
                RecapBarKind.Stub => ink.Hairline
            };
            canvas.FillRectangle(left, top, width, height);
            if (bar.Kind != RecapBarKind.Outlined)
                return;

            canvas.FillColor = ink.Ink;
            canvas.FillRectangle(left, top, width, Outline);
            canvas.FillRectangle(left, baseline - Outline, width, Outline);
            canvas.FillRectangle(left, top, Outline, height);
            canvas.FillRectangle(left + width - Outline, top, Outline, height);
        }

        private void DrawPrevious(ICanvas canvas, float totalWidth, float width, float baseline)
        {
            var levels = chart.Previous;
            var path = new PathF();
            for (var slot = 0; slot < levels.Count; slot++)
            {
                var y = baseline - (float)(levels[slot] * plotHeight);
                var left = slot * (width + gap);
                if (slot == 0)
                    path.MoveTo(left, y);
                else
                    path.LineTo(left, y);

                var right = slot == levels.Count - 1 ? left + width : left + width + gap;
                path.LineTo(Math.Min(right, totalWidth), y);
            }

            canvas.SaveState();
            canvas.StrokeColor = ink.Ink;
            canvas.StrokeSize = LineStroke;
            // The canvas reads dash lengths as multiples of the stroke size.
            canvas.StrokeDashPattern = [Dash / LineStroke, DashGap / LineStroke];
            canvas.DrawPath(path);
            canvas.RestoreState();
        }
    }
}
