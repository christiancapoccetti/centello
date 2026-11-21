using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Centesimo.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Centesimo.App;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ScreenOrientation = ScreenOrientation.Portrait, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    private static Microsoft.Maui.Controls.Application? App => Microsoft.Maui.Controls.Application.Current;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        if (savedInstanceState is null)
            Receive(Intent);

        ApplySystemBars();
        if (App is not { } app)
            return;

        app.RequestedThemeChanged += OnRequestedThemeChanged;
        app.ModalPushed += OnModalPushed;
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        Receive(intent);
    }

    protected override void OnDestroy()
    {
        if (App is { } app)
        {
            app.RequestedThemeChanged -= OnRequestedThemeChanged;
            app.ModalPushed -= OnModalPushed;
        }

        base.OnDestroy();
    }

    // A launch from Recents carries the first intent again: its entry was handled then.
    private static void Receive(Intent? intent)
    {
        if (intent is null || intent.Flags.HasFlag(ActivityFlags.LaunchedFromHistory))
            return;

        if (AppEntry.Parse(intent.DataString) is not { } entry || IPlatformApplication.Current is not { } application)
            return;

        application.Services.GetRequiredService<AppEntryRouter>().Receive(entry);
    }

    private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e) => ApplySystemBars();

    // A modal page opens in a window of its own, which starts with the light icons of the Android theme.
    private void OnModalPushed(object? sender, ModalPushedEventArgs e) => ApplySystemBars();

    private void ApplySystemBars() => SystemBars.Apply(this, App?.RequestedTheme == AppTheme.Dark);

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
