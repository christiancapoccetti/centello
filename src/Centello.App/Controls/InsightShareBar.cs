using Centello.App.Theming;
using Centello.Application;

namespace Centello.App.Controls;

public sealed class InsightShareBar : ThemedDrawingView
{
    public static readonly BindableProperty FractionProperty = BindableProperty.Create(nameof(Fraction), typeof(double),
        typeof(InsightShareBar), 0d, propertyChanged: (bar, _, _) => ((InsightShareBar)bar).Invalidate());
    public static readonly BindableProperty MarkerProperty = BindableProperty.Create(nameof(Marker), typeof(double?),
        typeof(InsightShareBar), propertyChanged: (bar, _, _) => ((InsightShareBar)bar).Invalidate());
    public static readonly BindableProperty ColorKeyProperty = BindableProperty.Create(nameof(ColorKey), typeof(string),
        typeof(InsightShareBar), "", propertyChanged: (bar, _, _) => ((InsightShareBar)bar).Invalidate());

    private const float MinimumWidth = 2;

    public double Fraction { get => (double)GetValue(FractionProperty); set => SetValue(FractionProperty, value); }
    public double? Marker { get => (double?)GetValue(MarkerProperty); set => SetValue(MarkerProperty, value); }
    public string ColorKey { get => (string)GetValue(ColorKeyProperty); set => SetValue(ColorKeyProperty, value); }

    protected override void Draw(ICanvas canvas, RectF bounds)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        var minimumWidth = Math.Min(MinimumWidth, bounds.Width);
        canvas.Antialias = false;
        canvas.FillColor = Token(ThemeColor.Hairline);
        canvas.FillRectangle(bounds);
        if (Fraction > 0)
        {
            canvas.FillColor = Palette(ColorKey);
            var width = Math.Max(minimumWidth, (float)Math.Min(Fraction, 1) * bounds.Width);
            canvas.FillRectangle(bounds.Left, bounds.Top, width, bounds.Height);
        }

        if (Marker is not { } marker)
            return;

        var point = bounds.Left + (float)marker * bounds.Width;
        var markerLeft = Math.Clamp(point - minimumWidth / 2, bounds.Left, bounds.Right - minimumWidth);
        canvas.FillColor = Token(ThemeColor.Ink);
        canvas.FillRectangle(markerLeft, bounds.Top, minimumWidth, bounds.Height);
    }
}
