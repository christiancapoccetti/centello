using Centesimo.App.Theming;
using Centesimo.Application;

namespace Centesimo.App.Controls;

/// <summary>A display-only GraphicsView that knows the scheme: IsDark follows the app theme and a flip redraws.
/// Subclasses read colors only through Token and Palette.</summary>
public abstract class ThemedDrawingView : GraphicsView, IDrawable
{
    private static readonly BindableProperty IsDarkProperty = BindableProperty.Create(nameof(IsDark), typeof(bool),
        typeof(ThemedDrawingView), false, propertyChanged: (view, _, _) => ((ThemedDrawingView)view).Invalidate());

    protected ThemedDrawingView()
    {
        Drawable = this;
        // The platform view takes touches by default, which would keep a drag that starts on it from scrolling the page.
        InputTransparent = true;
        this.SetAppTheme(IsDarkProperty, false, true);
    }

    protected bool IsDark => (bool)GetValue(IsDarkProperty);

    protected Color Token(ThemeColor color) => ThemeColors.Resolve(color, IsDark);

    protected Color Palette(string colorKey) => ThemeColors.ResolvePalette(colorKey, IsDark);

    void IDrawable.Draw(ICanvas canvas, RectF dirtyRect) => Draw(canvas, dirtyRect);

    protected abstract void Draw(ICanvas canvas, RectF bounds);
}
