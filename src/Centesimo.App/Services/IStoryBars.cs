using Centesimo.Application;

namespace Centesimo.App;

/// <summary>What the status and navigation bars sit on while a recap card is open: dark icons on any palette surface,
/// and below Android 15 the bar takes the surface's color. Release gives them back to the theme.</summary>
public interface IStoryBars
{
    void Hold(RecapSurface top, RecapSurface bottom);

    void Release();
}

/// <summary>For platforms without system bars to color.</summary>
public sealed class NoOpStoryBars : IStoryBars
{
    public void Hold(RecapSurface top, RecapSurface bottom)
    {
    }

    public void Release()
    {
    }
}
