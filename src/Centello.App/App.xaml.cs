using Centello.Application;
using Centello.Infrastructure;

namespace Centello.App;

public partial class App : Microsoft.Maui.Controls.Application
{
    private readonly Func<AppShell> _appShellFactory;
    private readonly RecurringPaymentAutomation _automation;
    private readonly AppEntryRouter _router;
    private readonly HomeScreen _widgets;
    private readonly RecapNoticeAutomation _recapNotices;
    private readonly SqliteDatabaseStartup _databaseStartup;

    public App(Func<AppShell> appShellFactory, RecurringPaymentAutomation automation, AppEntryRouter router,
        HomeScreen widgets, RecapNoticeAutomation recapNotices, SqliteDatabaseStartup databaseStartup)
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
        _recapNotices = recapNotices;
        _databaseStartup = databaseStartup;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window();
        window.Page = _databaseStartup.IsReady ? _appShellFactory() : StartupError(window);
        window.Activated += OnWindowActivated;
        return window;
    }

    private ContentPage StartupError(Window window)
    {
        var retry = new Button { Text = "Riprova" };
        retry.Clicked += async (_, _) =>
        {
            retry.IsEnabled = false;
            await _databaseStartup.Run();
            if (!_databaseStartup.IsReady)
            {
                retry.IsEnabled = true;
                return;
            }

            window.Page = _appShellFactory();
            OnWindowActivated(window, EventArgs.Empty);
        };

        return new ContentPage
        {
            Title = "Centello",
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 16,
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    new Label { Text = "Centello non è riuscito ad aprire i dati del telefono.", FontSize = 20 },
                    retry
                }
            }
        };
    }

    private void OnWindowActivated(object? sender, EventArgs eventArgs)
    {
        if (sender is not Window { Page: AppShell })
            return;

        _ = _automation.ProcessDue();
        _ = _widgets.Refresh();
        _ = _recapNotices.Sync();
        _ = _router.OpenWaiting();
        // On a cold start the Shell may not be current at the first activation: try once more when the UI is idle.
        if (_router.HasWaiting)
            Dispatcher.Dispatch(() => _ = _router.OpenWaiting());
    }
}
