namespace Centesimo.Application;

/// <summary>The only layout maths a recap chart needs, so it lives where it can be tested.</summary>
public static class UnitGrid
{
    /// <summary>The largest square side, at most <paramref name="maxSide"/>, that fits <paramref name="count"/>
    /// squares with <paramref name="gap"/> between them in <paramref name="width"/> x <paramref name="height"/>, and
    /// the column count it gives. Among equal sides the most columns wins, so the squares read like lines of text.
    /// Nothing to draw gives (0, 0).</summary>
    public static (double Side, int Columns) Fit(int count, double width, double height, double gap, double maxSide)
    {
        if (count <= 0)
            return (0, 0);

        var best = (Side: -1.0, Columns: 1);
        for (var columns = 1; columns <= count; columns++)
        {
            var rows = (count + columns - 1) / columns;
            var side = Math.Min(
                maxSide,
                Math.Min((width - gap * (columns - 1)) / columns, (height - gap * (rows - 1)) / rows));
            if (side >= best.Side)
                best = (side, columns);
        }

        return (Math.Max(0, best.Side), best.Columns);
    }
}
