using AndroidX.AppCompat.App;
using Centesimo.Application;

namespace Centesimo.App;

public sealed class AndroidStoryBars : IStoryBars
{
    public void Hold(RecapSurface top, RecapSurface bottom)
    {
        if (Activity is { } activity)
            SystemBars.Hold(activity, top, bottom, IsDark);
    }

    public void Release()
    {
        if (Activity is { } activity)
            SystemBars.Release(activity, IsDark);
    }

    private static AppCompatActivity? Activity => Microsoft.Maui.ApplicationModel.Platform.CurrentActivity as AppCompatActivity;

    private static bool IsDark => Microsoft.Maui.Controls.Application.Current?.RequestedTheme == AppTheme.Dark;
}
