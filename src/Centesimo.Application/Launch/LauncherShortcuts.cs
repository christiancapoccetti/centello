namespace Centesimo.Application;

/// <summary>One row of the launcher's long-press menu. Id is stable: a pinned copy keeps it, and a later publish with
/// the same Id rewrites the copy. A category's Id carries its CategoryId, which the backup carries to a new phone.</summary>
public sealed record LauncherShortcut(string Id, string ShortLabel, string LongLabel, AppEntry Entry, ShortcutArt Art);

/// <summary>How a shortcut's icon is drawn. A way in: graphite with its coral glyph. A category: its palette colour
/// with the glyph in tile ink, as its tile.</summary>
public abstract record ShortcutArt
{
    private ShortcutArt()
    {
    }

    public sealed record Way(string Glyph) : ShortcutArt;

    public sealed record Category(string IconKey, string ColorKey) : ShortcutArt;
}

public static class LauncherShortcuts
{
    /// <summary>Nuova spesa, Spesa a voce, Spesa da foto, then "Spesa in" the category used most, absent without
    /// categories. The order is the rank: the first is the nearest to the icon. Every entry goes through
    /// <see cref="HomeScreenData.Admit"/>.</summary>
    public static IReadOnlyList<LauncherShortcut> Of(HomeScreenData data) =>
    [
        new("new", "Nuova spesa", "Nuova spesa", data.Admit(new AppEntry.Screen.NewExpense(null)),
            new ShortcutArt.Way(MaterialSymbols.Add)),
        new("voice", "A voce", "Spesa a voce", data.Admit(new AppEntry.Panel.VoicePanel()),
            new ShortcutArt.Way(MaterialSymbols.Mic)),
        new("photo", "Da foto", "Spesa da foto", data.Admit(new AppEntry.Panel.PhotoPanel()),
            new ShortcutArt.Way(MaterialSymbols.PhotoCamera)),
        .. data.Shortcuts
            .Take(1)
            .Select(top => new LauncherShortcut($"category-{top.CategoryId:N}", top.Name, $"Spesa in {top.Name}",
                data.Admit(new AppEntry.Screen.NewExpense(top.CategoryId)),
                new ShortcutArt.Category(top.IconKey, top.ColorKey)))
    ];
}
