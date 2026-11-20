using Centesimo.App.Theming;
using Centesimo.Application;

namespace Centesimo.App.Controls;

/// <summary>"Da ottobre a novembre": the start total, one floating step per change and the end total on a canvas.
/// The value labels are Labels in a Grid of the same columns over the canvas, and a foot Grid under it holds the
/// period names under the totals and a category tile under each step. Built in code so every column is the same width
/// in the canvas and in both Grids. It reads levels, keys and finished strings only.</summary>
public sealed class WaterfallChartView : ContentView
{
    public static readonly BindableProperty ChartProperty = BindableProperty.Create(nameof(Chart),
        typeof(WaterfallChart), typeof(WaterfallChartView),
        propertyChanged: (view, _, _) => ((WaterfallChartView)view).Apply());

    private const float PlotHeight = 134;
    // The value label of the highest column stands above 1.0.
    private const float LabelRise = 22;
    private const float LabelGap = 3;
    private const float FootGap = 6;
    private const float TileSize = 24;
    // The canvas gap and the column spacing of both Grids, so a label and a tile stand under the column they name.
    private const float Gap = 3;
    private const float MinimumStep = 2;

    private readonly WaterfallCanvas _canvas = new();
    private readonly Grid _values = new() { ColumnSpacing = Gap };
    private readonly Grid _foot = new() { ColumnSpacing = Gap, Margin = new Thickness(0, FootGap, 0, 0) };

    public WaterfallChartView()
    {
        Content = new VerticalStackLayout
        {
            Children =
            {
                new Grid { HeightRequest = LabelRise + PlotHeight, Children = { _canvas, _values } },
                _foot
            }
        };
        Apply();
    }

    public WaterfallChart? Chart
    {
        get => (WaterfallChart?)GetValue(ChartProperty);
        set => SetValue(ChartProperty, value);
    }

    private static float PlotY(double level) => (float)(LabelRise + PlotHeight * (1 - level));

    private static float Baseline => PlotY(0);

    private static float ColumnWidth(double width, int columns) => (float)((width - Gap * (columns - 1)) / columns);

    private static float ColumnLeft(int column, double width, int columns) =>
        column * (ColumnWidth(width, columns) + Gap);

    // The drawn extent of a step: its height is never under the minimum, and the extra goes below.
    private static (float Top, float Bottom) Extent(WaterfallStep step)
    {
        var top = PlotY(Math.Max(step.From, step.To));
        return (top, top + Math.Max(MinimumStep, PlotY(Math.Min(step.From, step.To)) - top));
    }

    /// <summary>Rebuilds both Grids with one Star column per total and step and hands the chart to the canvas. A null
    /// chart clears all of them and hides the view, so it never keeps an old drawing.</summary>
    private void Apply()
    {
        var chart = Chart;
        IsVisible = chart is not null;
        _canvas.Chart = chart;
        _canvas.Invalidate();
        SemanticProperties.SetDescription(_canvas, chart?.Description);
        Rebuild(chart);
        Place();
    }

    private void Rebuild(WaterfallChart? chart)
    {
        _values.Children.Clear();
        _values.ColumnDefinitions.Clear();
        _foot.Children.Clear();
        _foot.ColumnDefinitions.Clear();
        if (chart is null)
            return;

        var columns = chart.Steps.Count + 2;
        for (var column = 0; column < columns; column++)
        {
            _values.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            _foot.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        }

        AddColumn(0, ValueLabel(chart.Start.Value, Fonts.Bold), CaptionLabel(chart.Start.Caption));
        for (var index = 0; index < chart.Steps.Count; index++)
            AddColumn(index + 1, ValueLabel(chart.Steps[index].Value, Fonts.SemiBold), Tile(chart.Steps[index]));

        AddColumn(columns - 1, ValueLabel(chart.End.Value, Fonts.Bold), CaptionLabel(chart.End.Caption));
    }

    private void AddColumn(int column, Label value, View foot)
    {
        _values.Add(value, column, 0);
        _foot.Add(foot, column, 0);
    }

    private Label ValueLabel(string text, string fontFamily)
    {
        var label = CreateLabel(text, fontFamily, 12);
        label.VerticalOptions = LayoutOptions.Start;
        // A label knows its height only once laid out, and its place depends on it.
        label.SizeChanged += (_, _) => Place();
        return label;
    }

    private static Label CaptionLabel(string text)
    {
        var label = CreateLabel(text, Fonts.SemiBold, 13);
        label.VerticalOptions = LayoutOptions.Center;
        return label;
    }

