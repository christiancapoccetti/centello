using Centesimo.App.Drawing;
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
    private const double PaceLabelRise = 22;
    private const double AxisGap = 5;

    private readonly BarsCanvas _bars = new();
    private readonly Label _pace = CreateLabel(ThemeColor.Pace, Fonts.SemiBold);
    private readonly Label _start = CreateLabel(ThemeColor.Mute, Fonts.Regular);
    private readonly Label _highlight = CreateLabel(ThemeColor.Ink, Fonts.Bold);
    private readonly Label _end = CreateLabel(ThemeColor.Mute, Fonts.Regular);

    public DailySpendChartView()
    {
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
        // The chart's own words keep the size its geometry is drawn for; a screen reader gets its description.
        var label = new Label { FontFamily = fontFamily, FontSize = 12, FontAutoScalingEnabled = false, LineBreakMode = LineBreakMode.NoWrap };
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
        _pace.Text = chart.PaceLabel;
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

        var placement = ChartAxisLayout.Place(Width, chart.HighlightPosition, chart.HasHighlight, _highlight.Width,
            _start.Width, _end.Width);
        _highlight.TranslationX = placement.HighlightLeft;
        _start.Opacity = placement.ShowStart ? 1 : 0;
        _end.Opacity = placement.ShowEnd ? 1 : 0;
    }

    private sealed class BarsCanvas : ThemedDrawingView
    {
        public SpendChart? Chart { get; set; }

        protected override void Draw(ICanvas canvas, RectF bounds)
        {
            if (Chart is not { } chart)
                return;

            var plot = new RectF(bounds.Left, bounds.Top + (float)PaceLabelRise, bounds.Width,
                bounds.Height - (float)PaceLabelRise);
            SpendBarsPainter.Draw(canvas, plot, chart, IsDark);
        }
    }
}
