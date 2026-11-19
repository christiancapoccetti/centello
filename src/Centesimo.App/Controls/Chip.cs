using Centesimo.App.Theming;
using Centesimo.Application;
using Centesimo.Domain;
using Microsoft.Maui.Controls.Shapes;

namespace Centesimo.App.Controls;

/// <summary>A choice chip: 32 high inside a 48 touch target. Selected draws the ink ring and a check. A chip with a
/// glyph of its own keeps that glyph instead of the check.</summary>
public sealed class Chip : ContentView
{
    public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string),
        typeof(Chip), "", propertyChanged: (chip, _, _) => ((Chip)chip).Apply());
    public static readonly BindableProperty GlyphProperty = BindableProperty.Create(nameof(Glyph), typeof(string),
        typeof(Chip), "", propertyChanged: (chip, _, _) => ((Chip)chip).Apply());
    public static readonly BindableProperty IsSelectedProperty = BindableProperty.Create(nameof(IsSelected), typeof(bool),
        typeof(Chip), false, propertyChanged: (chip, _, _) => ((Chip)chip).Apply());

    private const double SidePadding = 12;
    private readonly Label _glyph = new()
    {
        FontFamily = Fonts.Icons,
        FontSize = 18,
        Margin = new Thickness(-4, 0, 0, 0),
        VerticalTextAlignment = TextAlignment.Center
    };
    private readonly Label _text = new()
    {
        FontFamily = Fonts.SemiBold,
        FontSize = 14,
        LineBreakMode = LineBreakMode.NoWrap,
        VerticalTextAlignment = TextAlignment.Center
    };
    private readonly Border _shape = new()
    {
        HeightRequest = 32,
        VerticalOptions = LayoutOptions.Center,
        StrokeShape = new RoundRectangle { CornerRadius = 8 }
    };

    public Chip()
    {
        HeightRequest = 48;
        _shape.Content = new HorizontalStackLayout { Spacing = 6, Children = { _glyph, _text } };
        Content = _shape;
        _glyph.SetThemeColor(Label.TextColorProperty, ThemeColor.Ink);
        _text.SetThemeColor(Label.TextColorProperty, ThemeColor.Ink);
        // The glyph is a private-use codepoint a screen reader cannot speak. The chip carries the text.
        AutomationProperties.SetIsInAccessibleTree(_glyph, false);
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => Tapped?.Invoke(this, EventArgs.Empty);
        GestureRecognizers.Add(tap);
        Apply();
    }

    public event EventHandler? Tapped;

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public string Glyph { get => (string)GetValue(GlyphProperty); set => SetValue(GlyphProperty, value); }
    public bool IsSelected { get => (bool)GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }

    private void Apply()
    {
        var hasOwnGlyph = Glyph.HasValue();
        _text.Text = Text;
        _glyph.Text = hasOwnGlyph ? Glyph : MaterialSymbols.Check;
        _glyph.IsVisible = hasOwnGlyph || IsSelected;
        // The ring is one unit wider than the outline. The padding gives that unit back, so selecting never moves the text.
        _shape.StrokeThickness = IsSelected ? 2 : 1;
        _shape.Padding = new Thickness(SidePadding - _shape.StrokeThickness + 1, 0);
        _shape.SetThemeColor(Border.StrokeProperty, IsSelected ? ThemeColor.Ink : ThemeColor.Outline);
        if (IsSelected)
            _shape.SetThemeColor(BackgroundColorProperty, ThemeColor.Selected);
        else
            _shape.BackgroundColor = Colors.Transparent;

        SemanticProperties.SetDescription(this, Text);
        SemanticProperties.SetHint(this, IsSelected ? "Selezionato" : "");
    }
}
