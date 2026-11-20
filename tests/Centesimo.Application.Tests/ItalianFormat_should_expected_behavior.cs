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

    [Theory]
    [InlineData(2026, 11, 18, "scade il 18 novembre.")]
    [InlineData(2026, 8, 8, "scade l'8 agosto.")]
    [InlineData(2026, 12, 11, "scade l'11 dicembre.")]
    public void Say_when_a_payment_is_due(int year, int month, int day, string expected) =>
        Assert.Equal(
            $"Una spesa di {ItalianFormat.Money(1250)} {expected}",
            ItalianFormat.PaymentDue(1250, new DateOnly(year, month, day)));

    [Fact]
    public void Name_a_period_without_its_year()
    {
        Assert.Equal("Novembre", ItalianFormat.PeriodName(Period.Month(2026, 11)));
        Assert.Equal("Anno", ItalianFormat.PeriodName(Period.Year(2026)));
    }

    [Theory]
    [InlineData(0, "0")]
    [InlineData(37, "37")]
    [InlineData(1218, "1.218")]
    public void Group_the_thousands_of_a_count(int count, string expected) =>
        Assert.Equal(expected, ItalianFormat.Count(count));
}
