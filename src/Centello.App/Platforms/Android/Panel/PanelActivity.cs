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
    private AiAccess? _access;
    private CancellationTokenSource? _preflight;
    private (AppEntry.Panel? Entry, bool Restored)? _pendingEntry;
    private (DraftSource Source, string SurveyUrl)? _blockedTrial;
    private bool _postResumed;

    protected override async void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        _pendingEntry = (EntryOf(Intent), savedInstanceState is not null);
        if (IPlatformApplication.Current?.Services is not { } services)
        {
            Finish();
            return;
        }

        var ready = await services.GetRequiredService<SqliteDatabaseStartup>().Run();
        if (IsFinishing || IsDestroyed)
            return;

        if (!ready)
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
        _access = services.GetRequiredService<AiAccess>();
        _access.Changed += OnAiAccessChanged;
        SetContentView(sheet.Root);
        ApplySystemBars(IsDark(Resources.Configuration!));

        sheet.MicTapped += () => Run(() => _dictation?.Send() ?? Task.CompletedTask);
        sheet.ActionTapped += OnActionTapped;
        sheet.Dismissed += Dismiss;
        OnBackPressedDispatcher.AddCallback(this, new BackToDismiss(Dismiss));

        StartPendingEntry();
        Run(Tick);
    }

    /// <summary>A new tap on a panel entry starts that panel over. singleTask has already cleared what was above it.</summary>
    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        Intent = intent;
        _pendingEntry = (EntryOf(intent), false);
    }

    protected override void OnPostResume()
    {
        base.OnPostResume();
        _postResumed = true;
        StartPendingEntry();
    }

    protected override void OnPause()
    {
        _postResumed = false;
        base.OnPause();
    }

    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
    {
        Microsoft.Maui.ApplicationModel.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
    }

    public override void OnConfigurationChanged(Configuration newConfig)
    {
        base.OnConfigurationChanged(newConfig);
        _sheet?.Resize(newConfig);
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
        if (_access is { } access)
            access.Changed -= OnAiAccessChanged;

        _ticking.Cancel();
        Release();

        base.OnDestroy();
    }

    private static AppEntry.Panel? EntryOf(Intent? intent) => AppEntry.Parse(intent?.DataString) as AppEntry.Panel;

    private static bool IsDark(Configuration configuration) =>
        (configuration.UiMode & UiMode.NightMask) == UiMode.NightYes;

    private void StartPendingEntry()
    {
        // On foreground entry, the process observer invalidates the previous session before post-resume.
        if (!_postResumed || _sheet is null || _pendingEntry is not { } pending || IsFinishing || IsDestroyed)
            return;

        _pendingEntry = null;
        Run(() => Start(pending.Entry, pending.Restored));
    }

    private void ApplySystemBars(bool isDark)
    {
        if (WindowCompat.GetInsetsController(Window!, Window!.DecorView) is not { } controller)
            return;

        controller.AppearanceLightStatusBars = false;
        controller.AppearanceLightNavigationBars = !isDark;
    }

    private async Task Start(AppEntry.Panel? entry, bool restored)
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
        _sheet?.Show(new PanelFace.Hidden());
        await CheckAndBegin(capture, restored ? capture.Restore : capture.Begin);
    }

    private void Release()
    {
        _blockedTrial = null;
        _preflight?.Cancel();
        _preflight = null;
        if (_capture is not { } capture)
            return;

        _capture = null;
        _dictation = null;
        capture.Changed -= OnChanged;
        Run(capture.Cancel);
    }

    private async Task CheckAndBegin(IPanelCapture capture, Func<Task> begin)
    {
        if (_services is not { } services || _preflight is not null)
            return;

        using var preflight = new CancellationTokenSource();
        _preflight = preflight;
        try
        {
            var access = await services.GetRequiredService<ExpenseDraftService>().CheckCapture(preflight.Token);
            if (preflight.IsCancellationRequested || IsFinishing || !ReferenceEquals(_capture, capture))
                return;

            if (access.IsFailure)
            {
                ShowFace(capture.Source, access.Error == DraftErrors.ConsentRequired
                    ? new PanelState.NeedsConsent()
                    : new PanelState.Failed(access.Error));
                return;
            }

            ShowFace(capture.Source, capture.State);
            await begin();
        }
        finally
        {
            if (ReferenceEquals(_preflight, preflight))
                _preflight = null;
        }
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

        if (sender is IPanelCapture capture)
            ShowFace(capture.Source, state);
    }

    private void ShowFace(DraftSource source, PanelState state)
    {
        var surveyUrl = "";
        _blockedTrial = null;
        if (state is PanelState.Failed failed && failed.Error == DraftErrors.TrialExhausted)
        {
            surveyUrl = _access?.SurveyUrl ?? "";
            _blockedTrial = (source, surveyUrl);
        }

        _sheet?.Show(PanelFaces.Of(source, state, surveyUrl));
    }

    private void OnAiAccessChanged() => MainThread.BeginInvokeOnMainThread(() =>
    {
        if (_blockedTrial is not { } blocked || IsFinishing || IsDestroyed || blocked.SurveyUrl == _access?.SurveyUrl)
            return;

        ShowFace(blocked.Source, new PanelState.Failed(DraftErrors.TrialExhausted));
    });

    private void OnActionTapped(PanelAction action)
    {
        switch (action)
        {
            case PanelAction.OpenSettings:
                OpenApp(new AppEntry.Screen.Settings());
                break;
            case PanelAction.OpenManual:
                OpenApp(new AppEntry.Screen.NewExpense(null));
                break;
            case PanelAction.OpenInvitation:
                OpenApp(new AppEntry.Screen.AiInvite());
                break;
            case PanelAction.OpenSurvey:
                Run(OpenSurvey);
                break;
            case PanelAction.Retry when _capture is { } capture:
                Run(() => CheckAndBegin(capture, capture.Retry));
                break;
        }
    }

    private async Task OpenSurvey()
    {
        var url = _access?.SurveyUrl;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var survey))
            return;

        try
        {
            if (await Browser.Default.OpenAsync(survey, BrowserLaunchMode.External))
                return;
        }
        catch (Exception exception)
        {
            Android.Util.Log.Warn(nameof(PanelActivity), exception.ToString());
        }

        Android.Widget.Toast.MakeText(this, "Non è stato possibile aprire il browser", Android.Widget.ToastLength.Short)?.Show();
    }

    private void Dismiss()
    {
        Release();
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
