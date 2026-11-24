using Centello.App.Theming;

namespace Centello.App.Controls;

public sealed class InsightDeltaBar : ThemedDrawingView
{
    public static readonly BindableProperty FractionProperty = BindableProperty.Create(nameof(Fraction), typeof(double),
        typeof(InsightDeltaBar), 0d, propertyChanged: (bar, _, _) => ((InsightDeltaBar)bar).Invalidate());
    public static readonly BindableProperty ZeroProperty = BindableProperty.Create(nameof(Zero), typeof(double),
        typeof(InsightDeltaBar), 0.5d, propertyChanged: (bar, _, _) => ((InsightDeltaBar)bar).Invalidate());
    public static readonly BindableProperty ColorKeyProperty = BindableProperty.Create(nameof(ColorKey), typeof(string),
        typeof(InsightDeltaBar), "", propertyChanged: (bar, _, _) => ((InsightDeltaBar)bar).Invalidate());

    public double Fraction { get => (double)GetValue(FractionProperty); set => SetValue(FractionProperty, value); }
    public double Zero { get => (double)GetValue(ZeroProperty); set => SetValue(ZeroProperty, value); }
    public string ColorKey { get => (string)GetValue(ColorKeyProperty); set => SetValue(ColorKeyProperty, value); }

    protected override void Draw(ICanvas canvas, RectF bounds)
    {
        var zero = bounds.Left + (float)Zero * bounds.Width;
        canvas.Antialias = false;
        canvas.FillColor = Token(ThemeColor.Hairline);
        canvas.FillRectangle(bounds);
        if (Fraction != 0)
        {
            var width = Math.Max(1, (float)Math.Abs(Fraction) * bounds.Width);
            var end = Math.Clamp(zero + Math.Sign(Fraction) * width, bounds.Left, bounds.Right);
            canvas.FillColor = Palette(ColorKey);
            canvas.FillRectangle(Math.Min(zero, end), bounds.Top, Math.Abs(zero - end), bounds.Height);
        }

        canvas.FillColor = Token(ThemeColor.Mute);
        canvas.FillRectangle(zero - 0.5f, bounds.Top, 1, bounds.Height);
    }
}
