using Centesimo.App.Theming;
using Centesimo.Application;

namespace Centesimo.App.Controls;

/// <summary>A flat swatch of one palette slot that follows the theme. Unknown keys fall back to "grey".
/// The description is the color's Italian name.</summary>
public sealed class PaletteSwatch : BoxView
{
    public static readonly BindableProperty ColorKeyProperty = BindableProperty.Create(nameof(ColorKey), typeof(string),
        typeof(PaletteSwatch), propertyChanged: (swatch, _, key) => ((PaletteSwatch)swatch).Apply((string?)key));

    public PaletteSwatch() => Apply(null);

    public string? ColorKey { get => (string?)GetValue(ColorKeyProperty); set => SetValue(ColorKeyProperty, value); }

    private void Apply(string? key)
    {
        this.SetPaletteColor(ColorProperty, key);
        SemanticProperties.SetDescription(this, CategoryPalette.Resolve(key).Name);
    }
}
