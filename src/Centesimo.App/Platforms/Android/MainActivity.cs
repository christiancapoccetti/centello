using Android.App;
using Android.Content.PM;
using Android.OS;
using Centesimo.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Centesimo.App;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        if (Microsoft.Maui.Controls.Application.Current is not { } app)
            return;

        SystemBars.Apply(this, app.RequestedTheme == AppTheme.Dark);
        app.RequestedThemeChanged += OnRequestedThemeChanged;
    }

    protected override void OnDestroy()
    {
        if (Microsoft.Maui.Controls.Application.Current is { } app)
            app.RequestedThemeChanged -= OnRequestedThemeChanged;

        base.OnDestroy();
    }

    private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e) =>
        SystemBars.Apply(this, e.RequestedTheme == AppTheme.Dark);

    protected override void OnStop()
    {
        CancelRecording();
        base.OnStop();
    }

    private void CancelRecording() => _ = CancelRecordingSafely();

    private static async Task CancelRecordingSafely()
    {
        try
        {
            var platformApplication = IPlatformApplication.Current;
            if (platformApplication is null)
                return;

            var recorder = platformApplication.Services.GetRequiredService<ISpeechRecorder>();
            await recorder.Cancel();
        }
        catch (Exception)
        {
        }
    }
}
