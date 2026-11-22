namespace Centello.App.Drawing;

internal readonly record struct AxisPlacement(double HighlightLeft, bool ShowStart, bool ShowEnd);

/// <summary>Where the highlight label of the daily spend chart goes: centred under its bar, kept inside the chart,
/// and an end label it would touch gives way.</summary>
internal static class ChartAxisLayout
{
    private const double LabelClearance = 8;

    internal static AxisPlacement Place(double chartWidth, double highlightPosition, bool hasHighlight,
        double highlightWidth, double startWidth, double endWidth)
    {
        var left = Math.Clamp(chartWidth * highlightPosition - highlightWidth / 2, 0,
            Math.Max(0, chartWidth - highlightWidth));
        return new AxisPlacement(left,
            !(hasHighlight && left < startWidth + LabelClearance),
            !(hasHighlight && left + highlightWidth > chartWidth - endWidth - LabelClearance));
    }
}
