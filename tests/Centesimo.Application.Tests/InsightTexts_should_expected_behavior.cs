namespace Centesimo.Application.Tests;

public sealed class InsightTexts_should_expected_behavior
{
    [Fact]
    public void Should_measure_the_month_of_today_against_the_month_before()
    {
        var summary = InsightTexts.Summary(Overview(Period.Month(2026, 11), new DateOnly(2026, 11, 19), -0.0612));

        Assert.Equal(new BudgetSummary("Hai speso il ", "6% in meno", " di ottobre."), summary);
    }

    [Fact]
    public void Should_name_a_past_month_and_the_month_before_it()
    {
        var october = InsightTexts.Summary(Overview(Period.Month(2026, 10), new DateOnly(2026, 10, 31), 0.18));
        var april = InsightTexts.Summary(Overview(Period.Month(2026, 4), new DateOnly(2026, 4, 30), 0.18));

        Assert.Equal(new BudgetSummary("A ottobre hai speso il ", "18% in più", " di settembre."), october);
        Assert.Equal(new BudgetSummary("Ad aprile hai speso il ", "18% in più", " di marzo."), april);
    }

    [Fact]
    public void Should_measure_the_year_of_today_against_the_same_part_of_the_year_before()
    {
        var summary = InsightTexts.Summary(Overview(Period.Year(2026), new DateOnly(2026, 11, 19), 0.04));

        Assert.Equal(new BudgetSummary("Da gennaio a oggi hai speso il ", "4% in più", " dello stesso periodo del 2025."), summary);
    }

    [Fact]
    public void Should_measure_a_past_year_against_the_whole_year_before()
    {
        var summary = InsightTexts.Summary(Overview(Period.Year(2025), new DateOnly(2025, 12, 31), -0.5));

        Assert.Equal(new BudgetSummary("Nel 2025 hai speso il ", "50% in meno", " del 2024."), summary);
    }

    [Fact]
    public void Should_say_the_spending_is_the_same_when_the_change_rounds_to_nothing()
    {
        var month = InsightTexts.Summary(Overview(Period.Month(2026, 11), new DateOnly(2026, 11, 19), 0.004));
        var year = InsightTexts.Summary(Overview(Period.Year(2026), new DateOnly(2026, 11, 19), -0.004));
        var pastYear = InsightTexts.Summary(Overview(Period.Year(2025), new DateOnly(2025, 12, 31), 0));

        Assert.Equal(new BudgetSummary("Hai speso ", "quanto", " a ottobre."), month);
        Assert.Equal(new BudgetSummary("Da gennaio a oggi hai speso ", "quanto", " nello stesso periodo del 2025."), year);
        Assert.Equal(new BudgetSummary("Nel 2025 hai speso ", "quanto", " nel 2024."), pastYear);
    }

    [Fact]
    public void Should_say_there_is_nothing_to_compare_with()
    {
        var month = InsightTexts.Summary(Overview(Period.Month(2026, 11), new DateOnly(2026, 11, 19), null));
        var year = InsightTexts.Summary(Overview(Period.Year(2026), new DateOnly(2026, 11, 19), null));

        Assert.Equal(new BudgetSummary("Nessuna spesa da confrontare a ottobre.", "", ""), month);
        Assert.Equal(new BudgetSummary("Nessuna spesa da confrontare nel 2025.", "", ""), year);
    }

    [Theory]
    [InlineData(0.324, "32%")]
    [InlineData(0.325, "33%")]
    [InlineData(1, "100%")]
    [InlineData(0, "0%")]
    public void Should_write_a_share_as_a_whole_percentage(double fraction, string expected) =>
        Assert.Equal(expected, InsightTexts.Share(fraction));

    [Fact]
    public void Should_badge_how_a_category_moved()
    {
        Assert.Equal(new InsightChange("+18%", InsightChangeTone.Higher), InsightTexts.Change(0.18));
        Assert.Equal(new InsightChange("−47%", InsightChangeTone.Lower), InsightTexts.Change(-0.47));
        Assert.Equal(new InsightChange("uguale", InsightChangeTone.Same), InsightTexts.Change(0.004));
        Assert.Equal(new InsightChange("nuovo", InsightChangeTone.New), InsightTexts.Change(null));
    }

    private static InsightsOverview Overview(Period period, DateOnly today, double? change) =>
        new(period, new HomeNavigation(period.Previous(), null, period), period.Start, today, today < period.End, 10_000,
            change.HasValue ? 9_000 : null, change, [], []);
}
