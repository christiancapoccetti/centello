namespace Centello.Application.Tests;

public sealed class Period_should_expected_behavior
{
    [Fact]
    public void Should_start_a_month_on_its_first_day_and_end_it_on_its_last()
    {
        var period = Period.Month(2026, 11);

        Assert.Equal(PeriodKind.Month, period.Kind);
        Assert.Equal(new DateOnly(2026, 11, 1), period.Start);
        Assert.Equal(new DateOnly(2026, 11, 30), period.End);
    }

    [Theory]
    [InlineData(2026, 2, 28)]
    [InlineData(2028, 2, 29)]
    [InlineData(2026, 12, 31)]
    public void Should_end_a_month_on_its_last_day(int year, int month, int lastDay) =>
        Assert.Equal(new DateOnly(year, month, lastDay), Period.Month(year, month).End);

    [Fact]
    public void Should_start_a_year_on_January_first_and_end_it_on_December_thirty_first()
    {
        var period = Period.Year(2028);

        Assert.Equal(PeriodKind.Year, period.Kind);
        Assert.Equal(new DateOnly(2028, 1, 1), period.Start);
        Assert.Equal(new DateOnly(2028, 12, 31), period.End);
    }

    [Fact]
    public void Should_normalize_any_day_to_the_month_that_contains_it() =>
        Assert.Equal(Period.Month(2026, 7), Period.MonthOf(new DateOnly(2026, 7, 20)));

    [Fact]
    public void Should_step_months_across_a_year_boundary()
    {
        Assert.Equal(Period.Month(2025, 12), Period.Month(2026, 1).Previous());
        Assert.Equal(Period.Month(2027, 1), Period.Month(2026, 12).Next());
        Assert.Equal(Period.Month(2026, 6), Period.Month(2026, 7).Previous());
        Assert.Equal(Period.Month(2026, 7), Period.Month(2026, 6).Next());
    }

    [Fact]
    public void Should_step_years()
    {
        Assert.Equal(Period.Year(2025), Period.Year(2026).Previous());
        Assert.Equal(Period.Year(2027), Period.Year(2026).Next());
    }

    [Fact]
    public void Should_contain_every_day_between_start_and_end_inclusive()
    {
        var month = Period.Month(2026, 7);

        Assert.True(month.Contains(new DateOnly(2026, 7, 1)));
        Assert.True(month.Contains(new DateOnly(2026, 7, 31)));
        Assert.False(month.Contains(new DateOnly(2026, 6, 30)));
        Assert.False(month.Contains(new DateOnly(2026, 8, 1)));
        Assert.True(Period.Year(2026).Contains(new DateOnly(2026, 12, 31)));
        Assert.False(Period.Year(2026).Contains(new DateOnly(2027, 1, 1)));
    }

    [Fact]
    public void Should_compare_by_value_and_distinguish_a_month_from_a_year()
    {
        Assert.Equal(Period.Month(2026, 1), Period.MonthOf(new DateOnly(2026, 1, 31)));
        Assert.NotEqual<Period>(Period.Month(2026, 1), Period.Year(2026));
    }
}
