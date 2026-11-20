using Centesimo.Application;

namespace Centesimo.App.Controls;

/// <summary>The color of a category: the chromatic palette slots in one row across the width of the picker, the chosen
/// one ringed. Grey is never offered. A key that is not one of the slots leaves every swatch unringed.</summary>
public sealed class CategoryColorPicker : Grid
{
    public static readonly BindableProperty SelectedKeyProperty = BindableProperty.Create(nameof(SelectedKey), typeof(string),
        typeof(CategoryColorPicker), "", BindingMode.TwoWay, propertyChanged: (picker, _, _) => ((CategoryColorPicker)picker).ApplySelection());

    private const double SwatchSize = 40;
    private readonly List<RingCell> _cells = [];

    public CategoryColorPicker()
    {
        foreach (var color in CategoryPalette.Chromatic)
        {
            var swatch = new PaletteSwatch { ColorKey = color.Key, WidthRequest = SwatchSize, HeightRequest = SwatchSize };
            // The cell speaks the name of the color. The swatch would say it a second time.
            AutomationProperties.SetIsInAccessibleTree(swatch, false);
            var cell = new RingCell(color.Key, swatch, color.Name);
            cell.Tapped += (_, _) => SelectedKey = color.Key;
            _cells.Add(cell);
        }

        RingCell.Arrange(this, _cells, _cells.Count);
        ApplySelection();
    }

    public string SelectedKey { get => (string)GetValue(SelectedKeyProperty); set => SetValue(SelectedKeyProperty, value); }

    private void ApplySelection()
    {
        foreach (var cell in _cells)
            cell.IsSelected = cell.Key == SelectedKey;
    }
}
