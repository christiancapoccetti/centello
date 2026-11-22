namespace Centello.App.Controls;

internal readonly record struct BrowseScrollOffset(double Inset)
{
    public static BrowseScrollOffset Capture(double viewportTop, double itemTop) =>
        new(Math.Max(0, viewportTop - itemTop));

    public double Restore(double itemTop, double itemHeight, double contentHeight, double viewportHeight)
    {
        var inset = Math.Min(Inset, Math.Max(0, itemHeight - 1));
        return Math.Clamp(itemTop + inset, 0, Math.Max(0, contentHeight - viewportHeight));
    }
}
