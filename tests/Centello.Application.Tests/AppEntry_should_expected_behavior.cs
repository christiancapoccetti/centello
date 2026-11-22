namespace Centello.Application.Tests;

public sealed class AppEntry_should_expected_behavior
{
    [Fact]
    public void Open_a_shortcut_pinned_before_the_rename() =>
        Assert.Equal(new AppEntry.Screen.Settings(), AppEntry.Parse("centesimo://settings"));

    private static readonly Guid CategoryId = Guid.Parse("3f2c9a52-6d1e-4b7a-9c58-0e1d2a3b4c5d");

    public static TheoryData<AppEntry, string> Entries => new()
    {
        { new AppEntry.Screen.Today(), "centello://today" },
        { new AppEntry.Screen.NewExpense(null), "centello://expense/new" },
        { new AppEntry.Screen.NewExpense(CategoryId), "centello://expense/new?category=3f2c9a52-6d1e-4b7a-9c58-0e1d2a3b4c5d" },
        { new AppEntry.Screen.Draft(), "centello://expense/draft" },
        { new AppEntry.Screen.Settings(), "centello://settings" },
        { new AppEntry.Screen.Recap(Period.Month(2026, 11)), "centello://recap?year=2026&month=11" },
        { new AppEntry.Screen.Recap(Period.Year(2026)), "centello://recap?year=2026" },
        { new AppEntry.Panel.VoicePanel(), "centello://panel/voice" },
        { new AppEntry.Panel.PhotoPanel(), "centello://panel/photo" }
    };

    [Theory]
    [MemberData(nameof(Entries))]
    public void Should_write_the_link_of_each_entry(AppEntry entry, string link) =>
        Assert.Equal(link, entry.ToLink());

    [Theory]
    [MemberData(nameof(Entries))]
    public void Should_read_back_the_link_of_each_entry(AppEntry entry, string link) =>
        Assert.Equal(entry, AppEntry.Parse(link));

    public static TheoryData<string, AppEntry> PinnedLinks => new()
    {
        { "centello://today", new AppEntry.Screen.Today() },
        { "centello://expense/new", new AppEntry.Screen.NewExpense(null) },
        { "centello://expense/new?category=3f2c9a52-6d1e-4b7a-9c58-0e1d2a3b4c5d", new AppEntry.Screen.NewExpense(CategoryId) },
        { "centello://recap?year=2026&month=11", new AppEntry.Screen.Recap(Period.Month(2026, 11)) },
        { "centello://recap?year=2026", new AppEntry.Screen.Recap(Period.Year(2026)) },
        { "centello://panel/voice", new AppEntry.Panel.VoicePanel() },
        { "centello://panel/photo", new AppEntry.Panel.PhotoPanel() }
    };

    [Theory]
    [MemberData(nameof(PinnedLinks))]
    public void Pinned_links_never_change(string link, AppEntry expected) =>
        Assert.Equal(expected, AppEntry.Parse(link));

    [Fact]
    public void Should_ignore_the_voice_link_of_an_earlier_version() =>
        Assert.Null(AppEntry.Parse("centello://expense/voice"));

    [Theory]
    [InlineData("centello://today", false)]
    [InlineData("centello://expense/new", true)]
    [InlineData("centello://expense/draft", false)]
    [InlineData("centello://settings", false)]
    [InlineData("centello://recap?year=2026&month=11", false)]
    [InlineData("centello://panel/voice", true)]
    [InlineData("centello://panel/photo", true)]
    public void Should_tell_which_entries_add_an_expense(string link, bool addsExpense) =>
        Assert.Equal(addsExpense, AppEntry.Parse(link)!.AddsExpense);

    [Fact]
    public void Should_open_a_new_expense_without_category_when_the_category_is_not_a_guid() =>
        Assert.Equal(new AppEntry.Screen.NewExpense(null), AppEntry.Parse("centello://expense/new?category=abc"));

    [Fact]
    public void Should_open_a_new_expense_without_category_when_the_category_is_empty() =>
        Assert.Equal(new AppEntry.Screen.NewExpense(null), AppEntry.Parse("centello://expense/new?category="));

    [Theory]
    [InlineData("https://today")]
    [InlineData("other://expense/new")]
    [InlineData("centello://elsewhere")]
    [InlineData("centello://expense")]
    [InlineData("centello://expense/unknown")]
    [InlineData("centello:today")]
    [InlineData("not a link")]
    [InlineData("")]
    [InlineData(null)]
    public void Should_ignore_a_link_it_does_not_know(string? link) =>
        Assert.Null(AppEntry.Parse(link));

    [Theory]
    [InlineData("centello://recap")]
    [InlineData("centello://recap?year=abc")]
    [InlineData("centello://recap?year=1999")]
    [InlineData("centello://recap?year=2026&month=13")]
    [InlineData("centello://recap?year=2026&month=0")]
    [InlineData("centello://recap?year=2026&month=x")]
    [InlineData("centello://recap?month=11")]
    public void Should_ignore_a_recap_link_with_a_bad_period(string link) =>
        Assert.Null(AppEntry.Parse(link));
}
