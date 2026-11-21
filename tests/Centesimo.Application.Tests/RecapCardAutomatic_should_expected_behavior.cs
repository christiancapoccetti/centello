using Centesimo.Domain;

namespace Centesimo.Application.Tests;

public sealed class RecapCardAutomatic_should_expected_behavior
{
    private const string Nbsp = " ";

    private static RecapWorld World(out Category casa, long variableCents = 1_200_000)
    {
        var world = new RecapWorld(new DateOnly(2027, 1, 5));
        casa = world.Category("Casa", "peri");
        world.SpendInYear(world.Category("Spesa", "mint"), 2026, variableCents, 40);
        return world;
    }

    private static void Pay(RecapWorld world, Category category, string note, long cents, int times = 12)
    {
        var payment = Guid.NewGuid();
        for (var month = 1; month <= times; month++)
            world.Spend(category, cents, new DateOnly(2026, month, 1), note: note, payment: payment);
    }

    private static void FivePayments(RecapWorld world, Category casa)
    {
        Pay(world, casa, "Affitto", 32_000);
        Pay(world, casa, "Assicurazione auto", 5_100);
        Pay(world, casa, "Netflix", 1_399);
        Pay(world, casa, "Spotify", 1_099);
        Pay(world, casa, "Telefono", 999);
    }

    [Fact]
    public async Task Should_state_how_much_of_the_year_was_already_decided()
    {
        var world = World(out var casa);
        FivePayments(world, casa);

        var automatic = (await world.Deck(Period.Year(2026))).Single<RecapCard.Automatic>();

        Assert.Equal("Il 29% era già deciso", automatic.Headline);
        Assert.Equal(new RecapPhrase("", $"4.872{Nbsp}€", $" in spese automatiche, 12.000{Nbsp}€ variabili."), automatic.Detail);
        Assert.Equal(487_164 / 1_687_164.0, automatic.Split.Automatic);
        Assert.Equal(("Automatiche", "Variabili"), (automatic.Split.AutomaticName, automatic.Split.VariableName));
    }

    [Fact]
    public async Task Should_list_the_payments_largest_first_and_total_them_without_the_largest()
    {
        var world = World(out var casa);
        FivePayments(world, casa);

        var automatic = (await world.Deck(Period.Year(2026))).Single<RecapCard.Automatic>();

        Assert.Equal(
            [
                new RecapLine("Affitto", $"3.840,00{Nbsp}€"), new RecapLine("Assicurazione auto", $"612,00{Nbsp}€"),
                new RecapLine("Netflix", $"167,88{Nbsp}€"), new RecapLine("Spotify", $"131,88{Nbsp}€"),
                new RecapLine("Telefono", $"119,88{Nbsp}€")
            ],
            automatic.Payments);
        Assert.Equal(new RecapPhrase("Abbonamenti senza Affitto: ", $"1.031,64{Nbsp}€", " l'anno."), automatic.Note);
    }

    [Fact]
    public async Task Should_list_five_payments_but_total_all_but_the_largest()
    {
        var world = World(out var casa);
        FivePayments(world, casa);
        Pay(world, casa, "Palestra", 700);

        var automatic = (await world.Deck(Period.Year(2026))).Single<RecapCard.Automatic>();

        Assert.Equal(5, automatic.Payments.Count);
        Assert.DoesNotContain(automatic.Payments, line => line.Name == "Palestra");
        Assert.Equal($"1.115,64{Nbsp}€", automatic.Note.Emphasis);
    }

    [Fact]
    public async Task Should_name_a_payment_by_its_latest_note()
    {
        var world = World(out var casa);
        var rent = Guid.NewGuid();
        world.Spend(casa, 30_000, new DateOnly(2026, 1, 1), note: "Affitto vecchio", payment: rent);
        world.Spend(casa, 32_000, new DateOnly(2026, 6, 1), note: "Affitto", payment: rent);
        world.Spend(casa, 32_000, new DateOnly(2026, 7, 1), payment: rent);
        Pay(world, casa, "Netflix", 1_399);

        var automatic = (await world.Deck(Period.Year(2026))).Single<RecapCard.Automatic>();

        Assert.Equal("Affitto", automatic.Payments[0].Name);
    }

