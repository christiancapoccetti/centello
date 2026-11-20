namespace Centesimo.App;

/// <summary>Sends the app's task to the back, which lands the person on the home screen they came from.</summary>
public static class BackToLauncher
{
    public static void Go()
    {
#if ANDROID
        Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.MoveTaskToBack(true);
#endif
    }
}
