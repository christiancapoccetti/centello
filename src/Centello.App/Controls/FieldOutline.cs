using Centello.App.Theming;
using Centello.Application;

namespace Centello.App.Controls;

/// <summary>The outline of a labelled field in each of its states. Every field draws the same box, so the states
/// are decided here once.</summary>
internal static class FieldOutline
{
    private const double SidePadding = 16;
    // Half of the label at its usual size: the room the box leaves above itself for the label on its edge.
    private const double LabelOverhang = 8;
    // The air between the lower half of a larger label and the text under it.
    private const double LabelAir = 4;

    public static void Apply(Border box, Label caption, bool isFocused, bool hasError, bool isReadOnly)
    {
        var emphasis = hasError ? ThemeColor.Alert : isFocused ? ThemeColor.Ink : ThemeColor.Mute;
        box.SetThemeColor(Border.StrokeProperty, isReadOnly && !hasError ? ThemeColor.Hairline : emphasis);
        caption.SetThemeColor(Label.TextColorProperty, emphasis);
        // The focused outline is one unit wider. The padding gives that unit back, so focusing never moves the text.
        box.StrokeThickness = isFocused ? 2 : 1;
        var side = SidePadding - box.StrokeThickness + 1;
        box.Padding = new Thickness(side, box.Padding.Top, side, 0);
    }

    /// <summary>With a larger system font the label is taller. The box moves down by half of it, and what is in the box
    /// moves down by what the label takes of it, so the label never covers what is typed.</summary>
    public static void PlaceBox(Border box, Label caption)
    {
        var half = caption.Height / 2;
        box.Margin = new Thickness(0, Math.Max(LabelOverhang, half), 0, 0);
        box.Padding = new Thickness(box.Padding.Left, half > LabelOverhang ? half - LabelOverhang + LabelAir : 0,
            box.Padding.Right, 0);
    }
}