    [Fact]
    public async Task Should_name_a_payment_without_a_note_by_its_category()
    {
        var world = World(out var casa);
        Pay(world, casa, "", 32_000);
        Pay(world, casa, "Netflix", 1_399);

        var automatic = (await world.Deck(Period.Year(2026))).Single<RecapCard.Automatic>();

        Assert.Equal("Casa", automatic.Payments[0].Name);
    }

    [Fact]
    public async Task Should_name_a_payment_without_a_note_or_a_known_category_regular()
    {
        var world = World(out var casa);
        Pay(world, new Category(Guid.NewGuid(), "Cancellata", "more", "sky"), "", 32_000);
        Pay(world, casa, "Netflix", 1_399);

        var automatic = (await world.Deck(Period.Year(2026))).Single<RecapCard.Automatic>();

        Assert.Equal("Pagamento regolare", automatic.Payments[0].Name);
    }

    [Fact]
    public async Task Should_need_two_payments()
    {
        var world = World(out var casa);
        Pay(world, casa, "Affitto", 32_000);

        var deck = await world.Deck(Period.Year(2026));

        Assert.DoesNotContain(deck.Cards, card => card is RecapCard.Automatic);
    }

    [Theory]
    [InlineData(24_999, false)]
    [InlineData(25_000, true)]
    public async Task Should_need_five_percent_of_the_total(long second, bool shown)
    {
        var world = World(out var casa, variableCents: 950_000);
        Pay(world, casa, "Affitto", 25_000, times: 1);
        Pay(world, casa, "Netflix", second, times: 1);

        var deck = await world.Deck(Period.Year(2026));

        Assert.Equal(shown, deck.Cards.OfType<RecapCard.Automatic>().Any());
    }

    [Fact]
    public async Task Should_elide_the_article_before_eight_percent()
    {
        var world = World(out var casa, variableCents: 920_000);
        Pay(world, casa, "Affitto", 40_000, times: 1);
        Pay(world, casa, "Netflix", 40_000, times: 1);

        var automatic = (await world.Deck(Period.Year(2026))).Single<RecapCard.Automatic>();

        Assert.Equal("L'8% era già deciso", automatic.Headline);
    }

    [Fact]
    public async Task Should_count_the_year_so_far_as_so_far()
    {
        var world = new RecapWorld(new DateOnly(2026, 12, 31));
        var casa = world.Category("Casa", "peri");
        world.SpendInYear(world.Category("Spesa", "mint"), 2026, 1_000_000, 40, lastMonth: 10);
        Pay(world, casa, "Affitto", 32_000, times: 10);
        Pay(world, casa, "Netflix", 1_399, times: 10);

        var automatic = (await world.Deck(Period.Year(2026))).Single<RecapCard.Automatic>();

        Assert.Equal(" finora.", automatic.Note.Tail);
    }

    [Fact]
    public async Task Should_leave_a_summary_row_with_the_share()
    {
        var world = World(out var casa);
        FivePayments(world, casa);

        var automatic = (await world.Deck(Period.Year(2026))).Single<RecapCard.Automatic>();

        Assert.Equal(new RecapSummaryRow(MaterialSymbols.Autorenew, "Automatico", "29%"), automatic.Row);
    }

    [Fact]
    public async Task Should_not_belong_to_a_month()
    {
        var world = new RecapWorld(new DateOnly(2026, 12, 1));
        var casa = world.Category("Casa", "peri");
        world.SpendInMonth(world.Category("Spesa", "mint"), 2026, 11, 100_000, 12);
        world.Spend(casa, 32_000, new DateOnly(2026, 11, 1), automatic: true);
        world.Spend(casa, 1_399, new DateOnly(2026, 11, 2), automatic: true);

        var deck = await world.Deck(Period.Month(2026, 11));

        Assert.DoesNotContain(deck.Cards, card => card is RecapCard.Automatic);
    }
}
