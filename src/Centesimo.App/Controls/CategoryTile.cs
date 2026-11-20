using Centesimo.App.Theming;
using Centesimo.Application;
using Microsoft.Maui.Controls.Shapes;

namespace Centesimo.App.Controls;

/// <summary>The only place a category's icon and color keys become pixels: a square pastel tile with a dark glyph
/// that follows the theme. Unknown keys fall back to "more" and "grey". The description is the icon's Italian label.
/// A neutral tile is the raised surface with a mute glyph: an icon on offer that is not the category's own.</summary>
public sealed class CategoryTile : Border
{
    public static readonly BindableProperty IconKeyProperty = BindableProperty.Create(nameof(IconKey), typeof(string),
        typeof(CategoryTile), propertyChanged: (tile, _, key) => ((CategoryTile)tile).ApplyIcon((string?)key));
    public static readonly BindableProperty ColorKeyProperty = BindableProperty.Create(nameof(ColorKey), typeof(string),
        typeof(CategoryTile), propertyChanged: (tile, _, _) => ((CategoryTile)tile).ApplyLook());
    public static readonly BindableProperty IsNeutralProperty = BindableProperty.Create(nameof(IsNeutral), typeof(bool),
        typeof(CategoryTile), false, propertyChanged: (tile, _, _) => ((CategoryTile)tile).ApplyLook());
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
        ApplyIcon(null);
        ApplyLook();
        ApplySize(Size);
    }

    public string? IconKey { get => (string?)GetValue(IconKeyProperty); set => SetValue(IconKeyProperty, value); }
    public string? ColorKey { get => (string?)GetValue(ColorKeyProperty); set => SetValue(ColorKeyProperty, value); }
    public bool IsNeutral { get => (bool)GetValue(IsNeutralProperty); set => SetValue(IsNeutralProperty, value); }
    public double Size { get => (double)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    private void ApplyLook()
    {
        if (IsNeutral)
        {
            this.SetThemeColor(BackgroundColorProperty, ThemeColor.SurfaceRaised);
            _glyph.SetThemeColor(Label.TextColorProperty, ThemeColor.Mute);
            return;
        }

        this.SetPaletteColor(BackgroundColorProperty, ColorKey);
        _glyph.SetThemeColor(Label.TextColorProperty, ThemeColor.TileInk);
    }

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
