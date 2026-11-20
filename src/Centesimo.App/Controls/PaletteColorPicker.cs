using Centesimo.App.Theming;
using Centesimo.Application;

namespace Centesimo.App.Controls;

/// <summary>The color of a category: one swatch per palette slot in a row, the chosen one ringed and checked.</summary>
public sealed class PaletteColorPicker : Grid
{
    public static readonly BindableProperty SelectedKeyProperty = BindableProperty.Create(nameof(SelectedKey), typeof(string),
        typeof(PaletteColorPicker), "", BindingMode.TwoWay, propertyChanged: (picker, _, _) => ((PaletteColorPicker)picker).ApplySelection());

    private const double SwatchSize = 40;
    private readonly List<(RingCell Cell, Label Check)> _cells = [];

    public PaletteColorPicker()
    {
        foreach (var color in CategoryPalette.All)
        {
            var check = new Label
            {
                FontFamily = Fonts.Icons,
                FontSize = 22,
                Text = MaterialSymbols.Check,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };
            check.SetThemeColor(Label.TextColorProperty, ThemeColor.TileInk);
            var swatch = new PaletteSwatch { ColorKey = color.Key, WidthRequest = SwatchSize, HeightRequest = SwatchSize };
            // The cell speaks the name of the color. The swatch would say it a second time.
            AutomationProperties.SetIsInAccessibleTree(swatch, false);
            AutomationProperties.SetIsInAccessibleTree(check, false);
            var cell = new RingCell(color.Key, new Grid { Children = { swatch, check } }, color.Name);
            cell.Tapped += (_, _) => SelectedKey = color.Key;
            _cells.Add((cell, check));
        }

        RingCell.Arrange(this, _cells.Select(item => item.Cell).ToList(), _cells.Count);
        ApplySelection();
    }

    public string SelectedKey { get => (string)GetValue(SelectedKeyProperty); set => SetValue(SelectedKeyProperty, value); }

    private void ApplySelection()
    {
        foreach (var (cell, check) in _cells)
        {
            cell.IsSelected = cell.Key == SelectedKey;
            check.IsVisible = cell.IsSelected;
        }
    }
}
