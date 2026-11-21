namespace Centesimo.Application.Tests;

public sealed class AppEntry_should_expected_behavior
{
    private static readonly Guid CategoryId = Guid.Parse("3f2c9a52-6d1e-4b7a-9c58-0e1d2a3b4c5d");

    public static TheoryData<AppEntry, string> Entries => new()
    {
        { new AppEntry.Today(), "centesimo://today" },
        { new AppEntry.NewExpense(null), "centesimo://expense/new" },
        { new AppEntry.NewExpense(CategoryId), "centesimo://expense/new?category=3f2c9a52-6d1e-4b7a-9c58-0e1d2a3b4c5d" },
        { new AppEntry.VoiceDraft(), "centesimo://expense/voice" },
        { new AppEntry.Settings(), "centesimo://settings" }
    };

    [Theory]
    [MemberData(nameof(Entries))]
    public void Should_write_the_link_of_each_entry(AppEntry entry, string link) =>
        Assert.Equal(link, entry.ToLink());

    [Theory]
    [MemberData(nameof(Entries))]
    public void Should_read_back_the_link_of_each_entry(AppEntry entry, string link) =>
        Assert.Equal(entry, AppEntry.Parse(link));

    [Fact]
    public void Should_open_a_new_expense_without_category_when_the_category_is_not_a_guid() =>
        Assert.Equal(new AppEntry.NewExpense(null), AppEntry.Parse("centesimo://expense/new?category=abc"));

    [Fact]
    public void Should_open_a_new_expense_without_category_when_the_category_is_empty() =>
        Assert.Equal(new AppEntry.NewExpense(null), AppEntry.Parse("centesimo://expense/new?category="));

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
