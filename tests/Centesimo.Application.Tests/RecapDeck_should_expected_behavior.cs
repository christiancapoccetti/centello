using Centesimo.Domain;

namespace Centesimo.Application.Tests;

public sealed class RecapDeck_should_expected_behavior
{
    private const string Nbsp = " ";

    private static readonly Type[] MonthKinds =
    [
        typeof(RecapCard.Cover), typeof(RecapCard.Total), typeof(RecapCard.Rank), typeof(RecapCard.RecordDay),
        typeof(RecapCard.Categories), typeof(RecapCard.Changes), typeof(RecapCard.NewHabit), typeof(RecapCard.SmallExpenses),
        typeof(RecapCard.Consistency), typeof(RecapCard.YearSoFar), typeof(RecapCard.Summary)
    ];

    private static readonly Type[] YearKinds =
    [
        typeof(RecapCard.Cover), typeof(RecapCard.Total), typeof(RecapCard.Months), typeof(RecapCard.RecordDay),
        typeof(RecapCard.Weekday), typeof(RecapCard.Categories), typeof(RecapCard.Changes), typeof(RecapCard.TagOfYear),
        typeof(RecapCard.CostliestTag), typeof(RecapCard.SteadyTag), typeof(RecapCard.NewHabits),
        typeof(RecapCard.SmallExpenses), typeof(RecapCard.Automatic), typeof(RecapCard.Consistency), typeof(RecapCard.Summary)
    ];

    [Fact]
    public async Task Should_open_with_the_cover_and_close_with_the_summary_in_the_order_of_a_month()
    {
        var deck = await RecapWorld.MockupNovember().Deck(Period.Month(2026, 11));

        Assert.Equal(MonthKinds, deck.Cards.Select(card => card.GetType()));
    }

    [Fact]
    public async Task Should_open_with_the_cover_and_close_with_the_summary_in_the_order_of_a_year()
    {
        var deck = await RecapWorld.Mockup2026().Deck(Period.Year(2026));

        Assert.Equal(YearKinds, deck.Cards.Select(card => card.GetType()));
    }

    [Fact]
    public async Task Should_draw_every_kind_of_card_in_one_of_the_two_decks()
    {
        var month = await RecapWorld.MockupNovember().Deck(Period.Month(2026, 11));
        var year = await RecapWorld.Mockup2026().Deck(Period.Year(2026));

        var kinds = typeof(RecapCard).GetNestedTypes().Where(kind => kind.IsSubclassOf(typeof(RecapCard)));

        Assert.Equal(
            kinds.Select(kind => kind.Name).Order(),
            month.Cards.Concat(year.Cards).Select(card => card.GetType().Name).Distinct().Order());
    }