    private static Label CreateLabel(string text, string fontFamily, double size)
    {
        var label = new Label
        {
            Text = text,
            FontFamily = fontFamily,
            FontSize = size,
            HorizontalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.NoWrap
        };
        label.SetThemeColor(Label.TextColorProperty, ThemeColor.Ink);
        // The description on the canvas already says everything the labels show.
        AutomationProperties.SetIsInAccessibleTree(label, false);
        return label;
    }

    private static CategoryTile Tile(WaterfallStep step)
    {
        var tile = new CategoryTile
        {
            IconKey = step.IconKey,
            ColorKey = step.ColorKey,
            Size = TileSize,
            HorizontalOptions = LayoutOptions.Center
        };
        AutomationProperties.SetIsInAccessibleTree(tile, false);
        return tile;
    }

    // Sets the top of each value label from the levels of its column.
    private void Place()
    {
        if (Chart is not { } chart)
            return;

        var labels = _values.Children.OfType<Label>().ToList();
        if (labels.Count != chart.Steps.Count + 2)
            return;

        PlaceAbove(labels[0], PlotY(chart.Start.Level));
        for (var index = 0; index < chart.Steps.Count; index++)
            PlaceStep(labels[index + 1], chart.Steps[index]);

        PlaceAbove(labels[^1], PlotY(chart.End.Level));
    }

    private static void PlaceAbove(Label label, float y) => label.TranslationY = y - LabelGap - label.Height;

    // A rise carries its label above and a fall below, unless the fall ends so near the foot that the label would
    // cross it: then the label goes above too.
    private static void PlaceStep(Label label, WaterfallStep step)
    {
        var (top, bottom) = Extent(step);
        if (step.IsRise || bottom + LabelGap + label.Height > Baseline)
            PlaceAbove(label, top);
        else
            label.TranslationY = bottom + LabelGap;
    }

    private sealed class WaterfallCanvas : ThemedDrawingView
    {
        // The cut at the foot of an end column: a gap, then a slice of base.
        private const float Break = 3;
        private const float Base = 3;

        public WaterfallChart? Chart { get; set; }

        protected override void Draw(ICanvas canvas, RectF bounds)
        {
            if (Chart is not { } chart)
                return;

            // Hard edges: antialiasing would blur the 3dp gaps between columns.
            canvas.Antialias = false;
            var columns = chart.Steps.Count + 2;
            var width = ColumnWidth(bounds.Width, columns);
            DrawTotal(canvas, chart.Start, ThemeColor.ChartBar, chart.IsCut, ColumnLeft(0, bounds.Width, columns), width);
            for (var index = 0; index < chart.Steps.Count; index++)
                DrawStep(canvas, chart.Steps[index], ColumnLeft(index + 1, bounds.Width, columns), width);

            DrawTotal(canvas, chart.End, ThemeColor.Ink, chart.IsCut, ColumnLeft(columns - 1, bounds.Width, columns), width);
            DrawConnectors(canvas, chart, bounds.Width, columns);
        }

        // A cut column says the scale does not start at zero: its body ends above a break and a short base follows.
        private void DrawTotal(ICanvas canvas, WaterfallTotal total, ThemeColor color, bool isCut, float left, float width)
        {
            var top = PlotY(total.Level);
            canvas.FillColor = Token(color);
            if (!isCut)
            {
                canvas.FillRectangle(left, top, width, Math.Max(MinimumStep, Baseline - top));
                return;
            }

            var bodyBottom = Baseline - Base - Break;
            if (top < bodyBottom)
                canvas.FillRectangle(left, top, width, bodyBottom - top);

            var baseTop = Math.Max(top, Baseline - Base);
            canvas.FillRectangle(left, baseTop, width, Baseline - baseTop);
        }

        private void DrawStep(ICanvas canvas, WaterfallStep step, float left, float width)
        {
            var (top, bottom) = Extent(step);
            canvas.FillColor = Palette(step.ColorKey);
            canvas.FillRectangle(left, top, width, bottom - top);
        }

        // A 1dp line across the gap after each column, at the level the next column starts from.
        private void DrawConnectors(ICanvas canvas, WaterfallChart chart, float chartWidth, int columns)
        {
            var width = ColumnWidth(chartWidth, columns);
            canvas.FillColor = Token(ThemeColor.Outline);
            for (var column = 0; column < columns - 1; column++)
            {
                var level = column < chart.Steps.Count ? chart.Steps[column].From : chart.End.Level;
                canvas.FillRectangle(ColumnLeft(column, chartWidth, columns) + width, PlotY(level), Gap, 1);
            }
        }
    }
}
