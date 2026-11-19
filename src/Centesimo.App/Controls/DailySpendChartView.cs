using Centesimo.App.Theming;
using Centesimo.Application;

namespace Centesimo.App.Controls;

/// <summary>The spend chart of the home: one bar per day or month, the dashed budget pace line with its label, and
/// the axis. Built in code so the label and axis geometry stay inside the control.</summary>
public sealed class DailySpendChartView : ContentView
{
    public static readonly BindableProperty ChartProperty = BindableProperty.Create(nameof(Chart), typeof(SpendChart),
        typeof(DailySpendChartView), propertyChanged: (view, _, _) => ((DailySpendChartView)view).Apply());

    private const double BarsHeight = 104;
    // The pace label starts this far above its line. The plot keeps the same room above the tallest bar, so the
    // label fits even when the line is at the top of the scale.
    private const double PaceLabelRise = 20;
    private const double AxisGap = 8;
    private const double LabelClearance = 8;

    private readonly BarsCanvas _bars = new();
    private readonly Label _pace = CreateLabel(ThemeColor.Pace, Fonts.SemiBold);
    private readonly Label _start = CreateLabel(ThemeColor.Mute, Fonts.Regular);
    private readonly Label _highlight = CreateLabel(ThemeColor.Ink, Fonts.Bold);
    private readonly Label _end = CreateLabel(ThemeColor.Mute, Fonts.Regular);

    public DailySpendChartView()
    {
        _pace.Text = "media per restare nel budget";
        _pace.IsVisible = false;
        _pace.HorizontalOptions = LayoutOptions.End;
        _pace.VerticalOptions = LayoutOptions.Start;
        _pace.Padding = new Thickness(4, 0, 0, 0);
        // The label sits on the bars: the ground color behind it keeps the text clear of them.
        _pace.SetThemeColor(BackgroundColorProperty, ThemeColor.Ground);
        _start.HorizontalOptions = LayoutOptions.Start;
        _highlight.HorizontalOptions = LayoutOptions.Start;
        _end.HorizontalOptions = LayoutOptions.End;

        Content = new VerticalStackLayout
        {
            Spacing = AxisGap,
            Children =
            {
                new Grid { HeightRequest = PaceLabelRise + BarsHeight, Children = { _bars, _pace } },
                new Grid { Children = { _start, _end, _highlight } }
            }
        };
        SizeChanged += (_, _) => PlaceHighlight();
        _highlight.SizeChanged += (_, _) => PlaceHighlight();
    }

    public SpendChart? Chart
    {
        get => (SpendChart?)GetValue(ChartProperty);
        set => SetValue(ChartProperty, value);
    }

    private static Label CreateLabel(ThemeColor color, string fontFamily)
    {
        var label = new Label { FontFamily = fontFamily, FontSize = 12, LineBreakMode = LineBreakMode.NoWrap };
        label.SetThemeColor(Label.TextColorProperty, color);
        return label;
    }

    private void Apply()
    {
        if (Chart is not { } chart)
            return;

        _bars.Chart = chart;
        _bars.Invalidate();
        SemanticProperties.SetDescription(_bars, chart.Description);
        _pace.IsVisible = chart.HasPace;
        _pace.Margin = new Thickness(0, BarsHeight * (1 - (chart.PaceFraction ?? 0)), 0, 0);
        _start.Text = chart.StartLabel;
        _end.Text = chart.EndLabel;
        _highlight.Text = chart.HighlightLabel;
        PlaceHighlight();
    }

    // Centers the highlight under its bar and keeps it inside the chart. An end label it would touch gives way.
    private void PlaceHighlight()
    {
        if (Chart is not { } chart || Width <= 0)
            return;

        var left = Math.Clamp(Width * chart.HighlightPosition - _highlight.Width / 2, 0,
            Math.Max(0, Width - _highlight.Width));
        _highlight.TranslationX = left;
        _start.Opacity = chart.HasHighlight && left < _start.Width + LabelClearance ? 0 : 1;
        _end.Opacity = chart.HasHighlight && left + _highlight.Width > Width - _end.Width - LabelClearance ? 0 : 1;
    }

    private sealed class BarsCanvas : ThemedDrawingView
    {
        private const float Gap = 3;
        private const float FutureStub = 4;
        private const float MinimumBar = 2;
        private const float PaceStroke = 1.5f;
        private const float PaceDash = 4;
        private const float PaceDashGap = 3;

        public SpendChart? Chart { get; set; }

        protected override void Draw(ICanvas canvas, RectF bounds)
        {
            if (Chart is not { Bars.Count: > 0 } chart)
                return;

            // Hard edges: antialiasing would blur the 3dp gaps between bars.
            canvas.Antialias = false;
            var full = bounds.Height - (float)PaceLabelRise;
            var width = (bounds.Width - Gap * (chart.Bars.Count - 1)) / chart.Bars.Count;
            for (var index = 0; index < chart.Bars.Count; index++)
            {
                var bar = chart.Bars[index];
                var height = HeightOf(bar, full);
                if (height == 0)
                    continue;

                canvas.FillColor = Token(ColorOf(bar.Kind));
                canvas.FillRectangle(bounds.Left + index * (width + Gap), bounds.Bottom - height, width, height);
            }

            if (chart.PaceFraction is not { } pace)
                return;

            var y = bounds.Bottom - (float)pace * full;
            canvas.StrokeColor = Token(ThemeColor.Pace);
            canvas.StrokeSize = PaceStroke;
            // The canvas reads dash lengths as multiples of the stroke size.
            canvas.StrokeDashPattern = [PaceDash / PaceStroke, PaceDashGap / PaceStroke];
            canvas.DrawLine(bounds.Left, y, bounds.Right, y);
        }

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
}
