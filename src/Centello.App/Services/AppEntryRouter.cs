using System.Diagnostics;
using Centello.App.Pages;
using Centello.App.ViewModels;
using Centello.Application;

namespace Centello.App;

/// <summary>Opens what a tap from outside the app asked for, once the Shell can navigate. MainActivity hands it the
/// entry of each intent and the window's activation opens it, so a cold and a warm start take the same path. One slot:
/// a newer entry replaces one still waiting. It rebuilds the Oggi tab with one route, so an editor never lands on top
/// of another, and it asks first when a page of that tab holds unsaved input.</summary>
public sealed class AppEntryRouter(IConfirmation confirmation, InsightsViewModel insights)
{
    private readonly AppEntryInbox _inbox = new();

    public bool HasWaiting => _inbox.HasWaiting;

    public void Receive(AppEntry.Screen entry) => _inbox.Receive(entry);

    public void Receive(ExpenseDraft draft) => _inbox.Receive(draft);

    public async Task OpenWaiting()
    {
        if (Shell.Current is not { } shell || _inbox.Take() is not { } waiting)
            return;

        var (route, rebuildsToday) = RouteOf(waiting.Entry);
        if (waiting.Entry is AppEntry.Screen.Recap recap)
        {
            // A recap already on the Insight stack would only re-apply its query and keep the old story in front, so
            // pop to the tab root first. Insight then moves to the period, so every exit of the story is a local pop
            // and Back lands on that period.
            if (StackOf<InsightsPage>(shell).OfType<RecapPage>().Any())
                await shell.GoToAsync($"//{nameof(InsightsPage)}");

            await insights.Show(recap.Period);
        }

        if (rebuildsToday && StackOf<TodayPage>(shell).OfType<IHoldsUnsavedInput>().Any(page => page.HasUnsavedInput)
            && !await confirmation.AskToDiscard())
            return;

        // The same absolute route as the one on screen is no navigation at all: leave the editor first so the new one
        // is built and takes its query.
        if (rebuildsToday && StackOf<TodayPage>(shell).Any(page => page is not null))
            await shell.GoToAsync($"//{nameof(TodayPage)}");

        if (waiting.Draft is { } draft)
            await ExpenseEditorPage.OpenDraft(draft, fromOutside: true);
        else
            await shell.GoToAsync(route);
    }

    private static IReadOnlyList<Page?> StackOf<TRoot>(Shell shell) =>
        shell.Items
            .SelectMany(item => item.Items)
            .Where(section => section.Items.Any(content => content.Route.EndsWith(typeof(TRoot).Name)))
            .SelectMany(section => section.Navigation.NavigationStack)
            .ToList();

    private static (string Route, bool RebuildsToday) RouteOf(AppEntry.Screen entry) => entry switch
    {
        AppEntry.Screen.Today => ($"//{nameof(TodayPage)}", true),
        AppEntry.Screen.NewExpense expense =>
            ($"//{nameof(TodayPage)}/{ExpenseEditorPage.Route(categoryId: expense.CategoryId, fromOutside: true)}", true),
        AppEntry.Screen.Draft => ($"//{nameof(TodayPage)}/{ExpenseEditorPage.Route(fromOutside: true)}", true),
        AppEntry.Screen.Settings => ($"//{nameof(SettingsPage)}", false),
        AppEntry.Screen.AiInvite => (AiInvitePage.Route, false),
        AppEntry.Screen.Recap recap => ($"//{nameof(InsightsPage)}/{RecapPage.Route(recap.Period)}", false),
        _ => throw new UnreachableException()
    };
}
