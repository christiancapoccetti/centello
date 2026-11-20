using Centesimo.App.Theming;
using Centesimo.Domain;

namespace Centesimo.App.Controls;

/// <summary>A line of a settings list. With IsNavigation it leads to another screen: it draws the arrow and raises
/// Tapped. Without it, the line only informs.</summary>
public partial class SettingsRow : ContentView
{
    public static readonly BindableProperty GlyphProperty = BindableProperty.Create(nameof(Glyph), typeof(string), typeof(SettingsRow), "");
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(SettingsRow), "",
        propertyChanged: (row, _, _) => ((SettingsRow)row).Apply());
    public static readonly BindableProperty DetailProperty = BindableProperty.Create(nameof(Detail), typeof(string), typeof(SettingsRow), "",
        propertyChanged: (row, _, _) => ((SettingsRow)row).Apply());
    public static readonly BindableProperty IsNavigationProperty = BindableProperty.Create(nameof(IsNavigation), typeof(bool), typeof(SettingsRow), false,
        propertyChanged: (row, _, _) => ((SettingsRow)row).Apply());
    public static readonly BindableProperty IsPositiveProperty = BindableProperty.Create(nameof(IsPositive), typeof(bool), typeof(SettingsRow), false,
        propertyChanged: (row, _, _) => ((SettingsRow)row).Apply());

    public SettingsRow()
    {
        InitializeComponent();
        Apply();
    }

    public event EventHandler? Tapped;

    public string Glyph { get => (string)GetValue(GlyphProperty); set => SetValue(GlyphProperty, value); }
    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string Detail { get => (string)GetValue(DetailProperty); set => SetValue(DetailProperty, value); }
    public bool IsNavigation { get => (bool)GetValue(IsNavigationProperty); set => SetValue(IsNavigationProperty, value); }
    /// <summary>The line says something is on: its mark is drawn in the positive color.</summary>
    public bool IsPositive { get => (bool)GetValue(IsPositiveProperty); set => SetValue(IsPositiveProperty, value); }

    private void OnTapClicked(object? sender, EventArgs e) => Tapped?.Invoke(this, EventArgs.Empty);

    private void Apply()
    {
        DetailLabel.IsVisible = Detail.HasValue();
        Arrow.IsVisible = IsNavigation;
        Tap.IsVisible = IsNavigation;
        Mark.SetThemeColor(Label.TextColorProperty, IsPositive ? ThemeColor.OnPositiveContainer : ThemeColor.Ink);
        // The line reads as one phrase: the mark and the arrow are decoration.
        SemanticProperties.SetDescription(Tap, Detail.HasValue() ? $"{Title}, {Detail}" : Title);
        SemanticProperties.SetHint(Tap, "Apri");
    }
}
