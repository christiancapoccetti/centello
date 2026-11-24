namespace Centello.Application.Tests;

public sealed class InsightTexts_should_expected_behavior
{
    private const char Nbsp = '\u00A0';

    [Fact]
    public void Should_measure_the_month_of_today_against_the_same_days_of_the_month_before()
    {
        var lower = InsightTexts.Summary(Period.Month(2026, 11), true, 128_460, 136_660);
        var higher = InsightTexts.Summary(Period.Month(2026, 11), true, 140_000, 136_660);

        Assert.Equal(new BudgetSummary("Fino a oggi hai speso ", $"82,00{Nbsp}€ in meno", " degli stessi giorni di ottobre."), lower);
        Assert.Equal(new BudgetSummary("Fino a oggi hai speso ", $"33,40{Nbsp}€ in più", " degli stessi giorni di ottobre."), higher);
    }

    [Fact]
    public void Should_name_a_past_month_and_the_month_before_it()
    {
        var october = InsightTexts.Summary(Period.Month(2026, 10), false, 159_000, 150_000);
        var april = InsightTexts.Summary(Period.Month(2026, 4), false, 159_000, 150_000);

        Assert.Equal(new BudgetSummary("A ottobre hai speso ", $"90,00{Nbsp}€ in più", " di settembre."), october);
        Assert.Equal(new BudgetSummary("Ad aprile hai speso ", $"90,00{Nbsp}€ in più", " di marzo."), april);
    }

    [Fact]
    public void Should_measure_the_year_of_today_against_the_same_part_of_the_year_before()
    {
        var summary = InsightTexts.Summary(Period.Year(2026), true, 1_800_000, 1_850_000);

        Assert.Equal(new BudgetSummary("Da gennaio a oggi hai speso ", $"500,00{Nbsp}€ in meno", " dello stesso periodo del 2025."), summary);
    }

    [Fact]
    public void Should_measure_a_past_year_against_the_whole_year_before()
    {
        var summary = InsightTexts.Summary(Period.Year(2025), false, 2_000_000, 4_000_000);

        Assert.Equal(new BudgetSummary("Nel 2025 hai speso ", $"20.000,00{Nbsp}€ in meno", " del 2024."), summary);
    }

    [Fact]
    public void Should_say_the_spending_is_the_same_when_nothing_changed()
    {
        var month = InsightTexts.Summary(Period.Month(2026, 11), true, 136_660, 136_660);
        var pastMonth = InsightTexts.Summary(Period.Month(2026, 10), false, 150_000, 150_000);
        var year = InsightTexts.Summary(Period.Year(2026), true, 1_850_000, 1_850_000);
        var pastYear = InsightTexts.Summary(Period.Year(2025), false, 4_000_000, 4_000_000);

        Assert.Equal(new BudgetSummary("Fino a oggi hai speso ", "quanto", " negli stessi giorni di ottobre."), month);
        Assert.Equal(new BudgetSummary("A ottobre hai speso ", "quanto", " a settembre."), pastMonth);
        Assert.Equal(new BudgetSummary("Da gennaio a oggi hai speso ", "quanto", " nello stesso periodo del 2025."), year);
        Assert.Equal(new BudgetSummary("Nel 2025 hai speso ", "quanto", " nel 2024."), pastYear);
    }

    [Fact]
    public void Should_state_a_difference_of_one_cent()
    {
        var summary = InsightTexts.Summary(Period.Month(2026, 11), true, 136_661, 136_660);

        Assert.Equal($"0,01{Nbsp}€ in più", summary.Emphasis);
    }

    [Fact]
    public void Should_say_there_is_nothing_to_compare_with()
    {
        var month = InsightTexts.Summary(Period.Month(2026, 11), true, 128_460, null);
        var year = InsightTexts.Summary(Period.Year(2026), true, 1_800_000, null);

        var pastMonth = InsightTexts.Summary(Period.Month(2026, 10), false, 128_460, null);
        var pastYear = InsightTexts.Summary(Period.Year(2025), false, 1_800_000, null);

        Assert.Equal(new BudgetSummary("Nessuna spesa da confrontare negli stessi giorni di ottobre.", "", ""), month);
        Assert.Equal(new BudgetSummary("Nessuna spesa da confrontare nello stesso periodo del 2025.", "", ""), year);
        Assert.Equal(new BudgetSummary("Nessuna spesa da confrontare a settembre.", "", ""), pastMonth);
        Assert.Equal(new BudgetSummary("Nessuna spesa da confrontare nel 2024.", "", ""), pastYear);
    }

    [Theory]
    [InlineData(0.655, "65,5%")]
    [InlineData(0.1579, "15,8%")]
    [InlineData(0.0001, "<0,1%")]
    [InlineData(1, "100%")]
    [InlineData(0, "0%")]
    public void Should_write_a_share_with_up_to_one_decimal(double fraction, string expected) =>
        Assert.Equal(expected, InsightTexts.Share(fraction));

    [Fact]
    public void Should_show_exact_signed_euros_and_variations()
    {
        Assert.Equal($"+160,01{Nbsp}€", InsightTexts.Delta(16_001));
        Assert.Equal($"−25{Nbsp}€", InsightTexts.Delta(-2_500));
        Assert.Equal("+40%", InsightTexts.Variation(56_000, 40_000));
        Assert.Equal("−15,6%", InsightTexts.Variation(13_500, 16_000));
        Assert.Equal("−100%", InsightTexts.Variation(0, 6_800));
        Assert.Equal("0%", InsightTexts.Variation(6_800, 6_800));
        Assert.Equal("Nuovo", InsightTexts.Variation(6_800, 0));
        Assert.Equal("—", InsightTexts.Variation(6_800, null));
    }

    [Fact]
    public void Should_show_compact_month_and_year_ranges()
    {
        Assert.Equal("1 novembre", InsightTexts.DateRange(new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 1)));
        Assert.Equal("1–24 novembre", InsightTexts.DateRange(new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 24)));
        Assert.Equal("1 gennaio–24 novembre", InsightTexts.DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 11, 24)));
    }
}
