using Centesimo.App.Theming;
using Centesimo.Application;

namespace Centesimo.App.Controls.Recap;

/// <summary>One square per small expense, in its category's ink, largest category first. <see cref="UnitGrid.Fit"/>
/// picks the side and the columns for the room the card gives; the view takes the height those rows need.</summary>
public sealed class RecapUnitsView : ThemedDrawingView
{
    private const float Gap = 3;
    private const float DenseGap = 2;
    private const int DenseCount = 100;

    private readonly RecapUnits _units;
    private readonly StoryInk _ink;
    private readonly float _gap;
    private readonly double _side;
    private readonly int _columns;

    public RecapUnitsView(RecapUnits units, StoryInk ink, double width, double maxHeight, double maxSide)
    {
        _units = units;
        _ink = ink;
        _gap = units.ColorKeys.Count > DenseCount ? DenseGap : Gap;
        (_side, _columns) = UnitGrid.Fit(units.ColorKeys.Count, width, maxHeight, _gap, maxSide);
        var rows = _columns == 0 ? 0 : (units.ColorKeys.Count + _columns - 1) / _columns;
        HeightRequest = rows == 0 ? 0 : rows * _side + (rows - 1) * _gap;
        HorizontalOptions = LayoutOptions.Start;
        WidthRequest = _columns == 0 ? 0 : _columns * _side + (_columns - 1) * _gap;
        SemanticProperties.SetDescription(this, units.Description);
    }

    protected override void Draw(ICanvas canvas, RectF bounds)
    {
        if (_columns == 0)
            return;

        canvas.Antialias = false;
        var side = (float)_side;
        for (var index = 0; index < _units.ColorKeys.Count; index++)
        {
            canvas.FillColor = _ink.Palette(_units.ColorKeys[index]);
            canvas.FillRectangle(index % _columns * (side + _gap), index / _columns * (side + _gap), side, side);
        }
    }
}
