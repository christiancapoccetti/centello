using Centello.App.Theming;
using Centello.Application;
using Centello.Domain;
using Microsoft.Maui.Layouts;

namespace Centello.App.Controls;

/// <summary>"Spesa accumulata": one slot per day or month on a canvas, the previous period as a dashed line, the
/// budget hairline and the gap marker. Every text is a Label over or under the canvas, because a canvas cannot show
/// tabular figures, and the key sits below. Built in code so the label geometry stays inside the control. It reads
/// fractions, slot indices and finished strings only.</summary>
public sealed class PaceChartView : ContentView
{
    public static readonly BindableProperty ChartProperty = BindableProperty.Create(nameof(Chart), typeof(PaceChart),
        typeof(PaceChartView), propertyChanged: (view, _, _) => ((PaceChartView)view).Apply());

    private const float PlotHeight = 148;
    private const float LabelRise = 22;
    private const float LabelLift = 3;
    private const float FootRoom = 1;
    private const float AxisGap = 5;
    private const float LabelClearance = 8;
    private const float SlotGap = 2;
    private const float MarkerWidth = 2;
    private const float MarkerOffset = 5;
    private const float GapLabelMargin = 2;
    private const float GapLabelDrop = 6;
    private const float LineStroke = 2;
    private const float Dash = 4;
    private const float DashGap = 3;

    private readonly PaceCanvas _canvas = new();
    private readonly Label _budget = CreateLabel(ThemeColor.Mute, Fonts.Regular);
    private readonly Label _close = CreateLabel(ThemeColor.Mute, Fonts.Regular);
    private readonly Label _gapAmount = CreateLabel(ThemeColor.Ink, Fonts.Bold, 13);
    private readonly Label _gapCaption = CreateLabel(ThemeColor.Mute, Fonts.Regular);
    private readonly VerticalStackLayout _gapLabel;
    private readonly Label _start = CreateLabel(ThemeColor.Mute, Fonts.Regular);
    private readonly Label _today = CreateLabel(ThemeColor.Ink, Fonts.Bold);
    private readonly Label _end = CreateLabel(ThemeColor.Mute, Fonts.Regular);
    private readonly FlexLayout _key = new()
    {
        Wrap = FlexWrap.Wrap,
        AlignItems = FlexAlignItems.Center,
        Margin = new Thickness(0, 12, 0, -6)
    };

    public PaceChartView()
    {
        _gapLabel = new VerticalStackLayout { Padding = new Thickness(4, 0), Children = { _gapAmount, _gapCaption } };
        _budget.Padding = new Thickness(0, 0, 4, 0);
        _budget.HorizontalOptions = LayoutOptions.Start;
        _close.Padding = new Thickness(4, 0, 0, 0);
        _close.HorizontalOptions = LayoutOptions.End;
        _gapLabel.HorizontalOptions = LayoutOptions.Start;
        View[] overlays = [_budget, _close, _gapLabel];
        foreach (var overlay in overlays)
        {
            overlay.VerticalOptions = LayoutOptions.Start;
            overlay.SetThemeColor(BackgroundColorProperty, ThemeColor.Ground);
        }

        _start.HorizontalOptions = LayoutOptions.Start;
        _today.HorizontalOptions = LayoutOptions.Start;
        _end.HorizontalOptions = LayoutOptions.Start;

        Content = new VerticalStackLayout
        {
            Children =
            {
                new Grid
                {
                    HeightRequest = LabelRise + PlotHeight + FootRoom,
                    Children = { _canvas, _budget, _close, _gapLabel }
                },
                new Grid { Margin = new Thickness(0, AxisGap, 0, 0), Children = { _start, _end, _today } },
                _key
            }
        };

        View[] placed = [this, _budget, _close, _gapLabel, _start, _today, _end];
        foreach (var view in placed)
            view.SizeChanged += (_, _) => Place();

        Apply();
    }

    public PaceChart? Chart
    {
        get => (PaceChart?)GetValue(ChartProperty);
        set => SetValue(ChartProperty, value);
    }

    private static Label CreateLabel(ThemeColor color, string fontFamily, double size = 12)
    {
        // The chart's own words keep the size its geometry is drawn for; a screen reader gets its description.
        var label = new Label { FontFamily = fontFamily, FontSize = size, FontAutoScalingEnabled = false, LineBreakMode = LineBreakMode.NoWrap };
        label.SetThemeColor(Label.TextColorProperty, color);
        AutomationProperties.SetIsInAccessibleTree(label, false);
        return label;
    }

    private static float PlotY(double fraction) => (float)(LabelRise + PlotHeight * (1 - fraction));

    private static float Baseline => PlotY(0);

    private static float SlotWidth(double width, int count) => (float)((width - SlotGap * (count - 1)) / count);

    private static float SlotLeft(int slot, double width, int count) => slot * (SlotWidth(width, count) + SlotGap);

    private static float SlotRight(int slot, double width, int count) =>
        SlotLeft(slot, width, count) + SlotWidth(width, count);

    private static float MarkerLeft(int slot, double width, int count) =>
        (float)Math.Min(SlotRight(slot, width, count) + MarkerOffset, width - MarkerWidth);

