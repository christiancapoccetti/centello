namespace Centello.Application.Tests;

public sealed class HomeScreenData_should_expected_behavior
{
    private static readonly HomeScreenData WithoutCategories = new([], null!);
    private static readonly HomeScreenData WithCategories =
        new([new CategoryShortcut(Guid.NewGuid(), "Spesa", "cart", "green")], null!);

    public static TheoryData<AppEntry> AddingEntries => new()
    {
        new AppEntry.Screen.NewExpense(null),
        new AppEntry.Screen.NewExpense(Guid.NewGuid()),
        new AppEntry.Panel.VoicePanel(),
        new AppEntry.Panel.PhotoPanel()
    };

    public static TheoryData<AppEntry> OtherEntries => new()
    {
        new AppEntry.Screen.Today(),
        new AppEntry.Screen.Settings(),
        new AppEntry.Screen.Draft()
    };

    [Theory]
    [MemberData(nameof(AddingEntries))]
    public void Should_open_today_instead_of_an_entry_that_adds_an_expense_before_the_first_category(AppEntry entry) =>
        Assert.Equal(new AppEntry.Screen.Today(), WithoutCategories.Admit(entry));

    [Theory]
    [MemberData(nameof(OtherEntries))]
    public void Should_let_the_other_entries_pass_before_the_first_category(AppEntry entry) =>
        Assert.Equal(entry, WithoutCategories.Admit(entry));

    [Theory]
    [MemberData(nameof(AddingEntries))]
    [MemberData(nameof(OtherEntries))]
    public void Should_let_every_entry_pass_once_a_category_exists(AppEntry entry) =>
        Assert.Equal(entry, WithCategories.Admit(entry));
}
