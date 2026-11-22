using Centello.App.Theming;
using Centello.Application;

namespace Centello.App.Controls;

/// <summary>The spending of one category over a period: a bar per day or per month in the color of the category.
/// A day without spending and a day to come are a stub on the hairline.</summary>
public sealed class CategoryBarsView : ThemedDrawingView
{
    public static readonly BindableProperty BarsProperty = BindableProperty.Create(nameof(Bars),
        typeof(IReadOnlyList<SpendBar>), typeof(CategoryBarsView),
        propertyChanged: (view, _, _) => ((CategoryBarsView)view).Invalidate());
    public static readonly BindableProperty ColorKeyProperty = BindableProperty.Create(nameof(ColorKey), typeof(string),
        typeof(CategoryBarsView), "", propertyChanged: (view, _, _) => ((CategoryBarsView)view).Invalidate());

    private const float DayGap = 2;
    private const float MonthGap = 6;
    private const float Stub = 3;

    public IReadOnlyList<SpendBar>? Bars
    {
        get => (IReadOnlyList<SpendBar>?)GetValue(BarsProperty);
        set => SetValue(BarsProperty, value);
    }

    public string ColorKey { get => (string)GetValue(ColorKeyProperty); set => SetValue(ColorKeyProperty, value); }

    protected override void Draw(ICanvas canvas, RectF bounds)
    {
        if (Bars is not { Count: > 0 } bars)
            return;

        // Hard edges: antialiasing would blur the gaps between bars.
        canvas.Antialias = false;
        var gap = bars.Count > 12 ? DayGap : MonthGap;
        var width = (bounds.Width - gap * (bars.Count - 1)) / bars.Count;
        for (var index = 0; index < bars.Count; index++)
        {
            var bar = bars[index];
            var spent = bar.Kind != SpendBarKind.Future && bar.Fraction > 0;
            var height = spent ? Math.Max(Stub, (float)bar.Fraction * bounds.Height) : Stub;
            canvas.FillColor = spent ? Palette(ColorKey) : Token(ThemeColor.Hairline);
            canvas.FillRectangle(bounds.Left + index * (width + gap), bounds.Bottom - height, width, height);
        }
    }
}
