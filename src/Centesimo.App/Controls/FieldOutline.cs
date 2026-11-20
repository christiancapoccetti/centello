using Centesimo.App.Theming;
using Centesimo.Application;

namespace Centesimo.App.Controls;

/// <summary>The outline of a labelled field in each of its states. Every field draws the same box, so the states
/// are decided here once.</summary>
internal static class FieldOutline
{
    private const double SidePadding = 16;

    public static void Apply(Border box, Label caption, bool isFocused, bool hasError, bool isReadOnly)
    {
        var emphasis = hasError ? ThemeColor.Alert : isFocused ? ThemeColor.Ink : ThemeColor.Mute;
        box.SetThemeColor(Border.StrokeProperty, isReadOnly && !hasError ? ThemeColor.Hairline : emphasis);
        caption.SetThemeColor(Label.TextColorProperty, emphasis);
        // The focused outline is one unit wider. The padding gives that unit back, so focusing never moves the text.
        box.StrokeThickness = isFocused ? 2 : 1;
        box.Padding = new Thickness(SidePadding - box.StrokeThickness + 1, 0);
    }
}
