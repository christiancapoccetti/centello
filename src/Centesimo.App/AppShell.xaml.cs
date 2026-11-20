using Centesimo.App.Pages;
using Centesimo.App.Theming;
using Centesimo.Application;

namespace Centesimo.App;

public partial class AppShell : Shell
{
    public AppShell(TodayPage todayPage, InsightsPage insightsPage, CategoriesPage categoriesPage, SettingsPage settingsPage)
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(ExpenseEditorPage), typeof(ExpenseEditorPage));
        Routing.RegisterRoute(nameof(ExpensesPage), typeof(ExpensesPage));
        Routing.RegisterRoute(nameof(CategoryEditorPage), typeof(CategoryEditorPage));
        Routing.RegisterRoute(nameof(CategoryDetailPage), typeof(CategoryDetailPage));
        Routing.RegisterRoute(nameof(RecurringPaymentsPage), typeof(RecurringPaymentsPage));
        Routing.RegisterRoute(nameof(RecurringPaymentEditorPage), typeof(RecurringPaymentEditorPage));
        Routing.RegisterRoute(nameof(LicensesPage), typeof(LicensesPage));
        Routing.RegisterRoute(nameof(BackupPage), typeof(BackupPage));
        Items.Add(new TabBar
        {
            Items =
            {
                CreateTab("Oggi", "TodayPage", MaterialSymbols.Today, todayPage),
                CreateTab("Insight", "InsightsPage", MaterialSymbols.BarChart, insightsPage),
                CreateTab("Categorie", "CategoriesPage", MaterialSymbols.Category, categoriesPage),
                CreateTab("Impostazioni", "SettingsPage", MaterialSymbols.Settings, settingsPage)
            }
        });
    }

    private static ShellContent CreateTab(string title, string route, string glyph, Page page) => new()
    {
        Title = title,
        Route = route,
        Icon = new FontImageSource { Glyph = glyph, FontFamily = Fonts.Icons, Size = 24 },
        Content = page
    };
}
