using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using AndroidX.Activity;
using AndroidX.AppCompat.App;
using AndroidX.Core.View;
using Centello.Application;
using Centello.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Centello.App;

[Activity(
    Name = "it.christiancapoccetti.centesimo.PanelActivity",
    Theme = "@style/Centello.Panel",
    Exported = PanelActivity.ShellCanStart,
    ExcludeFromRecents = true,
    TaskAffinity = "${applicationId}.panel",
    LaunchMode = LaunchMode.SingleTask,
    ConfigurationChanges = ConfigChanges.UiMode | ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public sealed class PanelActivity : AppCompatActivity
{
#if DEBUG
    internal const bool ShellCanStart = true;
#else
    internal const bool ShellCanStart = false;
#endif
    private const int TickMilliseconds = 100;
    private readonly CancellationTokenSource _ticking = new();
    private IServiceProvider? _services;
    private IPanelCapture? _capture;
    private Dictation? _dictation;
    private PanelSheet? _sheet;
    private AppEntryRouter? _router;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        if (IPlatformApplication.Current?.Services is not { } services)
        {
            Finish();
            return;
        }

        if (!services.GetRequiredService<SqliteDatabaseStartup>().IsReady)
        {
            StartActivity(new Intent(this, typeof(MainActivity))
                .AddFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop));
            Finish();
            return;
        }

        WindowCompat.SetDecorFitsSystemWindows(Window!, false);
        var sheet = new PanelSheet(this, services.GetRequiredService<IFontManager>(), IsDark(Resources!.Configuration!));
        _services = services;
        _sheet = sheet;
        _router = services.GetRequiredService<AppEntryRouter>();
        SetContentView(sheet.Root);
        ApplySystemBars(IsDark(Resources.Configuration!));

        sheet.MicTapped += () => Run(() => _dictation?.Send() ?? Task.CompletedTask);
        sheet.ActionTapped += OnActionTapped;
        sheet.Dismissed += Dismiss;
        OnBackPressedDispatcher.AddCallback(this, new BackToDismiss(Dismiss));

        Start(EntryOf(Intent), restored: savedInstanceState is not null);
        Run(Tick);
    }

    /// <summary>A new tap on a panel entry starts that panel over. singleTask has already cleared what was above it.</summary>
    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        Intent = intent;
        Start(EntryOf(intent), restored: false);
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
        if (_capture?.State.KeepsPanelWhenStopped != true)
            Dismiss();

        base.OnStop();
    }

    protected override void OnDestroy()
    {
        _ticking.Cancel();
        if (_capture is { } capture)
            capture.Changed -= OnChanged;

        base.OnDestroy();
    }

    private static AppEntry.Panel? EntryOf(Intent? intent) => AppEntry.Parse(intent?.DataString) as AppEntry.Panel;

    private static bool IsDark(Configuration configuration) =>
        (configuration.UiMode & UiMode.NightMask) == UiMode.NightYes;

    private void ApplySystemBars(bool isDark)
    {
        if (WindowCompat.GetInsetsController(Window!, Window!.DecorView) is not { } controller)
            return;

        controller.AppearanceLightStatusBars = false;
        controller.AppearanceLightNavigationBars = !isDark;
    }

    private void Start(AppEntry.Panel? entry, bool restored)
    {
        Release();
        if (entry is null || _services is null)
        {
            Finish();
            return;
        }

        IPanelCapture capture = entry switch
        {
            AppEntry.Panel.PhotoPanel => _services.GetRequiredService<PhotoReading>(),
            _ => _services.GetRequiredService<Dictation>()
        };
        _capture = capture;
        _dictation = capture as Dictation;
        capture.Changed += OnChanged;
        _sheet?.Show(PanelFaces.Of(capture.Source, capture.State));
        Run(restored ? capture.Restore : capture.Begin);
    }

    private void Release()
    {
        if (_capture is not { } capture)
            return;

        capture.Changed -= OnChanged;
        Run(capture.Cancel);
    }

    private void OnChanged(object? sender, PanelState state)
    {
        switch (state)
        {
            case PanelState.DraftReady ready:
                _router?.Receive(ready.Draft);
                OpenApp(new AppEntry.Screen.Draft());
                return;
            case PanelState.Closed:
                Finish();
                return;
        }

        if (_capture is { } capture)
            _sheet?.Show(PanelFaces.Of(capture.Source, state));
    }

    private void OnActionTapped(PanelAction action)
    {
        if (action == PanelAction.OpenSettings)
        {
            OpenApp(new AppEntry.Screen.Settings());
            return;
        }

        if (_capture is { } capture)
            Run(capture.Retry);
    }

    private void Dismiss()
    {
        if (_capture is { } capture)
            Run(capture.Cancel);

        Finish();
    }

    private async Task Tick()
    {
        while (!_ticking.IsCancellationRequested)
        {
            await Task.Delay(TickMilliseconds, _ticking.Token);
            if (_dictation is { State: PanelState.Listening } dictation)
                await dictation.Tick();
        }
    }

    private void OpenApp(AppEntry.Screen entry)
    {
        StartActivity(EntryLinks.Intent(this, entry), ActivityOptions.MakeCustomAnimation(this, 0, 0)!.ToBundle());
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
            Android.Util.Log.Error(nameof(PanelActivity), exception.ToString());
            Finish();
        }
    }

    private sealed class BackToDismiss(Action dismiss) : OnBackPressedCallback(true)
    {
        public override void HandleOnBackPressed() => dismiss();
    }
}