    [Fact]
    public async Task Should_say_november_in_the_words_of_the_mockup()
    {
        var deck = await RecapWorld.MockupNovember().Deck(Period.Month(2026, 11));

        var total = deck.Single<RecapCard.Total>();
        Assert.Equal("Il tuo\nnovembre", deck.Single<RecapCard.Cover>().Title);
        Assert.Equal(("A novembre hai speso", "1.284,60", "in 96 spese"), (total.Lead, total.Amount, total.Detail.Lead));
        Assert.Equal([$"112,20{Nbsp}€ meno di ottobre", $"48,50{Nbsp}€ più di novembre 2025"], total.Chips.Select(chip => chip.Text));

        var rank = deck.Single<RecapCard.Rank>();
        Assert.Equal(("Il secondo mese più leggero del 2026", $"Solo febbraio è costato meno: 1.198{Nbsp}€."), (rank.Headline, rank.Detail));

        var record = deck.Single<RecapCard.RecordDay>();
        Assert.Equal(("Il giorno più caro", "186,40", "Senza le spese automatiche."), (record.Lead, record.Amount, record.Note));
        Assert.Equal(new RecapPhrase("", "Sabato 14 novembre", ", in 3 spese"), record.Detail);
        Assert.Equal(["Spesa", "Ristoranti", "Trasporti"], record.Expenses.Select(expense => expense.Category.Name));

        var categories = deck.Single<RecapCard.Categories>();
        Assert.Equal(new RecapPhrase("Dove sono andati i ", $"1.284,60{Nbsp}€"), categories.Lead);
        Assert.Equal(
            ["Casa", "Spesa", "Ristoranti", "Trasporti", "Svago", "Bar", "Altre 3 categorie"],
            categories.Shares.Select(share => share.Category.Name));
        Assert.Equal(("25%", $"320,00{Nbsp}€"), (categories.Shares[0].Share, categories.Shares[0].Amount));
        Assert.Equal(("14%", $"182,50{Nbsp}€"), (categories.Shares[2].Share, categories.Shares[2].Amount));
        Assert.Equal(("5%", $"58,20{Nbsp}€"), (categories.Shares[5].Share, categories.Shares[5].Amount));
        Assert.Equal(("14%", $"174,00{Nbsp}€"), (categories.Shares[6].Share, categories.Shares[6].Amount));

        var changes = deck.Single<RecapCard.Changes>();
        Assert.Equal(
            ("Ristoranti", "+64,20", $"Da 118,30 a 182,50{Nbsp}€ rispetto a ottobre."),
            (changes.Rise!.Category.Name, changes.Rise.Amount, changes.Rise.Detail));
        Assert.Equal(
            ("Trasporti", "−48,60", $"Da 189,80 a 141,20{Nbsp}€."),
            (changes.Fall!.Category.Name, changes.Fall.Amount, changes.Fall.Detail));

        var habit = deck.Single<RecapCard.NewHabit>();
        Assert.Equal(("Una nuova abitudine", "Piscina"), (habit.Lead, habit.Tag));
        Assert.Equal(
            new RecapPhrase("", "5 volte", $", 42,50{Nbsp}€ in tutto, nella categoria Svago. A ottobre nessuna."),
            habit.Detail);

        var small = deck.Single<RecapCard.SmallExpenses>();
        Assert.Equal(
            ("Caffè", $"22 volte, in media 1,30{Nbsp}€", $"28,60{Nbsp}€"),
            (small.TopTag!.Tag, small.TopTag.Detail, small.TopTag.Amount));
        Assert.Equal(
            "22 coral, 5 mint, 4 sky",
            string.Join(", ", small.Units.ColorKeys.GroupBy(key => key).Select(group => $"{group.Count()} {group.Key}")));

        Assert.Equal("26 giorni su 30", deck.Single<RecapCard.Consistency>().Headline);

        var yearSoFar = deck.Single<RecapCard.YearSoFar>();
        Assert.Equal("16.071", yearSoFar.Amount);
        Assert.Equal($"349{Nbsp}€ meno del 2025 a fine novembre", yearSoFar.Chip!.Text);
        Assert.Equal(new RecapPhrase("2025, ", $"16.420{Nbsp}€"), yearSoFar.LastYear);

        Assert.Equal(
            [
                ("Totale", $"1.284,60{Nbsp}€"), ("Nell'anno", "2° più leggero"),
                ("Il giorno più caro", $"sab 14 · 186,40{Nbsp}€"), ("Prima categoria", "Casa · 25%"),
                ("Cresciuta di più", $"Ristoranti +64{Nbsp}€"), ("Nuova abitudine", "Piscina"),
                ("Costanza", "26 giorni su 30")
            ],
            deck.Single<RecapCard.Summary>().Rows.Select(row => (row.Label, row.Value)));
    }