    private static View KeyItem(PaceKeyItem item)
    {
        var text = new FormattedString();
        text.Spans.Add(new Span { Text = item.Name });
        if (item.Amount.HasValue())
        {
            var amount = new Span { Text = " " + item.Amount, FontFamily = Fonts.SemiBold };
            amount.SetThemeColor(Span.TextColorProperty, ThemeColor.Ink);
            text.Spans.Add(amount);
        }

        var label = new Label
        {
            FormattedText = text,
            FontFamily = Fonts.Regular,
            FontSize = 13,
            VerticalOptions = LayoutOptions.Center
        };
        label.SetThemeColor(Label.TextColorProperty, ThemeColor.Mute);
        AutomationProperties.SetIsInAccessibleTree(label, false);
        return new HorizontalStackLayout
        {
            Spacing = 6,
            Padding = new Thickness(0, 0, 16, 6),
            Children = { new KeySwatch(item.Series), label }
        };
    }

    private void Apply()
    {
        var chart = Chart;
        IsVisible = chart is not null;
        _canvas.Chart = chart;
        _canvas.Invalidate();
        SemanticProperties.SetDescription(_canvas, chart?.Description);
        SetText(_budget, chart?.Budget?.Label);
        SetText(_close, chart?.Previous?.CloseLabel);
        SetText(_gapAmount, chart?.Gap?.Amount);
        SetText(_gapCaption, chart?.Gap?.Caption);
        _gapLabel.IsVisible = chart?.Gap is not null;
        SetText(_start, chart?.Axis.Start.Text);
        SetText(_today, chart?.Axis.Today?.Text);
        SetText(_end, chart?.Axis.End.Text);
        _key.Children.Clear();
        foreach (var item in chart?.Key ?? [])
            _key.Children.Add(KeyItem(item));

        Place();
    }

    private static void SetText(Label label, string? text)
    {
        label.Text = text ?? "";
        label.IsVisible = text is not null;
    }

    private void Place()
    {
        if (Chart is not { Slots.Count: > 0 } chart || Width <= 0)
            return;

        var count = chart.Slots.Count;
        if (chart.Budget is { } budget)
            _budget.TranslationY = PlotY(budget.Fraction) - _budget.Height - LabelLift;

        if (chart.Previous is { Levels.Count: > 0 } previous)
            _close.TranslationY = PlotY(previous.Levels[^1]) - _close.Height - LabelLift;

        if (chart.Gap is { } gap)
            PlaceGapLabel(gap, count);

        PlaceAxis(chart.Axis, count);
    }

    private void PlaceGapLabel(PaceGap gap, int count)
    {
        var slotLeft = SlotLeft(gap.Slot, Width, count);
        var marker = MarkerLeft(gap.Slot, Width, count);
        var besideMarker = marker + MarkerWidth + GapLabelMargin;
        var onRight = besideMarker + _gapLabel.Width <= Width;
        var left = onRight ? besideMarker : slotLeft - GapLabelMargin - _gapLabel.Width;
        var alignment = onRight ? TextAlignment.Start : TextAlignment.End;
        _gapAmount.HorizontalTextAlignment = alignment;
        _gapCaption.HorizontalTextAlignment = alignment;
        _gapLabel.TranslationX = Math.Max(0, left);
        var top = PlotY(Math.Min(gap.From, gap.To)) + GapLabelDrop;
        _gapLabel.TranslationY = Math.Max(0, Math.Min(top, Baseline - _gapLabel.Height));
    }

    private void PlaceAxis(PaceAxis axis, int count)
    {
        var endLeft = SlotRight(Math.Clamp(axis.End.Slot, 0, count - 1), Width, count) - _end.Width;
        _end.TranslationX = endLeft;
        _start.Opacity = 1;
        _end.Opacity = 1;
        if (axis.Today is not { } today)
            return;

        var slot = Math.Clamp(today.Slot, 0, count - 1);
        var center = SlotLeft(slot, Width, count) + SlotWidth(Width, count) / 2;
        var left = Math.Clamp(center - _today.Width / 2, 0, Math.Max(0, Width - _today.Width));
        _today.TranslationX = left;
        if (left < _start.Width + LabelClearance)
            _start.Opacity = 0;

        if (left + _today.Width > endLeft - LabelClearance)
            _end.Opacity = 0;
    }

    private sealed class PaceCanvas : ThemedDrawingView
    {
        private const float FutureStub = 4;
        private const float MinimumBar = 2;
        private const float Band = 2;

        public PaceChart? Chart { get; set; }

        protected override void Draw(ICanvas canvas, RectF bounds)
        {
            if (Chart is not { Slots.Count: > 0 } chart)
                return;

            // Hard edges: antialiasing would blur the 2dp gaps between slots.
            canvas.Antialias = false;
            if (chart.Budget is { } budget)
                DrawBudget(canvas, budget, bounds.Width);

            DrawSlots(canvas, chart.Slots, bounds.Width);
            if (chart.Previous is { Levels.Count: > 0 } previous)
                DrawPrevious(canvas, previous, bounds.Width, chart.Slots.Count);

            if (chart.Gap is { } gap)
                DrawGap(canvas, gap, bounds.Width, chart.Slots.Count);
        }

