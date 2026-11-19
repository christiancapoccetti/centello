using Centesimo.App.Theming;
using Centesimo.Application;
using Microsoft.Maui.Controls.Shapes;

namespace Centesimo.App.Controls;

/// <summary>The only place a category's icon and color keys become pixels: a square pastel tile with a dark glyph
/// that follows the theme. Unknown keys fall back to "more" and "grey". The description is the icon's Italian label.</summary>
public sealed class CategoryTile : Border
{
    public static readonly BindableProperty IconKeyProperty = BindableProperty.Create(nameof(IconKey), typeof(string),
        typeof(CategoryTile), propertyChanged: (tile, _, key) => ((CategoryTile)tile).ApplyIcon((string?)key));
    public static readonly BindableProperty ColorKeyProperty = BindableProperty.Create(nameof(ColorKey), typeof(string),
        typeof(CategoryTile), propertyChanged: (tile, _, key) => tile.SetPaletteColor(BackgroundColorProperty, (string?)key));
    public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(double),
        typeof(CategoryTile), 36d, propertyChanged: (tile, _, size) => ((CategoryTile)tile).ApplySize((double)size));

    private const double GlyphScale = 0.58;
    private readonly Label _glyph = new()
    {
        FontFamily = Fonts.Icons,
        HorizontalOptions = LayoutOptions.Center,
        VerticalOptions = LayoutOptions.Center
    };

    public CategoryTile()
    {
        StrokeThickness = 0;
        StrokeShape = new Rectangle();
        Content = _glyph;
        // The glyph is a private-use codepoint a screen reader cannot speak. The tile carries the label.
        AutomationProperties.SetIsInAccessibleTree(_glyph, false);
        _glyph.SetThemeColor(Label.TextColorProperty, ThemeColor.TileInk);
        ApplyIcon(null);
        this.SetPaletteColor(BackgroundColorProperty, null);
        ApplySize(Size);
    }

    public string? IconKey { get => (string?)GetValue(IconKeyProperty); set => SetValue(IconKeyProperty, value); }
    public string? ColorKey { get => (string?)GetValue(ColorKeyProperty); set => SetValue(ColorKeyProperty, value); }
    public double Size { get => (double)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    private void ApplyIcon(string? key)
    {
        var icon = CategoryIcons.Resolve(key);
        _glyph.Text = icon.Glyph;
        SemanticProperties.SetDescription(this, icon.Label);
    }

    private void ApplySize(double size)
    {
        WidthRequest = size;
        HeightRequest = size;
        _glyph.FontSize = Math.Round(size * GlyphScale);
    }
}
