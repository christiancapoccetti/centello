using Centello.App.Controls;
using Centello.App.Pages;
using Centello.App.Theming;
using Centello.Application;

namespace Centello.App;

public partial class AppShell : Shell
{
    public AppShell(TodayPage todayPage, InsightsPage insightsPage, CategoriesPage categoriesPage, SettingsPage settingsPage)
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(ExpenseEditorPage), typeof(ExpenseEditorPage));
        Routing.RegisterRoute(nameof(ExpensesPage), typeof(ExpensesPage));
        Routing.RegisterRoute(nameof(CategoryEditorPage), typeof(CategoryEditorPage));
        Routing.RegisterRoute(nameof(CategoryDetailPage), typeof(CategoryDetailPage));
        Routing.RegisterRoute(nameof(RecapPage), typeof(RecapPage));
        Routing.RegisterRoute(nameof(RecurringPaymentsPage), typeof(RecurringPaymentsPage));
        Routing.RegisterRoute(nameof(RecurringPaymentEditorPage), typeof(RecurringPaymentEditorPage));
        Routing.RegisterRoute(nameof(LicensesPage), typeof(LicensesPage));
        Routing.RegisterRoute(nameof(BackupPage), typeof(BackupPage));
        Routing.RegisterRoute(nameof(RestorePage), typeof(RestorePage));
        var tabs = new TabBar();
        ContentPage[] pages = [todayPage, insightsPage, categoriesPage, settingsPage];
        ShellContent[] destinations =
        [
            CreateTab("Oggi", nameof(TodayPage), MaterialSymbols.Today, todayPage),
            CreateTab("Insight", nameof(InsightsPage), MaterialSymbols.BarChart, insightsPage),
            CreateTab("Categorie", nameof(CategoriesPage), MaterialSymbols.Category, categoriesPage),
            CreateTab("Impostazioni", nameof(SettingsPage), MaterialSymbols.Settings, settingsPage)
        ];
        for (var index = 0; index < pages.Length; index++)
        {
            var page = pages[index];
            tabs.Items.Add(destinations[index]);
            page.Content = new RootTabLayout(page, destinations, destinations[index], target => tabs.CurrentItem = tabs.Items[target]);
        }

        Items.Add(tabs);
    }

    private static ShellContent CreateTab(string title, string route, string glyph, Page page) => new()
    {
        Title = title,
        Route = route,
        Icon = new FontImageSource { Glyph = glyph, FontFamily = Fonts.Icons, Size = 24 },
        Content = page
    };
}
