using Centello.App.Theming;
using Centello.Application;
using Centello.Domain;

namespace Centello.App.Controls;

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
    private const float LabelRise = 22;
    private const float LabelGap = 3;
    private const float FootGap = 6;
    private const float TileSize = 24;
    private const float Gap = 3;
    private const float MinimumStep = 2;

    private readonly WaterfallCanvas _canvas = new();
    private readonly Grid _values = new() { ColumnSpacing = Gap };
    // The value labels with the whole text each one was given: what a label shows depends on its column.
    private readonly List<(Label Label, string Full)> _valueLabels = [];
    private readonly Grid _foot = new() { ColumnSpacing = Gap, Margin = new Thickness(0, FootGap, 0, 0) };
    private readonly Label _scale = CreateScaleLabel();

    public WaterfallChartView()
    {
        _values.SizeChanged += (_, _) => Fit();
        Content = new VerticalStackLayout
        {
            Children =
            {
                new Grid { HeightRequest = LabelRise + PlotHeight, Children = { _canvas, _values } },
                _foot,
                _scale
            }
        };
        Apply();
    }

    public WaterfallChart? Chart
    {
        get => (WaterfallChart?)GetValue(ChartProperty);
        set => SetValue(ChartProperty, value);
    }

    private static Label CreateScaleLabel()
    {
        var label = new Label
        {
            FontFamily = Fonts.Regular,
            FontSize = 12,
            HorizontalOptions = LayoutOptions.Start,
            Margin = new Thickness(0, FootGap, 0, 0)
        };
        label.SetThemeColor(Label.TextColorProperty, ThemeColor.Mute);
        AutomationProperties.SetIsInAccessibleTree(label, false);
        return label;
    }

    private static float PlotY(double level) => (float)(LabelRise + PlotHeight * (1 - level));

    private static float Baseline => PlotY(0);

    private static float ColumnWidth(double width, int columns) => (float)((width - Gap * (columns - 1)) / columns);

    private static float ColumnLeft(int column, double width, int columns) =>
        column * (ColumnWidth(width, columns) + Gap);

    private static (float Top, float Bottom) Extent(WaterfallStep step)
    {
        var top = PlotY(Math.Max(step.From, step.To));
        return (top, top + Math.Max(MinimumStep, PlotY(Math.Min(step.From, step.To)) - top));
    }

    private void Apply()
    {
        var chart = Chart;
        IsVisible = chart is not null;
        _canvas.Chart = chart;
        _scale.Text = chart?.ScaleLabel ?? "";
        _scale.IsVisible = _scale.Text.HasValue();
        _canvas.Invalidate();
        SemanticProperties.SetDescription(_canvas, chart?.Description);
        Rebuild(chart);
        Fit();
        Place();
    }

    // A value is as wide as its digits and a column is an eighth of the width: the label shrinks first, and when that
    // is not enough it takes the thousands mark, so no digit is cut off at the edge of the column.
    private void Fit()
    {
        if (Chart is not { } chart || _values.Width <= 0)
            return;

        var column = ColumnWidth(_values.Width, chart.Steps.Count + 2);
        foreach (var (label, full) in _valueLabels)
        {
            var fitted = WaterfallLabels.Fit(full, column, (text, size) => Measure(label, text, size));
            label.Text = fitted.Text;
            label.FontSize = fitted.SizeSp;
        }
    }

    private static double Measure(Label label, string text, double size)
    {
        label.Text = text;
        label.FontSize = size;
        return label.Measure(double.PositiveInfinity, double.PositiveInfinity).Width;
    }

    private void Rebuild(WaterfallChart? chart)
    {
        _valueLabels.Clear();
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
        var label = CreateLabel(text, fontFamily, WaterfallLabels.NormalSizeSp);
        label.VerticalOptions = LayoutOptions.Start;
        _valueLabels.Add((label, text));
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
            // The chart's own words stay the size of its columns when the system font grows; a screen reader gets the
            // description of the chart.
            FontAutoScalingEnabled = false,
            HorizontalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.NoWrap
        };
        label.SetThemeColor(Label.TextColorProperty, ThemeColor.Ink);
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
