using Centesimo.Application;

namespace Centesimo.App.Controls;

/// <summary>The category line of the home: one flat segment per category, as wide as its share of the spending.</summary>
public sealed class CategoryLineView : ThemedDrawingView
{
    public static readonly BindableProperty SegmentsProperty = BindableProperty.Create(nameof(Segments),
        typeof(IReadOnlyList<CategorySegment>), typeof(CategoryLineView),
        propertyChanged: (view, _, _) => ((CategoryLineView)view).Invalidate());

    private const float Gap = 3;
    private const float MinimumWidth = 2;

    public IReadOnlyList<CategorySegment>? Segments
    {
        get => (IReadOnlyList<CategorySegment>?)GetValue(SegmentsProperty);
        set => SetValue(SegmentsProperty, value);
    }

    protected override void Draw(ICanvas canvas, RectF bounds)
    {
        if (Segments is not { Count: > 0 } segments)
            return;

        // Hard edges: antialiasing would blur the 3dp gaps between segments.
        canvas.Antialias = false;
        var widths = Widths(segments, bounds.Width - Gap * (segments.Count - 1));
        var left = bounds.Left;
        for (var index = 0; index < segments.Count; index++)
        {
            canvas.FillColor = Palette(segments[index].ColorKey);
            canvas.FillRectangle(left, bounds.Top, widths[index], bounds.Height);
            left += widths[index] + Gap;
        }
    }

    // A share too small to see still gets the minimum width. The other segments split what is left in proportion,
    // so the line keeps its full width.
    private static float[] Widths(IReadOnlyList<CategorySegment> segments, float available)
    {
        var total = segments.Sum(segment => (double)segment.Cents);
        var slivers = segments
            .Select(segment => available * segment.Cents / total < MinimumWidth)
            .ToList();
        var shared = available - slivers.Count(isSliver => isSliver) * MinimumWidth;
        var sharing = segments
            .Where((_, index) => !slivers[index])
            .Sum(segment => (double)segment.Cents);
        return segments
            .Select((segment, index) => slivers[index] ? MinimumWidth : (float)(shared * segment.Cents / sharing))
            .ToArray();
    }
}
