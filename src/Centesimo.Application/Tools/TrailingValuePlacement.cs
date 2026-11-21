namespace Centesimo.Application;

/// <summary>Where the value that closes a row (an amount) goes against the block that describes it.</summary>
public enum ValuePlacement
{
    /// <summary>At the end of the row, on the line of the block.</summary>
    Beside,
    /// <summary>At the end of its own line, under the block, which then takes the whole width.</summary>
    Below
}

public static class TrailingValuePlacement
{
    /// <summary>The value stays beside the block while the block keeps <paramref name="minContentDp"/>. With a larger
    /// system font the value is wider and the block would shrink to a word a line, so the value gives way instead.</summary>
    public static ValuePlacement Of(double widthDp, double valueDp, double spacingDp, double minContentDp) =>
        double.IsInfinity(widthDp) || widthDp - valueDp - spacingDp >= minContentDp
            ? ValuePlacement.Beside
            : ValuePlacement.Below;
}
