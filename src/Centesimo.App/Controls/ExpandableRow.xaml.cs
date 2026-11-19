using Centesimo.Application;

namespace Centesimo.App.Controls;

/// <summary>A row with a title and a word under it that opens to show a long text, as the license of a component.</summary>
public partial class ExpandableRow : ContentView
{
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(ExpandableRow), "",
        propertyChanged: (row, _, _) => ((ExpandableRow)row).Apply());
    public static readonly BindableProperty SubtitleProperty = BindableProperty.Create(nameof(Subtitle), typeof(string), typeof(ExpandableRow), "",
        propertyChanged: (row, _, _) => ((ExpandableRow)row).Apply());
    public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(ExpandableRow), "");
    public static readonly BindableProperty IsExpandedProperty = BindableProperty.Create(nameof(IsExpanded), typeof(bool), typeof(ExpandableRow), false,
        propertyChanged: (row, _, _) => ((ExpandableRow)row).Apply());

    public ExpandableRow()
    {
        InitializeComponent();
        Apply();
    }

    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string Subtitle { get => (string)GetValue(SubtitleProperty); set => SetValue(SubtitleProperty, value); }
    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public bool IsExpanded { get => (bool)GetValue(IsExpandedProperty); set => SetValue(IsExpandedProperty, value); }

    private void OnToggleClicked(object? sender, EventArgs e) => IsExpanded = !IsExpanded;

    private void Apply()
    {
        Body.IsVisible = IsExpanded;
        Arrow.Text = IsExpanded ? MaterialSymbols.ExpandLess : MaterialSymbols.ExpandMore;
        SemanticProperties.SetDescription(Toggle, $"{Title}, {Subtitle}");
        SemanticProperties.SetHint(Toggle, IsExpanded ? "Nascondi il testo" : "Mostra il testo");
    }
}
