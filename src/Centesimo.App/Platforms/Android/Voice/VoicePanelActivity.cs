using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using AndroidX.Activity;
using AndroidX.AppCompat.App;
using AndroidX.Core.View;
using Centesimo.Application;
using Microsoft.Extensions.DependencyInjection;
using Uri = Android.Net.Uri;

namespace Centesimo.App;

[Activity(
    Theme = "@style/Centesimo.VoicePanel",
    Exported = VoicePanelActivity.ShellCanStart,
    ExcludeFromRecents = true,
    TaskAffinity = "it.christiancapoccetti.centesimo.voice",
    LaunchMode = LaunchMode.SingleTask,
    ConfigurationChanges = ConfigChanges.UiMode | ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public sealed class VoicePanelActivity : AppCompatActivity
{
#if DEBUG
    internal const bool ShellCanStart = true;
#else
    internal const bool ShellCanStart = false;
#endif
    private const int TickMilliseconds = 100;
    private readonly CancellationTokenSource _ticking = new();
    private Dictation? _dictation;
    private VoicePanelSheet? _sheet;
    private AppEntryRouter? _router;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        if (IPlatformApplication.Current?.Services is not { } services)
        {
            Finish();
            return;
        }

        WindowCompat.SetDecorFitsSystemWindows(Window!, false);
        var dictation = services.GetRequiredService<Dictation>();
        var sheet = new VoicePanelSheet(this, services.GetRequiredService<IFontManager>(), IsDark(Resources!.Configuration!));
        _dictation = dictation;
        _sheet = sheet;
        _router = services.GetRequiredService<AppEntryRouter>();
        SetContentView(sheet.Root);
        ApplySystemBars(IsDark(Resources.Configuration!));

        dictation.Changed += OnChanged;
        sheet.MicTapped += () => Run(dictation.Send);
        sheet.RetryTapped += () => Run(dictation.Retry);
        sheet.SettingsTapped += () => OpenApp(new AppEntry.Settings());
        sheet.Dismissed += Dismiss;
        OnBackPressedDispatcher.AddCallback(this, new BackToDismiss(Dismiss));

        Run(dictation.Begin);
        Run(Tick);
    }

    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
    {
        Microsoft.Maui.ApplicationModel.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
    }

    public override void OnConfigurationChanged(Configuration newConfig)
    {
        base.OnConfigurationChanged(newConfig);
        _sheet?.Recolor(IsDark(newConfig));
        ApplySystemBars(IsDark(newConfig));
    }

    protected override void OnStop()
    {
        Dismiss();
        base.OnStop();
    }

    protected override void OnDestroy()
    {
        _ticking.Cancel();
        if (_dictation is { } dictation)
            dictation.Changed -= OnChanged;

        base.OnDestroy();
    }

    private static bool IsDark(Configuration configuration) =>
        (configuration.UiMode & UiMode.NightMask) == UiMode.NightYes;

    private void ApplySystemBars(bool isDark)
    {
        if (WindowCompat.GetInsetsController(Window!, Window!.DecorView) is not { } controller)
            return;

        controller.AppearanceLightStatusBars = false;
        controller.AppearanceLightNavigationBars = !isDark;
    }

    private void OnChanged(object? sender, DictationState state)
    {
        _sheet?.Show(state);
        if (state is not DictationState.DraftReady ready)
            return;

        _router?.Receive(ready.Draft);
        OpenApp(new AppEntry.VoiceDraft());
    }

    private void Dismiss()
    {
        if (_dictation is { } dictation)
            Run(dictation.Cancel);

        Finish();
    }

    private async Task Tick()
    {
        while (!_ticking.IsCancellationRequested)
        {
            await Task.Delay(TickMilliseconds, _ticking.Token);
            if (_dictation is { State: DictationState.Listening } dictation)
                await dictation.Tick();
        }
    }

    private void OpenApp(AppEntry entry)
    {
        var intent = new Intent(this, typeof(MainActivity))
            .SetData(Uri.Parse(entry.ToLink()))!
            .SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop)!;
        StartActivity(intent, ActivityOptions.MakeCustomAnimation(this, 0, 0)!.ToBundle());
        Finish();
    }

    private async void Run(Func<Task> work)
    {
        try
        {
            await work();
        }
        catch (System.OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            Android.Util.Log.Error(nameof(VoicePanelActivity), exception.ToString());
            Finish();
        }
    }

    private sealed class BackToDismiss(Action dismiss) : OnBackPressedCallback(true)
    {
        public override void HandleOnBackPressed() => dismiss();
    }
}
