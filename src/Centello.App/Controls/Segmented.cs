using System.ComponentModel;
using Centello.App.Theming;
using Centello.Application;
using Microsoft.Maui.Controls.Shapes;

namespace Centello.App.Controls;

/// <summary>A segmented button: one choice among a few short labels, the chosen one filled and checked. In XAML the
/// labels are one comma-separated string. Disabled, it shows the choice and takes no taps.</summary>
public sealed class Segmented : Border
{
    public static readonly BindableProperty ItemsProperty = BindableProperty.Create(nameof(Items), typeof(IList<string>),
        typeof(Segmented), propertyChanged: (segmented, _, _) => ((Segmented)segmented).Rebuild());
    public static readonly BindableProperty SelectedIndexProperty = BindableProperty.Create(nameof(SelectedIndex), typeof(int),
        typeof(Segmented), 0, BindingMode.TwoWay, propertyChanged: (segmented, _, _) => ((Segmented)segmented).ApplySelection());

    private readonly Grid _segments = new();
    private readonly List<Segment> _items = [];

    public Segmented()
    {
        HeightRequest = 40;
        StrokeThickness = 1;
        StrokeShape = new RoundRectangle { CornerRadius = 20 };
        this.SetThemeColor(StrokeProperty, ThemeColor.Mute);
        Content = _segments;
    }

    [TypeConverter(typeof(ListStringTypeConverter))]
    public IList<string>? Items { get => (IList<string>?)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }
    public int SelectedIndex { get => (int)GetValue(SelectedIndexProperty); set => SetValue(SelectedIndexProperty, value); }

    private void Rebuild()
    {
        _segments.Clear();
        _segments.ColumnDefinitions.Clear();
        _items.Clear();
        foreach (var text in Items ?? [])
        {
            if (_items.Count > 0)
                AddColumn(Divider(), GridLength.Auto);

            var index = _items.Count;
            var segment = new Segment(text);
            segment.Tapped += (_, _) => Select(index);
            _items.Add(segment);
            AddColumn(segment, GridLength.Star);
        }

        ApplySelection();
    }

    private void AddColumn(View view, GridLength width)
    {
        _segments.ColumnDefinitions.Add(new ColumnDefinition(width));
        _segments.Add(view, _segments.ColumnDefinitions.Count - 1);
    }

    private static BoxView Divider()
    {
        var divider = new BoxView { WidthRequest = 1 };
        divider.SetThemeColor(BoxView.ColorProperty, ThemeColor.Mute);
        return divider;
    }

    private void Select(int index)
    {
        if (IsEnabled)
            SelectedIndex = index;
    }

    private void ApplySelection()
    {
        for (var index = 0; index < _items.Count; index++)
            _items[index].IsSelected = index == SelectedIndex;
    }

    private sealed class Segment : Grid
    {
        private readonly Label _check = new()
        {
            FontFamily = Fonts.Icons,
            FontSize = 18,
            Text = MaterialSymbols.Check,
            VerticalTextAlignment = TextAlignment.Center
        };
        private readonly string _text;
        private bool _isSelected;

        public Segment(string text)
        {
            _text = text;
            var label = new Label
            {
                Text = text,
                FontFamily = Fonts.SemiBold,
                FontSize = 14,
                LineBreakMode = LineBreakMode.NoWrap,
                VerticalTextAlignment = TextAlignment.Center
            };
            label.SetThemeColor(Label.TextColorProperty, ThemeColor.Ink);
            _check.SetThemeColor(Label.TextColorProperty, ThemeColor.Ink);
            // The check is a private-use codepoint a screen reader cannot speak. The segment says it is selected.
            AutomationProperties.SetIsInAccessibleTree(_check, false);
            Children.Add(new HorizontalStackLayout
            {
                Spacing = 6,
                HorizontalOptions = LayoutOptions.Center,
                Children = { _check, label }
            });
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) => Tapped?.Invoke(this, EventArgs.Empty);
            GestureRecognizers.Add(tap);
            Apply();
        }

        public event EventHandler? Tapped;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                Apply();
            }
        }

        private void Apply()
        {
            _check.IsVisible = IsSelected;
            if (IsSelected)
                this.SetThemeColor(BackgroundColorProperty, ThemeColor.Selected);
            else
                BackgroundColor = Colors.Transparent;

            SemanticProperties.SetDescription(this, _text);
            SemanticProperties.SetHint(this, IsSelected ? "Selezionato" : "");
        }
    }
}
