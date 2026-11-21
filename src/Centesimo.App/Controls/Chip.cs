using Centesimo.App.Theming;
using Centesimo.Application;
using Centesimo.Domain;
using Microsoft.Maui.Controls.Shapes;

namespace Centesimo.App.Controls;

/// <summary>A choice chip: 32 high inside a 48 touch target. Selected draws the ink ring. What leads the text is, in
/// this order, the tile of a category (icon and color keys), the dot of a category (color key alone), a glyph of its
/// own, or the check of a selected chip. A trailing glyph names what a tap does to the chip, such as removing it.</summary>
public sealed class Chip : ContentView
{
    public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string),
        typeof(Chip), "", propertyChanged: (chip, _, _) => ((Chip)chip).Apply());
    public static readonly BindableProperty GlyphProperty = BindableProperty.Create(nameof(Glyph), typeof(string),
        typeof(Chip), "", propertyChanged: (chip, _, _) => ((Chip)chip).Apply());
    public static readonly BindableProperty TrailingGlyphProperty = BindableProperty.Create(nameof(TrailingGlyph), typeof(string),
        typeof(Chip), "", propertyChanged: (chip, _, _) => ((Chip)chip).Apply());
    public static readonly BindableProperty IconKeyProperty = BindableProperty.Create(nameof(IconKey), typeof(string),
        typeof(Chip), "", propertyChanged: (chip, _, _) => ((Chip)chip).Apply());
    public static readonly BindableProperty ColorKeyProperty = BindableProperty.Create(nameof(ColorKey), typeof(string),
        typeof(Chip), "", propertyChanged: (chip, _, _) => ((Chip)chip).Apply());
    public static readonly BindableProperty IsSelectedProperty = BindableProperty.Create(nameof(IsSelected), typeof(bool),
        typeof(Chip), false, propertyChanged: (chip, _, _) => ((Chip)chip).Apply());
    public static readonly BindableProperty HasCheckProperty = BindableProperty.Create(nameof(HasCheck), typeof(bool),
        typeof(Chip), true, propertyChanged: (chip, _, _) => ((Chip)chip).Apply());
    public static readonly BindableProperty DescriptionProperty = BindableProperty.Create(nameof(Description), typeof(string),
        typeof(Chip), "", propertyChanged: (chip, _, _) => ((Chip)chip).Apply());

    private const double SidePadding = 12;
    private const double TilePadding = 4;
    private const double TrailingPadding = 4;
    private readonly Label _glyph = new()
    {
        FontFamily = Fonts.Icons,
        FontAutoScalingEnabled = false,
        FontSize = 18,
        Margin = new Thickness(-4, 0, 0, 0),
        VerticalTextAlignment = TextAlignment.Center
    };
    private readonly CategoryTile _tile = new() { Size = 24, VerticalOptions = LayoutOptions.Center };
    private readonly PaletteSwatch _dot = new() { WidthRequest = 10, HeightRequest = 10, VerticalOptions = LayoutOptions.Center };
    private readonly Label _text = new()
    {
        FontFamily = Fonts.SemiBold,
        FontSize = 14,
        LineBreakMode = LineBreakMode.NoWrap,
        VerticalTextAlignment = TextAlignment.Center
    };
    private readonly Label _trailing = new()
    {
        FontFamily = Fonts.Icons,
        FontAutoScalingEnabled = false,
        FontSize = 18,
        WidthRequest = 24,
        HorizontalTextAlignment = TextAlignment.Center,
        VerticalTextAlignment = TextAlignment.Center
    };
    private readonly Border _shape = new()
    {
        MinimumHeightRequest = 32,
        VerticalOptions = LayoutOptions.Center,
        StrokeShape = new RoundRectangle { CornerRadius = 8 }
    };

    public Chip()
    {
        MinimumHeightRequest = 48;
        _shape.Content = new HorizontalStackLayout { Spacing = 6, Children = { _tile, _dot, _glyph, _text, _trailing } };
        Content = _shape;
        _glyph.SetThemeColor(Label.TextColorProperty, ThemeColor.Ink);
        _text.SetThemeColor(Label.TextColorProperty, ThemeColor.Ink);
        _trailing.SetThemeColor(Label.TextColorProperty, ThemeColor.Ink);
        // A glyph is a private-use codepoint a screen reader cannot speak, and the tile would add the name of its
        // icon. The chip carries the text.
        foreach (var picture in new View[] { _glyph, _tile, _dot, _trailing })
            AutomationProperties.SetIsInAccessibleTree(picture, false);

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => Tapped?.Invoke(this, EventArgs.Empty);
        GestureRecognizers.Add(tap);
        Apply();
    }

    public event EventHandler? Tapped;

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public string Glyph { get => (string)GetValue(GlyphProperty); set => SetValue(GlyphProperty, value); }
    public string TrailingGlyph { get => (string)GetValue(TrailingGlyphProperty); set => SetValue(TrailingGlyphProperty, value); }
    public string IconKey { get => (string)GetValue(IconKeyProperty); set => SetValue(IconKeyProperty, value); }
    public string ColorKey { get => (string)GetValue(ColorKeyProperty); set => SetValue(ColorKeyProperty, value); }
    public bool IsSelected { get => (bool)GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }
    /// <summary>False for a row of chips where a check would move every chip after the selected one.</summary>
    public bool HasCheck { get => (bool)GetValue(HasCheckProperty); set => SetValue(HasCheckProperty, value); }
    /// <summary>What a screen reader says when the text alone does not say what a tap does. Empty reads the text.</summary>
    public string Description { get => (string)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

    private void Apply()
    {
        var hasTile = IconKey.HasValue();
        var hasDot = !hasTile && ColorKey.HasValue();
        var hasOwnGlyph = !hasTile && !hasDot && Glyph.HasValue();
        var hasTrailing = TrailingGlyph.HasValue();
        _text.Text = Text;
        _tile.IsVisible = hasTile;
        _tile.IconKey = IconKey;
        _tile.ColorKey = ColorKey;
        _dot.IsVisible = hasDot;
        _dot.ColorKey = ColorKey;
        _glyph.Text = hasOwnGlyph ? Glyph : MaterialSymbols.Check;
        _glyph.IsVisible = hasOwnGlyph || IsSelected && HasCheck && !hasTile && !hasDot;
        _trailing.Text = TrailingGlyph;
        _trailing.IsVisible = hasTrailing;
        // The ring is one unit wider than the outline. The padding gives that unit back, so selecting never moves the text.
        _shape.StrokeThickness = IsSelected ? 2 : 1;
        var ring = _shape.StrokeThickness - 1;
        _shape.Padding = new Thickness((hasTile ? TilePadding : SidePadding) - ring, 0,
            (hasTrailing ? TrailingPadding : SidePadding) - ring, 0);
        _shape.SetThemeColor(Border.StrokeProperty, IsSelected ? ThemeColor.Ink : ThemeColor.Outline);
        if (IsSelected)
            _shape.SetThemeColor(BackgroundColorProperty, ThemeColor.Selected);
        else
            _shape.BackgroundColor = Colors.Transparent;

        SemanticProperties.SetDescription(this, Description.HasValue() ? Description : Text);
        SemanticProperties.SetHint(this, IsSelected ? "Selezionato" : "");
    }
}
