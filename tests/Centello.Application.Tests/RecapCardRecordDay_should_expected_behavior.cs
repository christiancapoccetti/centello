using Centello.Domain;

namespace Centello.Application.Tests;

public sealed class RecapCardRecordDay_should_expected_behavior
{
    private const string Nbsp = " ";

    private static (RecapWorld World, Category Spesa, Category Casa) November()
    {
        var world = new RecapWorld(new DateOnly(2026, 12, 1));
        var spesa = world.Category("Spesa", "mint");
        var casa = world.Category("Casa", "peri");
        world.SpendOnDays(spesa, 2_000, new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 3), new DateOnly(2026, 11, 4),
            new DateOnly(2026, 11, 5), new DateOnly(2026, 11, 6), new DateOnly(2026, 11, 8), new DateOnly(2026, 11, 9),
            new DateOnly(2026, 11, 20), new DateOnly(2026, 11, 22), new DateOnly(2026, 11, 27));
        return (world, spesa, casa);
    }

    [Fact]
    public async Task Should_name_the_day_with_the_most_spending_and_its_expenses_largest_first()
    {
        var (world, spesa, _) = November();
        var ristoranti = world.Category("Ristoranti", "butter");
        var trasporti = world.Category("Trasporti", "sky");
        world.Spend(trasporti, 3_030, new DateOnly(2026, 11, 14), note: "Benzina");
        world.Spend(spesa, 9_210, new DateOnly(2026, 11, 14), note: "Spesa della settimana");
        world.Spend(ristoranti, 6_400, new DateOnly(2026, 11, 14), note: "Cena fuori");

        var record = (await world.Deck(Period.Month(2026, 11))).Single<RecapCard.RecordDay>();

        Assert.Equal("Il giorno più caro", record.Lead);
        Assert.Equal("186,40", record.Amount);
        Assert.Equal(new RecapPhrase("", "Sabato 14 novembre", ", in 3 spese"), record.Detail);
        Assert.Equal(
            [("Spesa", "Spesa della settimana", $"92,10{Nbsp}€"), ("Ristoranti", "Cena fuori", $"64,00{Nbsp}€"),
                ("Trasporti", "Benzina", $"30,30{Nbsp}€")],
            record.Expenses.Select(expense => (expense.Category.Name, expense.Note, expense.Amount)));
    }

    [Fact]
    public async Task Should_show_only_the_three_largest_expenses_of_the_day()
    {
        var (world, spesa, _) = November();
        foreach (var cents in new long[] { 1_000, 4_000, 2_000, 3_000, 500 })
            world.Spend(spesa, cents, new DateOnly(2026, 11, 14));

        var record = (await world.Deck(Period.Month(2026, 11))).Single<RecapCard.RecordDay>();

        Assert.Equal(new RecapPhrase("", "Sabato 14 novembre", ", in 5 spese"), record.Detail);
        Assert.Equal([$"40,00{Nbsp}€", $"30,00{Nbsp}€", $"20,00{Nbsp}€"], record.Expenses.Select(expense => expense.Amount));
    }

    [Fact]
    public async Task Should_leave_automatic_expenses_out_and_say_so()
    {
        var (world, spesa, casa) = November();
        world.Spend(casa, 80_000, new DateOnly(2026, 11, 1), automatic: true);
        world.Spend(spesa, 9_210, new DateOnly(2026, 11, 14), note: "Spesa della settimana");

        var record = (await world.Deck(Period.Month(2026, 11))).Single<RecapCard.RecordDay>();

        Assert.Equal("92,10", record.Amount);
        Assert.Equal("Senza le spese automatiche.", record.Note);
    }

    [Fact]
    public async Task Should_not_mention_automatic_expenses_when_there_are_none()
    {
        var (world, spesa, _) = November();
        world.Spend(spesa, 9_210, new DateOnly(2026, 11, 14));

        var record = (await world.Deck(Period.Month(2026, 11))).Single<RecapCard.RecordDay>();

        Assert.Equal("", record.Note);
    }

    [Fact]
    public async Task Should_pick_the_earliest_day_on_a_tie()
    {
        var (world, spesa, _) = November();
        world.Spend(spesa, 9_000, new DateOnly(2026, 11, 21));
        world.Spend(spesa, 9_000, new DateOnly(2026, 11, 7));

        var record = (await world.Deck(Period.Month(2026, 11))).Single<RecapCard.RecordDay>();

        Assert.Equal("Sabato 7 novembre", record.Detail.Emphasis);
    }

    [Fact]
    public async Task Should_draw_the_days_of_the_month_with_the_record_day_strong()
    {
        var (world, spesa, _) = November();
        world.Spend(spesa, 4_000, new DateOnly(2026, 11, 14));

        var days = (await world.Deck(Period.Month(2026, 11))).Single<RecapCard.RecordDay>().Days!;

        Assert.Equal(30, days.Bars.Count);
        Assert.Equal(RecapBarKind.Strong, days.Bars[13].Kind);
        Assert.Equal(1.0, days.Bars[13].Height);
        Assert.Equal(0.5, days.Bars[1].Height);
        Assert.Equal(RecapBarKind.Bar, days.Bars[1].Kind);
        Assert.Equal(RecapBarKind.Stub, days.Bars[0].Kind);
    }

    [Theory]
    [InlineData(2, false)]
    [InlineData(3, true)]
    public async Task Should_need_three_days_with_variable_spending(int days, bool shown)
    {
        var world = new RecapWorld(new DateOnly(2026, 12, 1));
        var spesa = world.Category("Spesa", "mint");
        var casa = world.Category("Casa", "peri");
        for (var day = 1; day <= days; day++)
            world.Spend(spesa, 2_000, new DateOnly(2026, 11, day));

        for (var day = 10; day <= 20; day++)
            world.Spend(casa, 1_000, new DateOnly(2026, 11, day), automatic: true);

        var deck = await world.Deck(Period.Month(2026, 11));

        Assert.Equal(shown, deck.Cards.OfType<RecapCard.RecordDay>().Any());
    }

    [Fact]
    public async Task Should_leave_a_summary_row_with_the_day_and_its_amount()
    {
        var (world, spesa, _) = November();
        world.Spend(spesa, 18_640, new DateOnly(2026, 11, 14));

        var record = (await world.Deck(Period.Month(2026, 11))).Single<RecapCard.RecordDay>();

        Assert.Equal(new RecapSummaryRow(MaterialSymbols.Event, "Il giorno più caro", $"sab 14 · 186,40{Nbsp}€"), record.Row);
    }

    [Fact]
    public async Task Should_state_the_record_day_of_a_year_without_a_chart()
    {
        var world = new RecapWorld(new DateOnly(2027, 1, 5));
        var spesa = world.Category("Spesa", "mint");
        var trasporti = world.Category("Trasporti", "sky");
        world.SpendInYear(spesa, 2026, 1_200_000, 40);
        world.Spend(trasporti, 28_600, new DateOnly(2026, 8, 8), note: "Traghetto per l'Elba");
        world.Spend(spesa, 12_680, new DateOnly(2026, 8, 8));

        var record = (await world.Deck(Period.Year(2026))).Single<RecapCard.RecordDay>();

        Assert.Equal("Il giorno più caro dell'anno", record.Lead);
        Assert.Equal("412,80", record.Amount);
        Assert.Equal(new RecapPhrase("", "Sabato 8 agosto", ", in 2 spese"), record.Detail);
        Assert.Null(record.Days);
        Assert.Equal($"8 ago · 412,80{Nbsp}€", record.Row!.Value);
    }
}
