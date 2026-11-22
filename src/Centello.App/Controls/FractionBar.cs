using Centello.App.Theming;

namespace Centello.App.Controls;

/// <summary>A flat bar that fills a fraction of its width from the left, in the color of a category: how one amount
/// stands against another.</summary>
public sealed class FractionBar : ThemedDrawingView
{
    public static readonly BindableProperty FractionProperty = BindableProperty.Create(nameof(Fraction), typeof(double),
        typeof(FractionBar), 0d, propertyChanged: (bar, _, _) => ((FractionBar)bar).Invalidate());
    public static readonly BindableProperty ColorKeyProperty = BindableProperty.Create(nameof(ColorKey), typeof(string),
        typeof(FractionBar), "", propertyChanged: (bar, _, _) => ((FractionBar)bar).Invalidate());

    // An amount too small to see still leaves a mark.
    private const float MinimumWidth = 4;

    public double Fraction { get => (double)GetValue(FractionProperty); set => SetValue(FractionProperty, value); }
    public string ColorKey { get => (string)GetValue(ColorKeyProperty); set => SetValue(ColorKeyProperty, value); }

    protected override void Draw(ICanvas canvas, RectF bounds)
    {
        if (Fraction <= 0)
            return;

        canvas.Antialias = false;
        canvas.FillColor = Palette(ColorKey);
        canvas.FillRectangle(bounds.Left, bounds.Top, Math.Max(MinimumWidth, (float)Math.Min(Fraction, 1) * bounds.Width), bounds.Height);
    }
}