        private void DrawBudget(ICanvas canvas, PaceBudget budget, float width)
        {
            var y = PlotY(budget.Fraction);
            canvas.StrokeColor = Token(ThemeColor.Outline);
            canvas.StrokeSize = 1;
            canvas.DrawLine(0, y, width, y);
        }

        private void DrawSlots(ICanvas canvas, IReadOnlyList<PaceSlot> slots, float width)
        {
            for (var index = 0; index < slots.Count; index++)
            {
                var slot = slots[index];
                var height = HeightOf(slot);
                if (height == 0)
                    continue;

                var left = SlotLeft(index, width, slots.Count);
                var slotWidth = SlotWidth(width, slots.Count);
                canvas.FillColor = Token(ColorOf(slot.Kind));
                canvas.FillRectangle(left, Baseline - height, slotWidth, height);
                DrawAutomatic(canvas, slot, left, slotWidth, height);
            }
        }

        private void DrawAutomatic(ICanvas canvas, PaceSlot slot, float left, float width, float barHeight)
        {
            if (slot.Automatic <= 0)
                return;

            var automatic = Math.Min(barHeight, Math.Max(MinimumBar, Baseline - PlotY(slot.Automatic)));
            canvas.FillColor = Token(ThemeColor.Mute);
            canvas.FillRectangle(left, Baseline - automatic, width, automatic);
            if (barHeight - automatic <= Band)
                return;

            canvas.FillColor = Token(ThemeColor.Ground);
            canvas.FillRectangle(left, Baseline - automatic - Band, width, Band);
        }

        private void DrawPrevious(ICanvas canvas, PaceLine line, float width, int count)
        {
            var path = new PathF();
            path.MoveTo(0, Baseline);
            var y = Baseline;
            for (var index = 0; index < line.Levels.Count; index++)
            {
                var left = SlotLeft(index, width, count);
                path.LineTo(left, y);
                y = PlotY(line.Levels[index]);
                path.LineTo(left, y);
            }

            path.LineTo(width, y);
            canvas.SaveState();
            canvas.StrokeColor = Token(ThemeColor.Ink);
            canvas.StrokeSize = LineStroke;
            // The canvas reads dash lengths as multiples of the stroke size.
            canvas.StrokeDashPattern = [Dash / LineStroke, DashGap / LineStroke];
            canvas.DrawPath(path);
            canvas.RestoreState();
        }

        private void DrawGap(ICanvas canvas, PaceGap gap, float width, int count)
        {
            var top = PlotY(Math.Max(gap.From, gap.To));
            var height = Math.Max(MarkerWidth, PlotY(Math.Min(gap.From, gap.To)) - top);
            canvas.FillColor = Token(ThemeColor.Ink);
            canvas.FillRectangle(MarkerLeft(gap.Slot, width, count), top, MarkerWidth, height);
        }

        private static float HeightOf(PaceSlot slot) => slot.Kind switch
        {
            PaceSlotKind.Spent => slot.Total > 0 ? Math.Max(MinimumBar, Baseline - PlotY(slot.Total)) : 0,
            PaceSlotKind.Today => Math.Max(MinimumBar, Baseline - PlotY(slot.Total)),
            PaceSlotKind.Ahead => FutureStub,
            PaceSlotKind.Outside => 0
        };

        private static ThemeColor ColorOf(PaceSlotKind kind) => kind switch
        {
            PaceSlotKind.Spent => ThemeColor.ChartBar,
            PaceSlotKind.Today => ThemeColor.Ink,
            PaceSlotKind.Ahead => ThemeColor.Hairline,
            PaceSlotKind.Outside => ThemeColor.Ground
        };
    }

    private sealed class KeySwatch : ThemedDrawingView
    {
        private readonly PaceSeries _series;

        public KeySwatch(PaceSeries series)
        {
            _series = series;
            WidthRequest = series == PaceSeries.Previous ? 14 : 10;
            HeightRequest = 10;
            VerticalOptions = LayoutOptions.Center;
            AutomationProperties.SetIsInAccessibleTree(this, false);
        }

        protected override void Draw(ICanvas canvas, RectF bounds)
        {
            canvas.Antialias = false;
            if (_series != PaceSeries.Previous)
            {
                canvas.FillColor = Token(ColorOf(_series));
                canvas.FillRectangle(bounds);
                return;
            }

            canvas.StrokeColor = Token(ColorOf(_series));
            canvas.StrokeSize = LineStroke;
            canvas.StrokeDashPattern = [Dash / LineStroke, DashGap / LineStroke];
            canvas.DrawLine(bounds.Left, bounds.Center.Y, bounds.Right, bounds.Center.Y);
        }

        private static ThemeColor ColorOf(PaceSeries series) => series switch
        {
            PaceSeries.Automatic => ThemeColor.Mute,
            PaceSeries.Variable => ThemeColor.ChartBar,
            PaceSeries.Spent => ThemeColor.ChartBar,
            PaceSeries.Previous => ThemeColor.Ink
        };
    }
}