    [Fact]
    public async Task Should_say_the_year_in_the_words_of_the_mockup()
    {
        var deck = await RecapWorld.Mockup2026().Deck(Period.Year(2026));

        var total = deck.Single<RecapCard.Total>();
        Assert.Equal("Il tuo\n2026", deck.Single<RecapCard.Cover>().Title);
        Assert.Equal(("Nel 2026 hai speso", "17.955"), (total.Lead, total.Amount));
        Assert.Equal([$"385{Nbsp}€ meno del 2025"], total.Chips.Select(chip => chip.Text));

        var months = deck.Single<RecapCard.Months>();
        Assert.Equal(
            ("Agosto il più caro, febbraio il più leggero", $"1.948{Nbsp}€ contro 1.198{Nbsp}€."),
            (months.Headline, months.Detail));

        var record = deck.Single<RecapCard.RecordDay>();
        Assert.Equal(("Il giorno più caro dell'anno", "412,80"), (record.Lead, record.Amount));
        Assert.Equal(new RecapPhrase("", "Sabato 8 agosto", ", in 3 spese"), record.Detail);
        Assert.Equal(["Trasporti", "Ristoranti", "Spesa"], record.Expenses.Select(expense => expense.Category.Name));

        var weekday = deck.Single<RecapCard.Weekday>();
        Assert.Equal("Il sabato è il tuo giorno", weekday.Headline);
        Assert.Equal(new RecapPhrase("Il ", "24%", " delle spese variabili cade di sabato."), weekday.Detail);

        var changes = deck.Single<RecapCard.Changes>();
        Assert.Equal(
            ("Ristoranti", "+620", $"Da 1.560 a 2.180{Nbsp}€ rispetto al 2025."),
            (changes.Rise!.Category.Name, changes.Rise.Amount, changes.Rise.Detail));
        Assert.Equal(
            ("Trasporti", "−540", $"Da 2.500 a 1.960{Nbsp}€."),
            (changes.Fall!.Category.Name, changes.Fall.Amount, changes.Fall.Detail));

        var tagOfYear = deck.Single<RecapCard.TagOfYear>();
        Assert.Equal("Caffè", tagOfYear.Tag);
        Assert.Equal(new RecapPhrase("", "262 volte", $", 340,60{Nbsp}€ in tutto, 1,30{Nbsp}€ in media."), tagOfYear.Detail);

        var costliest = deck.Single<RecapCard.CostliestTag>();
        Assert.Equal(("Pizza", new RecapPhrase("", $"512,40{Nbsp}€ in tutto", ", 41 volte")), (costliest.Tag, costliest.Detail));

        var steady = deck.Single<RecapCard.SteadyTag>();
        Assert.Equal(("Pane", "40 settimane su 52"), (steady.Tag, steady.Detail.Emphasis));

        var habits = deck.Single<RecapCard.NewHabits>();
        Assert.Equal("Tre abitudini nuove", habits.Headline);
        Assert.Equal(
            [
                ("Piscina", $"153,00{Nbsp}€", "Svago, da settembre, 18 volte"),
                ("Libri", $"146,70{Nbsp}€", "Svago, da marzo, 9 volte"),
                ("Veterinario", $"210,00{Nbsp}€", "Spesa, da giugno, 4 volte")
            ],
            habits.Habits.Select(habit => (habit.Tag, habit.Amount, habit.Detail)));

        var automatic = deck.Single<RecapCard.Automatic>();
        Assert.Equal("Il 27% era già deciso", automatic.Headline);
        Assert.Equal(
            ["Affitto", "Assicurazione auto", "Netflix", "Spotify", "Telefono"],
            automatic.Payments.Select(line => line.Name));
        Assert.Equal(new RecapPhrase("Abbonamenti senza Affitto: ", $"1.031,64{Nbsp}€", " l'anno."), automatic.Note);

        Assert.Equal(12 * 31, deck.Single<RecapCard.Consistency>().Days.Cells.Count);
    }
    [Fact]
    public async Task Should_draw_one_summary_row_per_card_that_leaves_one()
    {
        var deck = await RecapWorld.MockupNovember().Deck(Period.Month(2026, 11));

        var rows = deck.Cards.Skip(1).SkipLast(1).Select(card => card.Row).OfType<RecapSummaryRow>();

        Assert.Equal(rows, deck.Single<RecapCard.Summary>().Rows);
    }
}
