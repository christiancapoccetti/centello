namespace Centello.Application.Tests;

public sealed class RecapSpan_should_expected_behavior
{
    private static readonly DateOnly Today = new(2026, 11, 21);

    [Fact]
    public void Should_cover_the_whole_of_a_closed_month()
    {
        var span = RecapSpan.Of(Period.Month(2026, 10), Today)!;

        Assert.Equal(new DateOnly(2026, 10, 31), span.Through);
        Assert.False(span.IsSoFar);
    }

    [Fact]
    public void Should_cover_the_whole_of_a_closed_year()
    {
        var span = RecapSpan.Of(Period.Year(2025), Today)!;

        Assert.Equal(new DateOnly(2025, 12, 31), span.Through);
        Assert.False(span.IsSoFar);
    }

    [Fact]
    public void Should_have_no_span_for_the_year_of_today_in_November() =>
        Assert.Null(RecapSpan.Of(Period.Year(2026), Today));

    [Fact]
    public void Should_have_no_span_for_the_year_of_today_the_day_before_it_opens() =>
        Assert.Null(RecapSpan.Of(Period.Year(2026), new DateOnly(2026, 12, 29)));

    [Fact]
    public void Should_cover_the_year_of_today_up_to_yesterday_from_30_December()
    {
        var span = RecapSpan.Of(Period.Year(2026), new DateOnly(2026, 12, 30))!;

        Assert.Equal(new DateOnly(2026, 12, 29), span.Through);
        Assert.True(span.IsSoFar);
    }

    [Fact]
    public void Should_cover_the_year_of_today_up_to_the_30th_on_31_December() =>
        Assert.Equal(new DateOnly(2026, 12, 30), RecapSpan.Of(Period.Year(2026), new DateOnly(2026, 12, 31))!.Through);

    [Fact]
    public void Should_have_no_span_for_the_month_of_today() =>
        Assert.Null(RecapSpan.Of(Period.Month(2026, 11), Today));

    [Fact]
    public void Should_have_no_span_for_a_month_on_its_last_day() =>
        Assert.Null(RecapSpan.Of(Period.Month(2026, 11), new DateOnly(2026, 11, 30)));

    [Fact]
    public void Should_have_a_span_for_a_month_on_the_day_after_it_closes() =>
        Assert.NotNull(RecapSpan.Of(Period.Month(2026, 11), new DateOnly(2026, 12, 1)));

    [Fact]
    public void Should_have_no_span_for_the_year_of_today_on_1_January() =>
        Assert.Null(RecapSpan.Of(Period.Year(2026), new DateOnly(2026, 1, 1)));

    [Fact]
    public void Should_have_a_closed_span_for_the_year_that_ended_the_day_before() =>
        Assert.False(RecapSpan.Of(Period.Year(2025), new DateOnly(2026, 1, 1))!.IsSoFar);

    [Fact]
    public void Should_have_no_span_for_the_future()
    {
        Assert.Null(RecapSpan.Of(Period.Month(2026, 12), Today));
        Assert.Null(RecapSpan.Of(Period.Year(2027), Today));
    }

    [Fact]
    public void Should_compare_november_with_the_whole_of_a_31_day_october()
    {
        var span = RecapSpan.Of(Period.Month(2026, 11), new DateOnly(2026, 12, 1))!;

        Assert.Equal((new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31)), span.Compared);
    }

    [Fact]
    public void Should_compare_a_closed_year_with_the_whole_year_before() =>
        Assert.Equal(
            (new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31)),
            RecapSpan.Of(Period.Year(2025), Today)!.Compared);

    [Fact]
    public void Should_compare_the_year_so_far_with_the_same_days_a_year_earlier() =>
        Assert.Equal(
            (new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 30)),
            RecapSpan.Of(Period.Year(2026), new DateOnly(2026, 12, 31))!.Compared);
}
