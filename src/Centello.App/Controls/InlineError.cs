using Centello.App.Theming;
using Centello.Application;
using Centello.Domain;

namespace Centello.App.Controls;

/// <summary>The message under the field that caused it. It takes no room while there is nothing to say.</summary>
public sealed class InlineError : Grid
{
    public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string),
        typeof(InlineError), "", propertyChanged: (error, _, text) => ((InlineError)error).Apply((string?)text));

    private readonly Label _message = new() { FontSize = 13, LineHeight = 1.35 };

    public InlineError()
    {
        ColumnDefinitions = [new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star)];
        ColumnSpacing = 4;
        var glyph = new Label
        {
            FontFamily = Fonts.Icons,
            FontSize = 16,
            Text = MaterialSymbols.Error,
            Margin = new Thickness(0, 1, 0, 0),
            VerticalOptions = LayoutOptions.Start
        };
        glyph.SetThemeColor(Label.TextColorProperty, ThemeColor.Alert);
        _message.SetThemeColor(Label.TextColorProperty, ThemeColor.Alert);
        AutomationProperties.SetIsInAccessibleTree(glyph, false);
        this.Add(glyph);
        this.Add(_message, 1);
        Apply("");
    }

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }

    /// <summary>Brings the first error of a form into view. A save that stops on a field out of sight would look
    /// like a button that does nothing.</summary>
    public static async Task ShowFirst(ScrollView form)
    {
        var first = form.GetVisualTreeDescendants().OfType<InlineError>().FirstOrDefault(error => error.IsVisible);
        if (first is null)
            return;

        await form.ScrollToAsync(first, ScrollToPosition.Center, true);
    }

    private void Apply(string? text)
    {
        _message.Text = text ?? "";
        IsVisible = text.HasValue();
    }
}
