using Centesimo.App.Theming;
using Centesimo.Application;
using Microsoft.Maui.Layouts;

namespace Centesimo.App.Controls.Recap;

/// <summary>The month calendar (seven columns, weekday letters above) and the year heatmap (31 columns, a month per
/// row, its letter on the left): one square per day, full when the day has an expense, outlined when it has none. The
/// squares are painted on a canvas and every letter is a real Label. The width is known when the card is built, so the
/// view can size itself before the first layout.</summary>
public sealed class RecapGridView : ContentView
{
    private const int CalendarColumns = 7;
    private const double ColumnLabelsHeight = 18;
    private const double RowLabelWidth = 14;
    private const double RowLabelHeight = 14;

    private readonly double _side;
    private readonly float _gap;
    private readonly double _top;
    private readonly double _left;

    public RecapGridView(RecapGrid grid, StoryInk ink, double width)
    {
        var columns = grid.Columns;
        var isCalendar = columns <= CalendarColumns;
        _gap = isCalendar ? 4 : 2;
        _left = grid.RowLabels.Count > 0 ? RowLabelWidth + _gap : 0;
        _top = grid.ColumnLabels.Count > 0 ? ColumnLabelsHeight : 0;
        _side = Math.Floor(Math.Min(isCalendar ? 40 : 9, (width - _left - _gap * (columns - 1)) / columns));
        var rows = (grid.Cells.Count + columns - 1) / columns;
        var canvas = new GridCanvas(grid, ink, _side, _gap, _left, _top);
        SemanticProperties.SetDescription(canvas, grid.Description);
        var plane = new AbsoluteLayout { Children = { canvas } };
        AbsoluteLayout.SetLayoutBounds(canvas, new Rect(0, 0, 1, 1));
        AbsoluteLayout.SetLayoutFlags(canvas, AbsoluteLayoutFlags.All);
        for (var column = 0; column < grid.ColumnLabels.Count; column++)
            plane.Children.Add(Label(grid.ColumnLabels[column], ink, 12, ColumnX(column), 0, _side));

        for (var row = 0; row < grid.RowLabels.Count; row++)
            plane.Children.Add(Label(grid.RowLabels[row], ink, 11, 0, _top + RowY(row) + _side / 2 - RowLabelHeight / 2,
                RowLabelWidth));

        HeightRequest = _top + rows * _side + (rows - 1) * _gap;
        Content = plane;
    }

    private double ColumnX(int column) => _left + column * (_side + _gap);

    private double RowY(int row) => row * (_side + _gap);

    private static Label Label(string text, StoryInk ink, double size, double x, double y, double width)
    {
        var label = new Label
        {
            Text = text,
            TextColor = ink.Mute,
            FontFamily = Fonts.Regular,
            FontSize = size,
            HorizontalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.NoWrap,
            InputTransparent = true
        };
        AutomationProperties.SetIsInAccessibleTree(label, false);
        AbsoluteLayout.SetLayoutBounds(label, new Rect(x, y, width, RowLabelHeight + 2));
        return label;
    }

    private sealed class GridCanvas(RecapGrid grid, StoryInk ink, double side, float gap, double left, double top)
        : ThemedDrawingView
    {
        protected override void Draw(ICanvas canvas, RectF bounds)
        {
            canvas.Antialias = false;
            var size = (float)side;
            for (var index = 0; index < grid.Cells.Count; index++)
            {
                var cell = grid.Cells[index];
                if (cell == RecapCell.None)
                    continue;

                var x = (float)left + index % grid.Columns * (size + gap);
                var y = (float)top + index / grid.Columns * (size + gap);
                if (cell == RecapCell.Filled)
                {
                    canvas.FillColor = ink.Ink;
                    canvas.FillRectangle(x, y, size, size);
                    continue;
                }

                canvas.StrokeColor = ink.Hairline;
                canvas.StrokeSize = 1;
                canvas.DrawRectangle(x + 0.5f, y + 0.5f, size - 1, size - 1);
            }
        }
    }
}
