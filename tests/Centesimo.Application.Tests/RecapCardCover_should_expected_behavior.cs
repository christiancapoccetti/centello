using Centesimo.Domain;

namespace Centesimo.Application.Tests;

public sealed class RecapCardCover_should_expected_behavior
{
    private static async Task<RecapCard.Cover> MonthCover()
    {
        var world = new RecapWorld(new DateOnly(2026, 12, 1));
        var spesa = world.Category("Spesa", "mint");
        world.SpendOnDays(spesa, 1_000, Enumerable.Range(1, 12).Select(day => new DateOnly(2026, 11, day)).ToArray());
        world.Spend(spesa, 4_000, new DateOnly(2026, 11, 14));
        world.Spend(spesa, 38_000, new DateOnly(2026, 11, 1), automatic: true);
        return (await world.Deck(Period.Month(2026, 11))).Single<RecapCard.Cover>();
    }

    [Fact]
    public async Task Should_title_a_month_on_two_lines_and_invite_to_tap()
    {
        var cover = await MonthCover();

        Assert.Equal("Il tuo\nnovembre", cover.Title);
        Assert.Equal("Le spese del mese, in poche schede.", cover.Subtitle);
        Assert.Equal("Tocca per andare avanti", cover.Hint);
    }

    [Fact]
    public async Task Should_draw_one_bar_per_day_of_the_month_with_a_stub_for_a_day_without_spending()
    {
        var cover = await MonthCover();

        Assert.Equal(30, cover.Chart.Bars.Count);
        Assert.Equal(RecapBarKind.Stub, cover.Chart.Bars[19].Kind);
        Assert.Equal(0, cover.Chart.Bars[19].Height);
        Assert.Equal(RecapBarKind.Bar, cover.Chart.Bars[2].Kind);
        Assert.Empty(cover.Chart.Previous);
    }

    [Fact]
    public async Task Should_scale_the_days_to_the_dearest_variable_day_and_leave_the_rent_out()
    {
        var cover = await MonthCover();

        Assert.Equal(1.0, cover.Chart.Bars[13].Height);
        Assert.Equal(0.25, cover.Chart.Bars[0].Height, 10);
    }

    [Fact]
    public async Task Should_describe_the_peak_day_for_a_screen_reader()
    {
        var cover = await MonthCover();

        Assert.Equal(
            "Spesa giorno per giorno a novembre: picco di 40,00 € sabato 14",
            cover.Chart.Description);
    }

    [Fact]
    public async Task Should_describe_a_month_of_automatic_spending_only_without_a_peak()
    {
        var world = new RecapWorld(new DateOnly(2026, 12, 1));
        var casa = world.Category("Casa", "peri");
        for (var day = 1; day <= 12; day++)
            world.Spend(casa, 1_000, new DateOnly(2026, 11, day), automatic: true);

        var cover = (await world.Deck(Period.Month(2026, 11))).Single<RecapCard.Cover>();

        Assert.All(cover.Chart.Bars, bar => Assert.Equal(RecapBarKind.Stub, bar.Kind));
        Assert.Equal("Spesa giorno per giorno a novembre.", cover.Chart.Description);
    }

    private static async Task<RecapCard.Cover> YearCover(DateOnly today, int year = 2026, int lastMonth = 12)
    {
        var world = new RecapWorld(today);
        var spesa = world.Category("Spesa", "mint");
        for (var month = 1; month <= lastMonth; month++)
            world.SpendInMonth(spesa, year, month, month == 8 ? 194_800 : month == 2 ? 119_800 : 140_000, 5);

        return (await world.Deck(Period.Year(year))).Single<RecapCard.Cover>();
    }

    [Fact]
    public async Task Should_title_a_year_and_draw_its_twelve_months()
    {
        var cover = await YearCover(new DateOnly(2027, 1, 5));

        Assert.Equal("Il tuo\n2026", cover.Title);
        Assert.Equal("Un anno di spese, in poche schede.", cover.Subtitle);
        Assert.Equal("GFMAMGLASOND", string.Concat(cover.Chart.Bars.Select(bar => bar.Label)));
    }

    [Fact]
    public async Task Should_mark_the_dearest_month_strong_and_the_lightest_outlined_with_their_euros()
    {
        var cover = await YearCover(new DateOnly(2027, 1, 5));

        var august = cover.Chart.Bars[7];
        var february = cover.Chart.Bars[1];
        Assert.Equal((RecapBarKind.Strong, 1.0, "1.948"), (august.Kind, august.Height, august.Value));
        Assert.Equal((RecapBarKind.Outlined, "1.198"), (february.Kind, february.Value));
        Assert.Equal(10, cover.Chart.Bars.Count(bar => bar.Kind == RecapBarKind.Bar));
        Assert.Equal(
            $"Spesa per mese nel 2026: agosto il più caro con 1.948{" "}€, febbraio il più leggero con 1.198{" "}€",
            cover.Chart.Description);
    }

    [Fact]
    public async Task Should_say_until_when_the_year_so_far_is_counted_and_stub_the_months_to_come()
    {
        var cover = await YearCover(new DateOnly(2026, 11, 21), lastMonth: 11);

        Assert.Equal("Fino al 20 novembre, in poche schede.", cover.Subtitle);
        Assert.Equal(RecapBarKind.Stub, cover.Chart.Bars[11].Kind);
    }

    [Fact]
    public async Task Should_not_rank_the_month_still_in_progress_in_a_year_so_far()
    {
        var world = new RecapWorld(new DateOnly(2026, 11, 21));
        var spesa = world.Category("Spesa", "mint");
        for (var month = 1; month <= 10; month++)
            world.SpendInMonth(spesa, 2026, month, 100_000, 5);

        world.SpendInMonth(spesa, 2026, 11, 10_000, 5);

        var cover = (await world.Deck(Period.Year(2026))).Single<RecapCard.Cover>();

        Assert.Equal(RecapBarKind.Bar, cover.Chart.Bars[10].Kind);
        Assert.Equal(0.1, cover.Chart.Bars[10].Height, 10);
        Assert.Equal(RecapBarKind.Strong, cover.Chart.Bars[0].Kind);
    }
}
