namespace Centello.Application.Tests;

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

    [Fact]
    public void Name_a_period_without_its_year()
    {
        Assert.Equal("Novembre", ItalianFormat.PeriodName(Period.Month(2026, 11)));
        Assert.Equal("Anno", ItalianFormat.PeriodName(Period.Year(2026)));
    }

    [Theory]
    [InlineData(0, "0")]
    [InlineData(49, "0")]
    [InlineData(50, "1")]
    [InlineData(149, "1")]
    [InlineData(150, "2")]
    [InlineData(8_200, "82")]
    [InlineData(136_660, "1.367")]
    public void Round_to_whole_euros(long cents, string expected) =>
        Assert.Equal(expected, ItalianFormat.WholeEuros(cents));

    [Theory]
    [InlineData(0, "0")]
    [InlineData(37, "37")]
    [InlineData(1218, "1.218")]
    public void Group_the_thousands_of_a_count(int count, string expected) =>
        Assert.Equal(expected, ItalianFormat.Count(count));

    [Theory]
    [InlineData(21, 40, "21:40")]
    [InlineData(9, 5, "09:05")]
    [InlineData(0, 0, "00:00")]
    public void Write_a_time_with_two_digits_each(int hour, int minute, string expected) =>
        Assert.Equal(expected, ItalianFormat.Time(new TimeOnly(hour, minute)));

    [Theory]
    [InlineData(DayOfWeek.Saturday, "sabato")]
    [InlineData(DayOfWeek.Sunday, "domenica")]
    [InlineData(DayOfWeek.Monday, "lunedì")]
    [InlineData(DayOfWeek.Friday, "venerdì")]
    public void Name_a_weekday(DayOfWeek day, string expected) =>
        Assert.Equal(expected, ItalianFormat.Weekday(day));

    [Theory]
    [InlineData(DayOfWeek.Saturday, "sab")]
    [InlineData(DayOfWeek.Sunday, "dom")]
    [InlineData(DayOfWeek.Monday, "lun")]
    public void Abbreviate_a_weekday(DayOfWeek day, string expected) =>
        Assert.Equal(expected, ItalianFormat.ShortWeekday(day));

    [Theory]
    [InlineData(20, "al 20 novembre")]
    [InlineData(8, "all'8 novembre")]
    [InlineData(11, "all'11 novembre")]
    [InlineData(1, "al 1 novembre")]
    public void Say_until_which_day(int day, string expected) =>
        Assert.Equal(expected, ItalianFormat.UntilLongDate(new DateOnly(2026, 11, day)));

    [Theory]
    [InlineData(1, "G")]
    [InlineData(11, "N")]
    [InlineData(12, "D")]
    public void Initial_a_month_with_one_letter(int month, string expected) =>
        Assert.Equal(expected, ItalianFormat.MonthInitial(month));

    [Fact]
    public void Write_whole_euros_with_the_euro_sign()
    {
        Assert.Equal("1.367 €", ItalianFormat.WholeMoney(136_660));
        Assert.Equal("385 €", ItalianFormat.WholeMoney(38_500));
    }
}
