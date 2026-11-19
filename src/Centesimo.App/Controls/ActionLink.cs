using Centesimo.App.Theming;
using Centesimo.Application;

namespace Centesimo.App.Controls;

/// <summary>A text action that leads somewhere: bold words and a chevron. The padding is part of the touch target,
/// 48 high by default, so the place that uses it sets the padding and never a size.</summary>
public sealed class ActionLink : ContentView
{
    public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string),
        typeof(ActionLink), "", propertyChanged: (link, _, text) => ((ActionLink)link).Apply((string)text));

    private readonly Label _text = new()
    {
        FontFamily = Fonts.Bold,
        FontSize = 14,
        LineBreakMode = LineBreakMode.NoWrap,
        VerticalTextAlignment = TextAlignment.Center
    };

    public ActionLink()
    {
        Padding = new Thickness(0, 14);
        var chevron = new Label
        {
            FontFamily = Fonts.Icons,
            FontSize = 18,
            Text = MaterialSymbols.ChevronRight,
            VerticalTextAlignment = TextAlignment.Center
        };
        _text.SetThemeColor(Label.TextColorProperty, ThemeColor.Ink);
        chevron.SetThemeColor(Label.TextColorProperty, ThemeColor.Ink);
        // The chevron is a private-use codepoint a screen reader cannot speak. The link carries the text.
        AutomationProperties.SetIsInAccessibleTree(chevron, false);
        Content = new HorizontalStackLayout { Children = { _text, chevron } };
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => Tapped?.Invoke(this, EventArgs.Empty);
        GestureRecognizers.Add(tap);
    }

    public event EventHandler? Tapped;

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }

    private void Apply(string text)
    {
        _text.Text = text;
        SemanticProperties.SetDescription(this, text);
    }
}
