using Centesimo.App.Theming;
using Centesimo.Application;
using Microsoft.Maui.Controls.Shapes;

namespace Centesimo.App.Controls;

/// <summary>One choice of a picker: its picture, with a ring around it when it is the chosen one. The ring keeps its
/// room while it is not drawn, so choosing never moves the pictures.</summary>
internal sealed class RingCell : Border
{
    /// <summary>What the ring adds on every side of the picture: 3 of gap and 2 of ring.</summary>
    public const double Room = 5;

    private bool _isSelected;

    public RingCell(string key, View picture, string description)
    {
        Key = key;
        StrokeThickness = 2;
        Padding = 3;
        StrokeShape = new Rectangle();
        Content = picture;
        SemanticProperties.SetDescription(this, description);
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => Tapped?.Invoke(this, EventArgs.Empty);
        GestureRecognizers.Add(tap);
        Apply();
    }

    public event EventHandler? Tapped;

    public string Key { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            _isSelected = value;
            Apply();
        }
    }

    /// <summary>Lays the cells out in rows of <paramref name="columns"/>: the first picture of a row on the left
    /// edge of the grid, the last on the right edge and equal gaps between them. The grid is wider than the pictures
    /// by the room of a ring on each side, so it takes a margin smaller by <see cref="Room"/>.</summary>
    public static void Arrange(Grid grid, IReadOnlyList<RingCell> cells, int columns)
    {
        for (var column = 0; column < columns; column++)
        {
            if (column > 0)
                grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        }

        for (var row = 0; row < (cells.Count + columns - 1) / columns; row++)
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        for (var index = 0; index < cells.Count; index++)
            grid.Add(cells[index], index % columns * 2, index / columns);
    }

    private void Apply()
    {
        if (IsSelected)
            this.SetThemeColor(StrokeProperty, ThemeColor.Ink);
        else
            Stroke = Colors.Transparent;

        SemanticProperties.SetHint(this, IsSelected ? "Selezionato" : "");
    }
}
