namespace Centesimo.Application.Tests;

public sealed class ItalianFormat_should_expected_behavior
{
    [Theory]
    [InlineData(2026, 11, 19, "19 novembre 2026")]
    [InlineData(2027, 3, 1, "1 marzo 2027")]
    public void Write_a_date_in_full(int year, int month, int day, string expected) =>
        Assert.Equal(expected, ItalianFormat.FullDate(new DateOnly(year, month, day)));

    [Theory]
    [InlineData(2026, 12, 18, "18 dic")]
    [InlineData(2027, 3, 14, "14 mar 2027")]
    [InlineData(2025, 12, 31, "31 dic 2025")]
    public void Add_the_year_to_a_short_date_in_another_year(int year, int month, int day, string expected) =>
        Assert.Equal(expected, ItalianFormat.ShortDate(new DateOnly(year, month, day), new DateOnly(2026, 11, 19)));
}
