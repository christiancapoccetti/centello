using Centello.App.Theming;
using Centello.Application;
using Microsoft.Maui.Controls.Shapes;

namespace Centello.App.Controls;

/// <summary>A thin line with a bar sliding along it: the work is going on and nobody can say how far it is. The
/// slide runs only while the control is visible and on screen.</summary>
public sealed class IndeterminateBar : Border
{
    private const string AnimationName = "slide";
    private const double BarShare = 0.4;

    private readonly BoxView _bar = new() { HorizontalOptions = LayoutOptions.Start, CornerRadius = 2 };

    public IndeterminateBar()
    {
        HeightRequest = 4;
        StrokeThickness = 0;
        StrokeShape = new RoundRectangle { CornerRadius = 2 };
        this.SetThemeColor(BackgroundColorProperty, ThemeColor.Hairline);
        _bar.SetThemeColor(BoxView.ColorProperty, ThemeColor.Ink);
        Content = _bar;
        SizeChanged += (_, _) => Restart();
        Loaded += (_, _) => Restart();
        Unloaded += (_, _) => this.AbortAnimation(AnimationName);
        SemanticProperties.SetDescription(this, "In corso");
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == IsVisibleProperty.PropertyName)
            Restart();
    }

    private void Restart()
    {
        this.AbortAnimation(AnimationName);
        if (!IsVisible || Width <= 0)
            return;

        var barWidth = Width * BarShare;
        _bar.WidthRequest = barWidth;
        new Animation(position => _bar.TranslationX = position, -barWidth, Width, Easing.CubicInOut)
            .Commit(this, AnimationName, length: 1300, repeat: () => true);
    }
}
