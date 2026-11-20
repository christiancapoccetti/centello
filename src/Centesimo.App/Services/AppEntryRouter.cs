using System.Diagnostics;
using Centesimo.App.Pages;
using Centesimo.Application;

namespace Centesimo.App;

/// <summary>Opens what a tap from outside the app asked for, once the Shell can navigate. MainActivity hands it the
/// entry of each intent and the window's activation opens it, so a cold and a warm start take the same path. One slot:
/// a newer entry replaces one still waiting. It rebuilds the Oggi tab with one route, so an editor never lands on top
/// of another, and it asks first when a page of that tab holds unsaved input.</summary>
public sealed class AppEntryRouter(IConfirmation confirmation, IPendingExpenseSpeechDraft drafts)
{
    private AppEntry? _waiting;

    public bool HasWaiting => _waiting is not null;

    public void Receive(AppEntry entry) => _waiting = entry;

    public async Task OpenWaiting()
    {
        if (_waiting is not { } entry || Shell.Current is not { } shell)
            return;

        _waiting = null;
        var (route, rebuildsToday) = RouteOf(entry);
        if (rebuildsToday && TodayStack(shell).OfType<IHoldsUnsavedInput>().Any(page => page.HasUnsavedInput)
            && !await confirmation.AskToDiscard())
        {
            if (entry is AppEntry.VoiceDraft)
                drafts.Take();

            return;
        }

        await shell.GoToAsync(route);
    }

    private static IReadOnlyList<Page?> TodayStack(Shell shell) =>
        shell.Items
            .SelectMany(item => item.Items)
            .Where(section => section.Items.Any(content => content.Route.EndsWith(nameof(TodayPage))))
            .SelectMany(section => section.Navigation.NavigationStack)
            .ToList();

    private static (string Route, bool RebuildsToday) RouteOf(AppEntry entry) => entry switch
    {
        AppEntry.Today => ($"//{nameof(TodayPage)}", true),
        AppEntry.NewExpense expense =>
            ($"//{nameof(TodayPage)}/{ExpenseEditorPage.Route(categoryId: expense.CategoryId, fromOutside: true)}", true),
        AppEntry.VoiceDraft => ($"//{nameof(TodayPage)}/{ExpenseEditorPage.Route(fromOutside: true)}", true),
        AppEntry.Settings => ($"//{nameof(SettingsPage)}", false),
        _ => throw new UnreachableException()
    };
}
