namespace Centesimo.Application;

public sealed record HomeScreenData(IReadOnlyList<CategoryShortcut> Shortcuts, MonthGlance Month)
{
    /// <summary>False until an active category exists: every add action then opens Oggi, where the first run starts.</summary>
    public bool CanAdd => Shortcuts.Count > 0;

    /// <summary>The one first-run gate for every widget action and launcher shortcut: an entry that adds an expense
    /// becomes Oggi until a category exists.</summary>
    public AppEntry Admit(AppEntry entry) => CanAdd || !entry.AddsExpense ? entry : new AppEntry.Screen.Today();
}
