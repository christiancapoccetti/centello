namespace Centello.Application.Tests;

public sealed class LauncherShortcuts_should_expected_behavior
{
    private static readonly Guid TopId = Guid.NewGuid();

    private static readonly HomeScreenData WithoutCategories = new([], null!);

    private static readonly HomeScreenData WithCategories = new(
        [
            new CategoryShortcut(TopId, "Spesa", "cart", "green"),
            new CategoryShortcut(Guid.NewGuid(), "Casa", "home", "blue")
        ],
        null!);

    [Fact]
    public void Should_rank_the_modes_first_then_the_top_category()
    {
        var shortcuts = LauncherShortcuts.Of(WithCategories);

        Assert.Equal(["new", "voice", "photo", $"category-{TopId:N}"], shortcuts.Select(shortcut => shortcut.Id));
    }

    [Fact]
    public void Should_label_the_rows_in_italian()
    {
        var shortcuts = LauncherShortcuts.Of(WithCategories);

        Assert.Equal(["Nuova spesa", "A voce", "Da foto", "Spesa"], shortcuts.Select(shortcut => shortcut.ShortLabel));
        Assert.Equal(
            ["Nuova spesa", "Spesa a voce", "Spesa da foto", "Spesa in Spesa"],
            shortcuts.Select(shortcut => shortcut.LongLabel));
    }

    [Fact]
    public void Should_open_the_matching_entry_with_the_matching_art()
    {
        var shortcuts = LauncherShortcuts.Of(WithCategories);

        Assert.Equal(
            [
                new AppEntry.Screen.NewExpense(null),
                new AppEntry.Panel.VoicePanel(),
                new AppEntry.Panel.PhotoPanel(),
                new AppEntry.Screen.NewExpense(TopId)
            ],
            shortcuts.Select(shortcut => shortcut.Entry));
        Assert.Equal(
            [
                new ShortcutArt.Way(MaterialSymbols.Add),
                new ShortcutArt.Way(MaterialSymbols.Mic),
                new ShortcutArt.Way(MaterialSymbols.PhotoCamera),
                new ShortcutArt.Category("cart", "green")
            ],
            shortcuts.Select(shortcut => shortcut.Art));
    }

    [Fact]
    public void Should_leave_out_the_category_row_and_open_today_before_the_first_category()
    {
        var shortcuts = LauncherShortcuts.Of(WithoutCategories);

        Assert.Equal(["new", "voice", "photo"], shortcuts.Select(shortcut => shortcut.Id));
        Assert.All(shortcuts, shortcut => Assert.Equal(new AppEntry.Screen.Today(), shortcut.Entry));
    }

    [Fact]
    public void Should_keep_the_long_labels_of_the_modes_within_the_launcher_limit()
    {
        var modes = LauncherShortcuts.Of(WithCategories).Take(3);

        Assert.All(modes, shortcut => Assert.InRange(shortcut.LongLabel.Length, 1, 25));
    }

    [Fact]
    public void Should_be_equal_by_value_when_built_from_the_same_data()
    {
        Assert.True(LauncherShortcuts.Of(WithCategories).SequenceEqual(LauncherShortcuts.Of(WithCategories)));
        Assert.False(LauncherShortcuts.Of(WithCategories).SequenceEqual(LauncherShortcuts.Of(WithoutCategories)));
    }
}
