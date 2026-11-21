namespace Centesimo.Application.Tests;

public sealed class RecapTexts_should_expected_behavior
{
    private static readonly DateOnly Today = new(2026, 11, 21);

    [Fact]
    public void Should_title_a_month_with_its_name_and_badge_it_with_the_year()
    {
        var span = RecapSpan.Of(Period.Month(2026, 10), Today)!;

        Assert.Equal("Il tuo ottobre", RecapTexts.Title(span));
        Assert.Equal("2026", RecapTexts.Badge(span));
    }

    [Fact]
    public void Should_title_a_closed_year_with_no_badge()
    {
        var span = RecapSpan.Of(Period.Year(2025), Today)!;

        Assert.Equal("Il tuo 2025", RecapTexts.Title(span));
        Assert.Equal("", RecapTexts.Badge(span));
    }

    [Fact]
    public void Should_badge_the_year_so_far_with_finora()
    {
        var span = RecapSpan.Of(Period.Year(2026), Today)!;

        Assert.Equal("Il tuo 2026", RecapTexts.Title(span));
        Assert.Equal("finora", RecapTexts.Badge(span));
    }
}
