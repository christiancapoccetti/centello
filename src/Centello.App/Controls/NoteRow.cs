using Centello.App.Theming;
using Centello.Application;

namespace Centello.App.Controls;

/// <summary>A note under a settings row: a small glyph and a sentence that wraps inside what is left of the width.
/// A stack would measure the sentence with no limit, so it would run off the edge of a narrow phone.</summary>
public sealed class NoteRow : Grid
{
    public static readonly BindableProperty GlyphProperty = BindableProperty.Create(nameof(Glyph), typeof(string),
        typeof(NoteRow), "", propertyChanged: (note, _, glyph) => ((NoteRow)note)._glyph.Text = (string)glyph);
    public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string),
        typeof(NoteRow), "", propertyChanged: (note, _, text) => ((NoteRow)note)._text.Text = (string)text);

    private readonly Label _glyph = new()
    {
        FontFamily = Fonts.Icons,
        FontSize = 18,
        FontAutoScalingEnabled = false,
        VerticalOptions = LayoutOptions.Start,
        HorizontalTextAlignment = TextAlignment.Center
    };
    private readonly Label _text = new() { FontFamily = Fonts.Regular, FontSize = 13, LineHeight = 1.45 };

    public NoteRow()
    {
        ColumnDefinitions = [new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star)];
        ColumnSpacing = 8;
        _glyph.SetThemeColor(Label.TextColorProperty, ThemeColor.Mute);
        _text.SetThemeColor(Label.TextColorProperty, ThemeColor.Mute);
        AutomationProperties.SetIsInAccessibleTree(_glyph, false);
        this.Add(_glyph);
        this.Add(_text, 1);
    }

    public string Glyph { get => (string)GetValue(GlyphProperty); set => SetValue(GlyphProperty, value); }

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
}
