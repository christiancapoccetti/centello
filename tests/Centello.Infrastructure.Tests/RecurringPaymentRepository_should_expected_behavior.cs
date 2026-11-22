using Centello.Domain;

namespace Centello.Infrastructure.Tests;

public sealed class RecurringPaymentRepository_should_expected_behavior
{
    [Fact]
    public async Task Exclude_payments_whose_next_occurrence_is_after_end_date()
    {
        using var database = new TestDatabase();
        var repository = new RecurringPaymentRepository(database.Context);
        var categoryId = Guid.NewGuid();
        await new CategoryRepository(database.Context).Add(
            new Category(categoryId, "Bills", "receipt", "#123456"), []);
        var completed = new RecurringPayment(Guid.NewGuid(), categoryId, new Money(100),
            RecurrenceFrequency.Monthly, new DateOnly(2026, 8, 1));
        completed.End(new DateOnly(2026, 7, 1));
        var ongoing = new RecurringPayment(Guid.NewGuid(), categoryId, new Money(200),
            RecurrenceFrequency.Monthly, new DateOnly(2026, 8, 1));
        await repository.Add(completed);
        await repository.Add(ongoing);

        var result = await repository.GetDue(new DateOnly(2026, 8, 31));

        Assert.True(result.IsSuccess);
        Assert.Equal(ongoing.RecurringPaymentId, Assert.Single(result.Value).RecurringPaymentId);
    }

    [Fact]
    public async Task Read_a_payment_whose_next_occurrence_is_after_end_date()
    {
        using var database = new TestDatabase();
        var repository = new RecurringPaymentRepository(database.Context);
        var categoryId = Guid.NewGuid();
        await new CategoryRepository(database.Context).Add(
            new Category(categoryId, "Bills", "receipt", "#123456"), []);
        var ended = new RecurringPayment(Guid.NewGuid(), categoryId, new Money(490),
            RecurrenceFrequency.Monthly, new DateOnly(2026, 8, 31), note: "Rivista", endsOn: new DateOnly(2026, 8, 31));
        ended.MoveToNextOccurrence();
        await repository.Add(ended);
        database.Context.ChangeTracker.Clear();

        var all = await repository.GetAll();
        var one = await repository.Get(ended.RecurringPaymentId);

        Assert.True(all.IsSuccess);
        Assert.True(one.IsSuccess);
        var stored = Assert.Single(all.Value);
        Assert.Equal(ended.RecurringPaymentId, stored.RecurringPaymentId);
        Assert.True(stored.IsEnded);
        Assert.Equal((new DateOnly(2026, 9, 30), new DateOnly(2026, 8, 31), "Rivista", 490L),
            (stored.NextDueOn, stored.EndsOn, stored.Note, stored.Amount.Cents));
        Assert.Equal((8, 31), (stored.AnchorMonth, stored.AnchorDay));
        Assert.Equal(stored.NextDueOn, one.Value?.NextDueOn);
    }
}
