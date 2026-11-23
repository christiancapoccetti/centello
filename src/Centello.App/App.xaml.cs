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
        window.Page = _databaseStartup.IsReady ? _appShellFactory() : StartupPage(window);
        window.Activated += OnWindowActivated;
        return window;
    }

    private ContentPage StartupPage(Window window)
    {
        var splash = new Grid
        {
            BackgroundColor = Color.FromArgb("#141516"),
            Children =
            {
                new Image
                {
                    Source = "segno_su_scuro.png",
                    WidthRequest = 128,
                    HeightRequest = 128,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                }
            }
        };
        var retry = new Button { Text = "Riprova" };
        var error = new VerticalStackLayout
        {
            Padding = 24,
            Spacing = 16,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Label { Text = "Centello non è riuscito ad aprire i dati del telefono.", FontSize = 20 },
                retry
            }
        };
        var page = new ContentPage { Title = "Centello", Content = splash };
        page.Loaded += OnLoaded;
        retry.Clicked += async (_, _) => await Open();
        return page;

        async void OnLoaded(object? sender, EventArgs args)
        {
            page.Loaded -= OnLoaded;
            await Open();
        }

        async Task Open()
        {
            if (!ReferenceEquals(window.Page, page))
                return;

            page.Content = splash;
            var ready = await _databaseStartup.Run();
            if (!ReferenceEquals(window.Page, page))
                return;

            if (!ready)
            {
                page.Content = error;
                return;
            }

            window.Page = _appShellFactory();
            OnWindowActivated(window, EventArgs.Empty);
        }
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
