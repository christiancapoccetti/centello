namespace Centesimo.Application.Tests;

public sealed class AppEntry_should_expected_behavior
{
    private static readonly Guid CategoryId = Guid.Parse("3f2c9a52-6d1e-4b7a-9c58-0e1d2a3b4c5d");

    public static TheoryData<AppEntry, string> Entries => new()
    {
        { new AppEntry.Screen.Today(), "centesimo://today" },
        { new AppEntry.Screen.NewExpense(null), "centesimo://expense/new" },
        { new AppEntry.Screen.NewExpense(CategoryId), "centesimo://expense/new?category=3f2c9a52-6d1e-4b7a-9c58-0e1d2a3b4c5d" },
        { new AppEntry.Screen.Draft(), "centesimo://expense/draft" },
        { new AppEntry.Screen.Settings(), "centesimo://settings" },
        { new AppEntry.Panel.VoicePanel(), "centesimo://panel/voice" },
        { new AppEntry.Panel.PhotoPanel(), "centesimo://panel/photo" }
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
        { "centesimo://today", new AppEntry.Screen.Today() },
        { "centesimo://expense/new", new AppEntry.Screen.NewExpense(null) },
        { "centesimo://expense/new?category=3f2c9a52-6d1e-4b7a-9c58-0e1d2a3b4c5d", new AppEntry.Screen.NewExpense(CategoryId) },
        { "centesimo://panel/voice", new AppEntry.Panel.VoicePanel() },
        { "centesimo://panel/photo", new AppEntry.Panel.PhotoPanel() }
    };

    [Theory]
    [MemberData(nameof(PinnedLinks))]
    public void Pinned_links_never_change(string link, AppEntry expected) =>
        Assert.Equal(expected, AppEntry.Parse(link));

    [Fact]
    public void Should_ignore_the_voice_link_of_an_earlier_version() =>
        Assert.Null(AppEntry.Parse("centesimo://expense/voice"));

    [Theory]
    [InlineData("centesimo://today", false)]
    [InlineData("centesimo://expense/new", true)]
    [InlineData("centesimo://expense/draft", false)]
    [InlineData("centesimo://settings", false)]
    [InlineData("centesimo://panel/voice", true)]
    [InlineData("centesimo://panel/photo", true)]
    public void Should_tell_which_entries_add_an_expense(string link, bool addsExpense) =>
        Assert.Equal(addsExpense, AppEntry.Parse(link)!.AddsExpense);

    [Fact]
    public void Should_open_a_new_expense_without_category_when_the_category_is_not_a_guid() =>
        Assert.Equal(new AppEntry.Screen.NewExpense(null), AppEntry.Parse("centesimo://expense/new?category=abc"));

    [Fact]
    public void Should_open_a_new_expense_without_category_when_the_category_is_empty() =>
        Assert.Equal(new AppEntry.Screen.NewExpense(null), AppEntry.Parse("centesimo://expense/new?category="));

    [Theory]
    [InlineData("https://today")]
    [InlineData("other://expense/new")]
    [InlineData("centesimo://elsewhere")]
    [InlineData("centesimo://expense")]
    [InlineData("centesimo://expense/unknown")]
    [InlineData("centesimo:today")]
    [InlineData("not a link")]
    [InlineData("")]
    [InlineData(null)]
    public void Should_ignore_a_link_it_does_not_know(string? link) =>
        Assert.Null(AppEntry.Parse(link));
}
