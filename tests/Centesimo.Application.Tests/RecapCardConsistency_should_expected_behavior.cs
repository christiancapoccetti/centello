using Centesimo.Domain;

namespace Centesimo.Application.Tests;

public sealed class RecapCardConsistency_should_expected_behavior
{
    private static readonly int[] EmptyNovemberDays = [2, 9, 23, 30];

    private static async Task<RecapCard.Consistency> NovemberWithout(params int[] empty)
    {
        var world = new RecapWorld(new DateOnly(2026, 12, 1));
        var spesa = world.Category("Spesa", "mint");
        for (var day = 1; day <= 30; day++)
            if (!empty.Contains(day))
                world.Spend(spesa, 1_000, new DateOnly(2026, 11, day));

        return (await world.Deck(Period.Month(2026, 11))).Single<RecapCard.Consistency>();
    }

    [Fact]
    public async Task Should_count_the_days_with_an_expense()
    {
        var consistency = await NovemberWithout(EmptyNovemberDays);

        Assert.Equal("Hai registrato spese in", consistency.Lead);
        Assert.Equal("26 giorni su 30", consistency.Headline);
        Assert.Equal("Un quadrato pieno per ogni giorno con almeno una spesa.", consistency.Note);
        Assert.Equal("Spese registrate in 26 giorni su 30", consistency.Days.Description);
    }

    [Fact]
    public async Task Should_draw_a_calendar_starting_on_monday_with_blanks_before_the_first()
    {
        var days = (await NovemberWithout(EmptyNovemberDays)).Days;

        // 1 November 2026 is a Sunday: six blanks, then the 30 days.
        Assert.Equal(7, days.Columns);
        Assert.Equal(["L", "M", "M", "G", "V", "S", "D"], days.ColumnLabels);
        Assert.Empty(days.RowLabels);
        Assert.Equal(36, days.Cells.Count);
        Assert.All(days.Cells.Take(6), cell => Assert.Equal(RecapCell.None, cell));
        Assert.Equal(RecapCell.Filled, days.Cells[6]);
        Assert.Equal(RecapCell.Empty, days.Cells[6 + 1]);
        Assert.Equal([2, 9, 23, 30], Enumerable.Range(1, 30).Where(day => days.Cells[6 + day - 1] == RecapCell.Empty));
    }

    [Theory]
    [InlineData(2026, 3, 6)]
    [InlineData(2026, 6, 0)]
    [InlineData(2026, 9, 1)]
    [InlineData(2026, 10, 3)]
    public async Task Should_blank_as_many_cells_as_the_weekday_before_the_first(int year, int month, int blanks)
    {
        var world = new RecapWorld(new DateOnly(2027, 1, 5));
        var spesa = world.Category("Spesa", "mint");
        for (var day = 1; day <= 20; day++)
            world.Spend(spesa, 1_000, new DateOnly(year, month, day));

        var days = (await world.Deck(Period.Month(year, month))).Single<RecapCard.Consistency>().Days;

        Assert.Equal(blanks, days.Cells.TakeWhile(cell => cell == RecapCell.None).Count());
    }

    [Fact]
    public async Task Should_leave_a_day_with_only_an_automatic_expense_empty()
    {
        var world = new RecapWorld(new DateOnly(2026, 12, 1));
        var spesa = world.Category("Spesa", "mint");
        for (var day = 2; day <= 30; day++)
            world.Spend(spesa, 1_000, new DateOnly(2026, 11, day));

        world.Spend(spesa, 80_000, new DateOnly(2026, 11, 1), automatic: true);

        var consistency = (await world.Deck(Period.Month(2026, 11))).Single<RecapCard.Consistency>();

        Assert.Equal("29 giorni su 30", consistency.Headline);
        Assert.Equal(RecapCell.Empty, consistency.Days.Cells[6]);
    }

    [Theory]
    [InlineData(14, false)]
    [InlineData(15, true)]
    public async Task Should_need_half_the_days_of_a_month(int days, bool shown)
    {
        var world = new RecapWorld(new DateOnly(2026, 12, 1));
        var spesa = world.Category("Spesa", "mint");
        for (var day = 1; day <= days; day++)
            world.Spend(spesa, 1_000, new DateOnly(2026, 11, day));

        world.SpendOnDays(spesa, 500, Enumerable.Repeat(new DateOnly(2026, 11, 1), 12).ToArray());

        var deck = await world.Deck(Period.Month(2026, 11));

        Assert.Equal(shown, deck.Cards.OfType<RecapCard.Consistency>().Any());
    }

    private static async Task<RecapCard.Consistency> YearWith(int days, DateOnly today, int year = 2026)
    {
        var world = new RecapWorld(today);
        var spesa = world.Category("Spesa", "mint");
        var first = new DateOnly(year, 1, 1);
        for (var offset = 0; offset < days; offset++)
            world.Spend(spesa, 1_000, first.AddDays(offset));

        return (await world.Deck(Period.Year(year))).Single<RecapCard.Consistency>();
    }

    [Fact]
    public async Task Should_draw_a_year_as_twelve_rows_of_thirty_one_days()
    {
        var consistency = await YearWith(365, new DateOnly(2027, 1, 5));

        var days = consistency.Days;
        Assert.Equal("365 giorni su 365", consistency.Headline);
        Assert.Equal(31, days.Columns);
        Assert.Equal(12 * 31, days.Cells.Count);
        Assert.Equal(["G", "F", "M", "A", "M", "G", "L", "A", "S", "O", "N", "D"], days.RowLabels);
        Assert.Empty(days.ColumnLabels);
        Assert.Equal(365, days.Cells.Count(cell => cell == RecapCell.Filled));
        Assert.Equal(3, days.Cells.Skip(31).Take(31).Count(cell => cell == RecapCell.None));
        Assert.Equal(1, days.Cells.Skip(3 * 31).Take(31).Count(cell => cell == RecapCell.None));
    }

    [Fact]
    public async Task Should_blank_the_days_after_yesterday_in_the_year_so_far()
    {
        var consistency = await YearWith(200, new DateOnly(2026, 7, 20));

        var days = consistency.Days;
        Assert.Equal("200 giorni su 200", consistency.Headline);
        Assert.Equal(200, days.Cells.Count(cell => cell is RecapCell.Filled or RecapCell.Empty));
        Assert.Equal(RecapCell.None, days.Cells[6 * 31 + 19]);
        Assert.Equal(RecapCell.Filled, days.Cells[6 * 31 + 18]);
    }

    [Theory]
    [InlineData(182, false)]
    [InlineData(183, true)]
    public async Task Should_need_half_the_days_of_a_year(int days, bool shown)
    {
        var world = new RecapWorld(new DateOnly(2027, 1, 5));
        var spesa = world.Category("Spesa", "mint");
        for (var offset = 0; offset < days; offset++)
            world.Spend(spesa, 1_000, new DateOnly(2026, 1, 1).AddDays(offset));

        var deck = await world.Deck(Period.Year(2026));

        Assert.Equal(shown, deck.Cards.OfType<RecapCard.Consistency>().Any());
    }

    [Fact]
    public async Task Should_leave_a_summary_row_in_each_wording()
    {
        var month = await NovemberWithout(EmptyNovemberDays);
        var year = await YearWith(287, new DateOnly(2027, 1, 5));

        Assert.Equal(new RecapSummaryRow(MaterialSymbols.CheckCircle, "Costanza", "26 giorni su 30"), month.Row);
        Assert.Equal(new RecapSummaryRow(MaterialSymbols.CheckCircle, "Costanza", "287 su 365"), year.Row);
    }
}
