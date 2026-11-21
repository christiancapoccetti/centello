using Centesimo.App.Drawing;
using Centesimo.Application;

namespace Centesimo.App.Controls;

/// <summary>The category line of the home: one flat segment per category, as wide as its share of the spending.</summary>
public sealed class CategoryLineView : ThemedDrawingView
{
    public static readonly BindableProperty SegmentsProperty = BindableProperty.Create(nameof(Segments),
        typeof(IReadOnlyList<CategorySegment>), typeof(CategoryLineView),
        propertyChanged: (view, _, _) => ((CategoryLineView)view).Invalidate());

    public IReadOnlyList<CategorySegment>? Segments
    {
        get => (IReadOnlyList<CategorySegment>?)GetValue(SegmentsProperty);
        set => SetValue(SegmentsProperty, value);
    }

    protected override void Draw(ICanvas canvas, RectF bounds)
    {
        if (Segments is { } segments)
            CategoryLinePainter.Draw(canvas, bounds, segments, IsDark);
    }
}
