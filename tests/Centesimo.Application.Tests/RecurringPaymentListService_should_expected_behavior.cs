using Centesimo.Domain;

namespace Centesimo.Application.Tests;

public sealed class RecurringPaymentListService_should_expected_behavior
{
    private static readonly DateOnly Today = new(2026, 11, 19);
    private static readonly Error Failed = new("Test.Failed", "Operazione non riuscita.");

    [Fact]
    public async Task Should_say_what_an_active_payment_is_and_when_it_spends_next()
    {
        var leisure = NewCategory("Svago", icon: "leisure", color: "lilac");
        var netflix = Payment(leisure, 1_299, RecurrenceFrequency.Monthly, new DateOnly(2026, 12, 18), "Netflix");

        var list = await List([leisure], [netflix]);

        Assert.Equal(new RecurringPaymentRow(netflix.RecurringPaymentId, "Netflix", "Svago · ogni mese · prossima il 18 dic",
            "12,99 €", "leisure", "lilac", ""), list.Active.Single());
        Assert.False(list.Active.Single().HasBadge);
        Assert.Equal("Netflix, Svago · ogni mese · prossima il 18 dic, 12,99 €", list.Active.Single().Description);
        Assert.Empty(list.Paused);
        Assert.Empty(list.Ended);
    }

    [Fact]
    public async Task Should_name_each_frequency_and_show_the_year_of_a_date_in_another_year()
    {
        var transport = NewCategory("Trasporti");
        var payments = new[]
        {
            Payment(transport, 500, RecurrenceFrequency.Weekly, new DateOnly(2026, 11, 20), "Parcheggio"),
            Payment(transport, 3_900, RecurrenceFrequency.Monthly, new DateOnly(2026, 12, 1), "Abbonamento bus"),
            Payment(transport, 41_200, RecurrenceFrequency.Yearly, new DateOnly(2027, 3, 14), "Assicurazione auto")
        };

        var list = await List([transport], payments);

        Assert.Equal(
            [
                "Trasporti · ogni settimana · prossima il 20 nov",
                "Trasporti · ogni mese · prossima il 1 dic",
                "Trasporti · ogni anno · prossima il 14 mar 2027"
            ],
            list.Active.Select(row => row.Detail));
    }

    [Fact]
    public async Task Should_count_the_active_payments_in_words()
    {
        var transport = NewCategory("Trasporti");
        var parking = Payment(transport, 500, RecurrenceFrequency.Weekly, new DateOnly(2026, 11, 20), "Parcheggio");
        var bus = Payment(transport, 3_900, RecurrenceFrequency.Monthly, new DateOnly(2026, 12, 1), "Abbonamento bus");
        var insurance = Payment(transport, 41_200, RecurrenceFrequency.Yearly, new DateOnly(2027, 3, 14), "Assicurazione auto");
        var ended = Ended(transport, 900, RecurrenceFrequency.Monthly, new DateOnly(2026, 10, 1), "Vecchio");

        Assert.Equal("Nessuno attivo", (await List([transport], [ended])).ActiveSummary);
        Assert.Equal("1 attivo", (await List([transport], [parking, ended])).ActiveSummary);
        Assert.Equal("3 attivi", (await List([transport], [parking, bus, insurance])).ActiveSummary);
    }

    [Fact]
    public async Task Should_not_name_the_category_twice_when_a_payment_has_no_note()
    {
        var home = NewCategory("Casa");
        var rent = Payment(home, 38_000, RecurrenceFrequency.Monthly, new DateOnly(2026, 12, 1));

        var row = (await List([home], [rent])).Active.Single();

        Assert.Equal(("Casa", "Ogni mese · prossima il 1 dic"), (row.Title, row.Detail));
    }

    [Fact]
    public async Task Should_list_a_paused_payment_apart_with_its_badge()
    {
        var health = NewCategory("Salute");
        var yoga = Payment(health, 4_500, RecurrenceFrequency.Monthly, new DateOnly(2026, 12, 2), "Corso di yoga");
        yoga.Suspend();

        var list = await List([health], [yoga]);

        var row = list.Paused.Single();
        Assert.Equal(("Corso di yoga", "Salute · ogni mese", "In pausa"), (row.Title, row.Detail, row.Badge));
        Assert.True(row.HasBadge);
        Assert.Equal("Corso di yoga, In pausa, Salute · ogni mese, 45,00 €", row.Description);
        Assert.Empty(list.Active);
        Assert.Equal((false, true, false, false), (list.HasActive, list.HasPaused, list.HasEnded, list.IsEmpty));
    }

    [Fact]
    public async Task Should_list_a_terminated_payment_apart_with_its_last_date()
    {
        var other = NewCategory("Altro");
        var magazine = Ended(other, 490, RecurrenceFrequency.Weekly, new DateOnly(2026, 9, 30), "Rivista");
        var old = Ended(other, 900, RecurrenceFrequency.Monthly, new DateOnly(2025, 12, 31), "Vecchio abbonamento");

        var list = await List([other], [old, magazine]);

        Assert.Equal(
            [
                ("Rivista", "Altro · ogni settimana · ultima il 30 set", "Terminato"),
                ("Vecchio abbonamento", "Altro · ogni mese · ultima il 31 dic 2025", "Terminato")
            ],
            list.Ended.Select(row => (row.Title, row.Detail, row.Badge)));
        Assert.Empty(list.Active);
    }

