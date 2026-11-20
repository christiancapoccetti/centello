using Centesimo.Application;

namespace Centesimo.App.Controls;

/// <summary>The icon of a category: the catalog in six columns, every tile in the color chosen for the category and
/// the chosen icon ringed.</summary>
public sealed class CategoryIconPicker : Grid
{
    public static readonly BindableProperty SelectedKeyProperty = BindableProperty.Create(nameof(SelectedKey), typeof(string),
        typeof(CategoryIconPicker), "", BindingMode.TwoWay, propertyChanged: (picker, _, _) => ((CategoryIconPicker)picker).ApplySelection());
    public static readonly BindableProperty ColorKeyProperty = BindableProperty.Create(nameof(ColorKey), typeof(string),
        typeof(CategoryIconPicker), "", propertyChanged: (picker, _, _) => ((CategoryIconPicker)picker).ApplyColor());

    private const int Columns = 6;
    private const double TileSize = 48;
    // The tiles of two rows are 12 apart. The rings of both rows already take 10 of it.
    private const double RowGap = 12 - 2 * RingCell.Room;
    private readonly List<(RingCell Cell, CategoryTile Tile)> _cells = [];

    public CategoryIconPicker()
    {
        RowSpacing = RowGap;
        foreach (var icon in CategoryIcons.All)
        {
            var tile = new CategoryTile { IconKey = icon.Key, Size = TileSize };
            // The cell speaks the name of the icon. The tile would say it a second time.
            AutomationProperties.SetIsInAccessibleTree(tile, false);
            var cell = new RingCell(icon.Key, tile, icon.Label);
            cell.Tapped += (_, _) => SelectedKey = icon.Key;
            _cells.Add((cell, tile));
        }

        RingCell.Arrange(this, _cells.Select(item => item.Cell).ToList(), Columns);
        ApplySelection();
        ApplyColor();
    }

    public string SelectedKey { get => (string)GetValue(SelectedKeyProperty); set => SetValue(SelectedKeyProperty, value); }
    public string ColorKey { get => (string)GetValue(ColorKeyProperty); set => SetValue(ColorKeyProperty, value); }

    private void ApplySelection()
    {
        foreach (var (cell, _) in _cells)
            cell.IsSelected = cell.Key == SelectedKey;
    }

    private void ApplyColor()
    {
        foreach (var (_, tile) in _cells)
            tile.ColorKey = ColorKey;
    }
}
