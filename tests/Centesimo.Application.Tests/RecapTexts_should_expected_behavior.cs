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

    [Theory]
    [InlineData(1, "1 spesa")]
    [InlineData(96, "96 spese")]
    [InlineData(1143, "1.143 spese")]
    public void Should_count_with_the_italian_plural_and_thousands(int count, string expected) =>
        Assert.Equal(expected, RecapTexts.Count(count, "spesa", "spese"));

    [Theory]
    [InlineData(1, "1 volta")]
    [InlineData(22, "22 volte")]
    [InlineData(1262, "1.262 volte")]
    public void Should_say_how_many_times(int count, string expected) =>
        Assert.Equal(expected, RecapTexts.Times(count));

    [Theory]
    [InlineData(26, 30, "26 giorni su 30")]
    [InlineData(1, 30, "1 giorno su 30")]
    [InlineData(287, 365, "287 giorni su 365")]
    public void Should_say_days_of(int days, int of, string expected) =>
        Assert.Equal(expected, RecapTexts.DaysOf(days, of));

    [Theory]
    [InlineData(1, "")]
    [InlineData(2, "secondo ")]
    [InlineData(3, "terzo ")]
    public void Should_word_the_rank(int rank, string expected) =>
        Assert.Equal(expected, RecapTexts.Ordinal(rank));

    [Fact]
    public void Should_refuse_a_rank_it_does_not_word() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => RecapTexts.Ordinal(4));

    [Theory]
    [InlineData(DayOfWeek.Saturday, "Il sabato")]
    [InlineData(DayOfWeek.Monday, "Il lunedì")]
    [InlineData(DayOfWeek.Sunday, "La domenica")]
    public void Should_put_the_article_before_a_weekday(DayOfWeek day, string expected) =>
        Assert.Equal(expected, RecapTexts.TheWeekday(day));

    [Theory]
    [InlineData(2026, 11, 14, "Sabato 14 novembre")]
    [InlineData(2026, 8, 8, "Sabato 8 agosto")]
    [InlineData(2026, 11, 15, "Domenica 15 novembre")]
    public void Should_write_the_weekday_with_its_date(int year, int month, int day, string expected) =>
        Assert.Equal(expected, RecapTexts.WeekdayLongDate(new DateOnly(year, month, day)));

    [Theory]
    [InlineData(9, "da settembre")]
    [InlineData(4, "da aprile")]
    public void Should_say_since_which_month(int month, string expected) =>
        Assert.Equal(expected, RecapTexts.Since(month));

    [Fact]
    public void Should_keep_the_cents_of_a_month_and_round_a_year_to_euros()
    {
        var month = RecapSpan.Of(Period.Month(2026, 10), Today)!;
        var year = RecapSpan.Of(Period.Year(2025), Today)!;

        Assert.Equal("1.284,60", RecapTexts.Total(month, 128_460));
        Assert.Equal("17.955", RecapTexts.Total(year, 1_795_480));
        Assert.Equal("1.284,60 €", RecapTexts.TotalMoney(month, 128_460));
        Assert.Equal("17.955 €", RecapTexts.TotalMoney(year, 1_795_480));
    }

    [Fact]
    public void Should_name_what_a_month_is_measured_against()
    {
        var span = RecapSpan.Of(Period.Month(2026, 10), Today)!;

        Assert.Equal("di settembre", RecapTexts.Against(span, Period.Month(2026, 9)));
        Assert.Equal("di ottobre 2025", RecapTexts.Against(span, Period.Month(2025, 10)));
    }

    [Fact]
    public void Should_name_what_a_year_is_measured_against()
    {
        var closed = RecapSpan.Of(Period.Year(2025), Today)!;
        var soFar = RecapSpan.Of(Period.Year(2026), Today)!;

        Assert.Equal("del 2024", RecapTexts.Against(closed, Period.Year(2024)));
        Assert.Equal("del 2025 al 20 novembre", RecapTexts.Against(soFar, Period.Year(2025)));
    }

    [Fact]
    public void Should_say_what_a_change_is_measured_against()
    {
        var month = RecapSpan.Of(Period.Month(2026, 10), Today)!;
        var january = RecapSpan.Of(Period.Month(2026, 1), Today)!;
        var closed = RecapSpan.Of(Period.Year(2025), Today)!;
        var soFar = RecapSpan.Of(Period.Year(2026), Today)!;

        Assert.Equal("rispetto a settembre", RecapTexts.Versus(month));
        Assert.Equal("rispetto a dicembre 2025", RecapTexts.Versus(january));
        Assert.Equal("rispetto al 2024", RecapTexts.Versus(closed));
        Assert.Equal("rispetto al 2025 fino al 20 novembre", RecapTexts.Versus(soFar));
    }

    [Theory]
    [InlineData(4, "il 4")]
    [InlineData(8, "l'8")]
    [InlineData(11, "l'11")]
    [InlineData(18, "il 18")]
    public void Should_put_the_article_before_a_day(int day, string expected) =>
        Assert.Equal(expected, RecapTexts.TheDay(day));

    [Theory]
    [InlineData(31, "Il ")]
    [InlineData(18, "Il ")]
    [InlineData(8, "L'")]
    [InlineData(11, "L'")]
    [InlineData(80, "L'")]
    [InlineData(85, "L'")]
    [InlineData(90, "Il ")]
    public void Should_put_the_article_before_a_percentage(int percent, string expected) =>
        Assert.Equal(expected, RecapTexts.ThePercent(percent));

    [Theory]
    [InlineData("", new string[0])]
    [InlineData("a", new[] { "a" })]
    [InlineData("a e b", new[] { "a", "b" })]
    [InlineData("a, b e c", new[] { "a", "b", "c" })]
    public void Should_join_a_list_the_italian_way(string expected, string[] items) =>
        Assert.Equal(expected, RecapTexts.List(items));
}
