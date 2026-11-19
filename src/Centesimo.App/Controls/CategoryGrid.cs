using System.Collections;
using System.Collections.Specialized;
using Centesimo.App.Theming;
using Centesimo.Application;
using Microsoft.Maui.Controls.Shapes;

namespace Centesimo.App.Controls;

/// <summary>The category picker of the editors: four columns of tile and name, the selected one ringed, and a last
/// cell that creates a category. Items expose Name, Icon, Color and IsSelected. A grid that cannot choose ignores taps and
/// has no last cell.</summary>
public sealed class CategoryGrid : Grid
{
    public static readonly BindableProperty ItemsProperty = BindableProperty.Create(nameof(Items), typeof(IEnumerable),
        typeof(CategoryGrid), propertyChanged: (grid, old, current) => ((CategoryGrid)grid).Observe(old, current));
    public static readonly BindableProperty CanChooseProperty = BindableProperty.Create(nameof(CanChoose), typeof(bool),
        typeof(CategoryGrid), true, propertyChanged: (grid, _, _) => ((CategoryGrid)grid).Rebuild());

    private const int Columns = 4;

    public CategoryGrid()
    {
        ColumnSpacing = 8;
        RowSpacing = 8;
        for (var column = 0; column < Columns; column++)
            ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

        Rebuild();
    }

    public event EventHandler<object>? CategoryTapped;
    public event EventHandler? NewTapped;

    public IEnumerable? Items { get => (IEnumerable?)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }
    public bool CanChoose { get => (bool)GetValue(CanChooseProperty); set => SetValue(CanChooseProperty, value); }

    private void Observe(object? old, object? current)
    {
        if (old is INotifyCollectionChanged previous)
            previous.CollectionChanged -= OnItemsChanged;
        if (current is INotifyCollectionChanged next)
            next.CollectionChanged += OnItemsChanged;

        Rebuild();
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e) => Rebuild();

    private void Rebuild()
    {
        Clear();
        RowDefinitions.Clear();
        var cells = (Items?.Cast<object>() ?? []).Select(CategoryCell).ToList();
        if (CanChoose)
            cells.Add(NewCell());

        for (var row = 0; row < (cells.Count + Columns - 1) / Columns; row++)
            RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        for (var index = 0; index < cells.Count; index++)
            this.Add(cells[index], index % Columns, index / Columns);
    }

    private Cell CategoryCell(object item)
    {
        var tile = new CategoryTile { Size = Cell.TileSize, HorizontalOptions = LayoutOptions.Center };
        tile.SetBinding(CategoryTile.IconKeyProperty, "Icon");
        tile.SetBinding(CategoryTile.ColorKeyProperty, "Color");
        // The cell speaks the category name. The tile would add the icon's name to it.
        AutomationProperties.SetIsInAccessibleTree(tile, false);
        var cell = new Cell(tile, canTap: CanChoose) { BindingContext = item };
        cell.SetBinding(Cell.NameProperty, "Name");
        cell.SetBinding(Cell.IsSelectedProperty, "IsSelected");
        cell.Tapped += (_, _) => CategoryTapped?.Invoke(this, item);
        return cell;
    }

    private Cell NewCell()
    {
        var glyph = new Label
        {
            FontFamily = Fonts.Icons,
            FontSize = 21,
            Text = MaterialSymbols.Add,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };
        glyph.SetThemeColor(Label.TextColorProperty, ThemeColor.Mute);
        AutomationProperties.SetIsInAccessibleTree(glyph, false);
        var outline = new Border
        {
            WidthRequest = Cell.TileSize,
            HeightRequest = Cell.TileSize,
            HorizontalOptions = LayoutOptions.Center,
            StrokeThickness = 1.5,
            StrokeDashArray = [2, 2],
            StrokeShape = new Rectangle(),
            Content = glyph
        };
        outline.SetThemeColor(Border.StrokeProperty, ThemeColor.Outline);
        var cell = new Cell(outline, canTap: true) { Name = "Nuova" };
        SemanticProperties.SetDescription(cell, "Nuova categoria");
        cell.Tapped += (_, _) => NewTapped?.Invoke(this, EventArgs.Empty);
        return cell;
    }

    private sealed class Cell : Border
    {
        public const double TileSize = 36;

        public static readonly BindableProperty NameProperty = BindableProperty.Create(nameof(Name), typeof(string),
            typeof(Cell), "", propertyChanged: (cell, _, _) => ((Cell)cell).Apply());
        public static readonly BindableProperty IsSelectedProperty = BindableProperty.Create(nameof(IsSelected), typeof(bool),
            typeof(Cell), false, propertyChanged: (cell, _, _) => ((Cell)cell).Apply());

        private readonly Label _name = new()
        {
            FontFamily = Fonts.SemiBold,
            FontSize = 12,
            LineBreakMode = LineBreakMode.TailTruncation,
            MaxLines = 1,
            HorizontalTextAlignment = TextAlignment.Center
        };

        public Cell(View picture, bool canTap)
        {
            MinimumHeightRequest = 76;
            // The ring takes 2 of the cell on every side, also while it is transparent, so selecting never moves the tile.
            StrokeThickness = 2;
            Padding = new Thickness(1, 7, 1, 5);
            StrokeShape = new RoundRectangle { CornerRadius = 12 };
            _name.SetThemeColor(Label.TextColorProperty, ThemeColor.Ink);
            // 3 and not the 6 of the design: Android puts the other 3 of font padding over the name.
            Content = new VerticalStackLayout { Spacing = 3, Children = { picture, _name } };
            if (canTap)
            {
                var tap = new TapGestureRecognizer();
                tap.Tapped += (_, _) => Tapped?.Invoke(this, EventArgs.Empty);
                GestureRecognizers.Add(tap);
            }

            Apply();
        }

        public event EventHandler? Tapped;

        public string Name { get => (string)GetValue(NameProperty); set => SetValue(NameProperty, value); }
        public bool IsSelected { get => (bool)GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }

        private void Apply()
        {
            _name.Text = Name;
            if (IsSelected)
            {
                this.SetThemeColor(StrokeProperty, ThemeColor.Ink);
                this.SetThemeColor(BackgroundColorProperty, ThemeColor.Selected);
            }
            else
            {
                Stroke = Colors.Transparent;
                BackgroundColor = Colors.Transparent;
            }

            SemanticProperties.SetDescription(this, Name);
            SemanticProperties.SetHint(this, IsSelected ? "Selezionata" : "");
        }
    }
}