    [Fact]
    public async Task Should_call_a_paused_payment_past_its_end_terminated()
    {
        var other = NewCategory("Altro");
        var magazine = Ended(other, 490, RecurrenceFrequency.Weekly, new DateOnly(2026, 9, 30), "Rivista");
        magazine.Suspend();

        var list = await List([other], [magazine]);

        Assert.Equal("Rivista", list.Ended.Single().Title);
        Assert.Empty(list.Paused);
    }

    [Fact]
    public async Task Should_keep_a_payment_that_ends_on_its_next_date_active_until_then()
    {
        var leisure = NewCategory("Svago");
        var netflix = Payment(leisure, 1_299, RecurrenceFrequency.Monthly, new DateOnly(2026, 12, 18), "Netflix");
        netflix.End(new DateOnly(2026, 12, 18));

        var list = await List([leisure], [netflix]);

        Assert.Equal("Svago · ogni mese · prossima il 18 dic", list.Active.Single().Detail);
        Assert.Empty(list.Ended);
    }

    [Fact]
    public async Task Should_put_the_payment_that_spends_next_first()
    {
        var category = NewCategory("Casa");
        var payments = new[]
        {
            Payment(category, 100, RecurrenceFrequency.Monthly, new DateOnly(2026, 12, 18), "Terzo"),
            Payment(category, 100, RecurrenceFrequency.Monthly, new DateOnly(2026, 11, 20), "Primo"),
            Payment(category, 100, RecurrenceFrequency.Monthly, new DateOnly(2026, 12, 1), "Secondo")
        };

        var list = await List([category], payments);

        Assert.Equal(["Primo", "Secondo", "Terzo"], list.Active.Select(row => row.Title));
    }

    [Fact]
    public async Task Should_still_name_a_payment_of_an_archived_or_missing_category()
    {
        var gym = NewCategory("Palestra", icon: "gym", color: "mint", archived: true);
        var membership = Ended(gym, 3_900, RecurrenceFrequency.Monthly, new DateOnly(2026, 8, 31));
        var orphan = new RecurringPayment(Guid.NewGuid(), Guid.NewGuid(), new Money(700), RecurrenceFrequency.Monthly,
            new DateOnly(2026, 12, 5));

        var list = await List([gym], [membership, orphan]);

        var ended = list.Ended.Single();
        Assert.Equal(("Palestra", "Ogni mese · ultima il 31 ago", "gym", "mint"), (ended.Title, ended.Detail, ended.IconKey, ended.ColorKey));
        var active = list.Active.Single();
        Assert.Equal(("Categoria non disponibile", "Ogni mese · prossima il 5 dic", "more", "grey"),
            (active.Title, active.Detail, active.IconKey, active.ColorKey));
    }

    [Fact]
    public async Task Should_be_empty_without_payments()
    {
        var list = await List([NewCategory("Casa")], []);

        Assert.True(list.IsEmpty);
        Assert.Equal((false, false, false), (list.HasActive, list.HasPaused, list.HasEnded));
    }

    [Fact]
    public async Task Should_return_the_repository_failure()
    {
        var categoryFailure = await new RecurringPaymentListService(new FakeCategoryRepository { Failure = Failed },
            new FakeRecurringPaymentRepository(), () => Today).Get();
        var paymentFailure = await new RecurringPaymentListService(new FakeCategoryRepository(),
            new FakeRecurringPaymentRepository { Failure = Failed }, () => Today).Get();

        Assert.Equal(Failed, categoryFailure.Error);
        Assert.Equal(Failed, paymentFailure.Error);
    }

    private static async Task<RecurringPaymentList> List(IEnumerable<Category> categories, IEnumerable<RecurringPayment> payments)
    {
        var categoryRepository = new FakeCategoryRepository();
        categoryRepository.Items.AddRange(categories);
        var paymentRepository = new FakeRecurringPaymentRepository();
        paymentRepository.Items.AddRange(payments);
        var service = new RecurringPaymentListService(categoryRepository, paymentRepository, () => Today);

        var result = await service.Get();

        Assert.True(result.IsSuccess);
        return result.Value;
    }

    private static Category NewCategory(string name, string icon = "cart", string color = "mint", bool archived = false)
    {
        var category = new Category(Guid.NewGuid(), name, icon, color);
        if (archived)
            category.Archive();

        return category;
    }

    private static RecurringPayment Payment(Category category, long cents, RecurrenceFrequency frequency, DateOnly nextDueOn,
        string note = "") =>
        new(Guid.NewGuid(), category.CategoryId, new Money(cents), frequency, nextDueOn, note: note);

    // A payment whose last occurrence was created on its end date, so the next one falls after it.
    private static RecurringPayment Ended(Category category, long cents, RecurrenceFrequency frequency, DateOnly endsOn,
        string note = "")
    {
        var payment = new RecurringPayment(Guid.NewGuid(), category.CategoryId, new Money(cents), frequency, endsOn,
            note: note, endsOn: endsOn);
        payment.MoveToNextOccurrence();
        return payment;
    }
}
