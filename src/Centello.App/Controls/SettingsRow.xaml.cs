using Centello.App.Theming;
using Centello.Application;
using Centello.Domain;

namespace Centello.App.Controls;

/// <summary>A line of a settings list. An Action raises Tapped; a Navigation leads to another screen: it raises Tapped
/// and draws the arrow; a Toggle draws a switch and raises Toggled with the state it asks for. An Info line only informs.</summary>
public partial class SettingsRow : ContentView
{
    public static readonly BindableProperty GlyphProperty = BindableProperty.Create(nameof(Glyph), typeof(string), typeof(SettingsRow), "");
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(SettingsRow), "",
        propertyChanged: (row, _, _) => ((SettingsRow)row).Apply());
    public static readonly BindableProperty DetailProperty = BindableProperty.Create(nameof(Detail), typeof(string), typeof(SettingsRow), "",
        propertyChanged: (row, _, _) => ((SettingsRow)row).Apply());
    public static readonly BindableProperty KindProperty = BindableProperty.Create(nameof(Kind), typeof(SettingsRowKind), typeof(SettingsRow), SettingsRowKind.Info,
        propertyChanged: (row, _, _) => ((SettingsRow)row).Apply());
    public static readonly BindableProperty IsPositiveProperty = BindableProperty.Create(nameof(IsPositive), typeof(bool), typeof(SettingsRow), false,
        propertyChanged: (row, _, _) => ((SettingsRow)row).Apply());

    public static readonly BindableProperty IsOnProperty = BindableProperty.Create(nameof(IsOn), typeof(bool), typeof(SettingsRow), false,
        propertyChanged: (row, _, _) => ((SettingsRow)row).Apply());

    public SettingsRow()
    {
        InitializeComponent();
        Apply();
    }

    public event EventHandler? Tapped;

    /// <summary>A Toggle line asks for the opposite of <see cref="IsOn"/>. The switch only follows IsOn, so whoever owns
    /// the state decides when it flips, for instance after a permission dialog.</summary>
    public event EventHandler<ToggledEventArgs>? Toggled;

    public string Glyph { get => (string)GetValue(GlyphProperty); set => SetValue(GlyphProperty, value); }
    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string Detail { get => (string)GetValue(DetailProperty); set => SetValue(DetailProperty, value); }
    public SettingsRowKind Kind { get => (SettingsRowKind)GetValue(KindProperty); set => SetValue(KindProperty, value); }
    /// <summary>The line says something is on: its mark is drawn in the positive color.</summary>
    public bool IsPositive { get => (bool)GetValue(IsPositiveProperty); set => SetValue(IsPositiveProperty, value); }
    public bool IsOn { get => (bool)GetValue(IsOnProperty); set => SetValue(IsOnProperty, value); }

    private void OnTapClicked(object? sender, EventArgs e)
    {
        if (Kind == SettingsRowKind.Toggle)
            Toggled?.Invoke(this, new ToggledEventArgs(!IsOn));
        else
            Tapped?.Invoke(this, EventArgs.Empty);
    }

    private void Apply()
    {
        DetailLabel.IsVisible = Detail.HasValue();
        Arrow.IsVisible = Kind is SettingsRowKind.Navigation or SettingsRowKind.ExternalLink;
        Arrow.Text = Kind == SettingsRowKind.ExternalLink ? MaterialSymbols.OpenInNew : MaterialSymbols.ChevronRight;
        Switcher.IsVisible = Kind == SettingsRowKind.Toggle;
        Switcher.IsToggled = IsOn;
        Tap.IsVisible = Kind != SettingsRowKind.Info;
        Mark.SetThemeColor(Label.TextColorProperty, IsPositive ? ThemeColor.OnPositiveContainer : ThemeColor.Ink);
        // The line reads as one phrase: the mark, the arrow and the switch are decoration.
        SemanticProperties.SetDescription(Tap, string.Join(", ", new[] { Title, StateWord(), Detail }
            .Where(part => part.HasValue())
            .Distinct()));
        SemanticProperties.SetHint(Tap, Kind switch
        {
            SettingsRowKind.Navigation => "Apri",
            SettingsRowKind.ExternalLink => "Si apre nel browser",
            _ => ""
        });
    }

    private string StateWord() => Kind != SettingsRowKind.Toggle ? "" : IsOn ? "Attivo" : "Spento";
}
