using Centesimo.Application;

namespace Centesimo.App;

public partial class App : Microsoft.Maui.Controls.Application
{
    private readonly Func<AppShell> _appShellFactory;
    private readonly RecurringPaymentAutomation _automation;
    private readonly AppEntryRouter _router;
    private readonly HomeScreen _widgets;

    public App(Func<AppShell> appShellFactory, RecurringPaymentAutomation automation, AppEntryRouter router,
        HomeScreen widgets)
    {
        InitializeComponent();
        // The window pans under the keyboard by default, which pushes the top bar behind the status bar and hides the
        // save button. Resizing keeps both in place and scrolls the field being typed in into view.
        Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.Application.SetWindowSoftInputModeAdjust(
            this, Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.WindowSoftInputModeAdjust.Resize);
        _appShellFactory = appShellFactory;
        _automation = automation;
        _router = router;
        _widgets = widgets;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(_appShellFactory());
        window.Activated += OnWindowActivated;
        return window;
    }

    private void OnWindowActivated(object? sender, EventArgs eventArgs)
    {
        _ = _automation.ProcessDue();
        _ = _widgets.Refresh();
        _ = _router.OpenWaiting();
        // On a cold start the Shell may not be current at the first activation: try once more when the UI is idle.
        if (_router.HasWaiting)
            Dispatcher.Dispatch(() => _ = _router.OpenWaiting());
    }
}
