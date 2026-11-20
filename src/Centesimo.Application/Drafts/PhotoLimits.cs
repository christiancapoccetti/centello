namespace Centesimo.Application;

public static class PhotoLimits
{
    /// <summary>The longest side MediaPicker scales a photo down to: a receipt's total stays legible and the upload
    /// takes seconds on mobile data, well inside the 30 s request timeout.</summary>
    public const int MaximumDimension = 1600;

    public const int JpegQuality = 80;

    /// <summary>A scaled photo stays far below this. A bigger file means the scaling did not run.</summary>
    public const int MaximumBytes = 4 * 1024 * 1024;
}
