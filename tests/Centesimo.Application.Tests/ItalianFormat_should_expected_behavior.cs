namespace Centesimo.Application.Tests;

public sealed class ItalianFormat_should_expected_behavior
{
    [Theory]
    [InlineData(2026, 11, 19, "19 novembre 2026")]
    [InlineData(2027, 3, 1, "1 marzo 2027")]
    public void Write_a_date_in_full(int year, int month, int day, string expected) =>
        Assert.Equal(expected, ItalianFormat.FullDate(new DateOnly(year, month, day)));
}
