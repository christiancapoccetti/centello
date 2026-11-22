using Centello.Domain;

namespace Centello.Application.Tests;

public sealed partial class RecurringPaymentService_should_expected_behavior
{
    [Fact]
    public async Task Catch_up_missed_occurrences_and_remain_idempotent()
    {
        var categories = new FakeCategoryRepository();
        var category = new Category(Guid.NewGuid(), "Bills", "receipt", "#123456");
        categories.Items.Add(category);
        var expenses = new FakeExpenseRepository();
        var payments = new FakeRecurringPaymentRepository();
        var service = new RecurringPaymentService(categories, new FakeTagRepository(), expenses, payments,
            new FakeRecurringOccurrenceProcessor(expenses));
        await service.Create(new(category.CategoryId, 1_000, RecurrenceFrequency.Monthly, new DateOnly(2026, 5, 1)));

        var firstRun = await service.ProcessDue(new DateOnly(2026, 7, 20));
        var secondRun = await service.ProcessDue(new DateOnly(2026, 7, 20));

        Assert.Equal(3, firstRun.Value);
        Assert.Equal(0, secondRun.Value);
        Assert.Equal(3, expenses.Items.Count);
    }

    [Fact]
    public async Task Create_occurrence_expenses_with_time_ordered_ids()
    {
        var categories = new FakeCategoryRepository();
        var category = new Category(Guid.NewGuid(), "Bills", "receipt", "#123456");
        categories.Items.Add(category);
        var expenses = new FakeExpenseRepository();
        var service = new RecurringPaymentService(categories, new FakeTagRepository(), expenses,
            new FakeRecurringPaymentRepository(), new FakeRecurringOccurrenceProcessor(expenses));
        await service.Create(new(category.CategoryId, 1_000, RecurrenceFrequency.Monthly, new DateOnly(2026, 5, 1)));

        await service.ProcessDue(new DateOnly(2026, 7, 20));

        Assert.All(expenses.Items, expense => Assert.Equal(7, expense.ExpenseId.Version));
    }

    [Fact]
    public async Task Suspend_update_and_end_payment()
    {
        var categories = new FakeCategoryRepository();
        var category = new Category(Guid.NewGuid(), "Bills", "receipt", "#123456");
        categories.Items.Add(category);
        var payments = new FakeRecurringPaymentRepository();
        var expenses = new FakeExpenseRepository();
        var service = new RecurringPaymentService(categories, new FakeTagRepository(), expenses, payments,
            new FakeRecurringOccurrenceProcessor(expenses), () => new DateOnly(2026, 7, 1));
        var created = await service.Create(new(category.CategoryId, 1_000, RecurrenceFrequency.Weekly, new DateOnly(2026, 7, 20)));

        var updated = await service.Update(created.Value.RecurringPaymentId, new(category.CategoryId, 2_000, RecurrenceFrequency.Yearly, new DateOnly(2026, 8, 1)));
        var suspended = await service.Suspend(created.Value.RecurringPaymentId);
        var ended = await service.End(created.Value.RecurringPaymentId, new DateOnly(2027, 1, 1));

        Assert.True(updated.IsSuccess && suspended.IsSuccess && ended.IsSuccess);
        Assert.False(created.Value.IsSuspended);
        Assert.Equal(RecurrenceFrequency.Yearly, created.Value.Frequency);
    }

    [Fact]
    public async Task Resume_suspended_payment_and_reject_invalid_end_date()
    {
        var categories = new FakeCategoryRepository();
        var category = new Category(Guid.NewGuid(), "Bills", "receipt", "#123456");
        categories.Items.Add(category);
        var expenses = new FakeExpenseRepository();
        var payments = new FakeRecurringPaymentRepository();
        var service = new RecurringPaymentService(categories, new FakeTagRepository(), expenses,
            payments, new FakeRecurringOccurrenceProcessor(expenses), () => new DateOnly(2026, 7, 1));
        var created = await service.Create(new(category.CategoryId, 1_000, RecurrenceFrequency.Monthly, new DateOnly(2026, 7, 20)));
        await service.Suspend(created.Value.RecurringPaymentId);

        var resumed = await service.Resume(created.Value.RecurringPaymentId);
        var invalidEnd = await service.End(created.Value.RecurringPaymentId, new DateOnly(2026, 7, 19));

        Assert.True(resumed.IsSuccess);
        Assert.False(created.Value.IsSuspended);
        Assert.Equal("RecurringPayment.InvalidEndDate", invalidEnd.Error.Code);
    }

    [Fact]
    public async Task Resume_without_creating_the_expenses_missed_while_paused()
    {
        var (service, payment, expenses) = await ServiceWithPaymentAndExpenses(
            RecurrenceFrequency.Monthly, new DateOnly(2026, 6, 15), today: new DateOnly(2026, 9, 10));
        await service.Suspend(payment.RecurringPaymentId);

        await service.Resume(payment.RecurringPaymentId);
        await service.ProcessDue(new DateOnly(2026, 9, 10));
        await service.ProcessDue(new DateOnly(2026, 9, 20));

        Assert.Equal([new DateOnly(2026, 9, 15)], expenses.Items.Select(expense => expense.OccurredOn));
        Assert.Equal(new DateOnly(2026, 10, 15), payment.NextDueOn);
    }

    [Fact]
    public async Task Create_the_occurrence_due_on_the_day_it_is_resumed()
    {
        var (service, payment, expenses) = await ServiceWithPaymentAndExpenses(
            RecurrenceFrequency.Monthly, new DateOnly(2026, 6, 15), today: new DateOnly(2026, 9, 15));
        await service.Suspend(payment.RecurringPaymentId);

        await service.Resume(payment.RecurringPaymentId);
        await service.ProcessDue(new DateOnly(2026, 9, 15));

        Assert.Equal([new DateOnly(2026, 9, 15)], expenses.Items.Select(expense => expense.OccurredOn));
    }

    [Fact]
    public async Task End_a_payment_whose_end_date_passed_while_it_was_paused()
    {
        var (service, payment, expenses) = await ServiceWithPaymentAndExpenses(
            RecurrenceFrequency.Monthly, new DateOnly(2026, 6, 15), endsOn: new DateOnly(2026, 8, 15),
            today: new DateOnly(2026, 9, 20));
        await service.Suspend(payment.RecurringPaymentId);

        await service.Resume(payment.RecurringPaymentId);
        await service.ProcessDue(new DateOnly(2026, 9, 20));

        Assert.True(payment.IsEnded);
        Assert.Empty(expenses.Items);
    }

    [Theory]
    [InlineData(RecurrenceFrequency.Weekly, "2026-06-01", "2026-06-23", "2026-06-29")]
    [InlineData(RecurrenceFrequency.Yearly, "2024-03-10", "2026-05-01", "2027-03-10")]
    public async Task Resume_weekly_and_yearly_payments_from_the_first_occurrence_after_the_pause(
        RecurrenceFrequency frequency, string dueOn, string today, string expectedNext)
    {
        var (service, payment, expenses) = await ServiceWithPaymentAndExpenses(
            frequency, DateOnly.Parse(dueOn), today: DateOnly.Parse(today));
        await service.Suspend(payment.RecurringPaymentId);

        await service.Resume(payment.RecurringPaymentId);
        await service.ProcessDue(DateOnly.Parse(today));

        Assert.Empty(expenses.Items);
        Assert.Equal(DateOnly.Parse(expectedNext), payment.NextDueOn);
    }

    [Fact]
    public async Task Return_recurring_payments_ordered_by_next_due_date()
    {
        var repository = new FakeRecurringPaymentRepository();
        var categoryId = Guid.NewGuid();
        repository.Items.Add(new(Guid.NewGuid(), categoryId, new Money(100), RecurrenceFrequency.Monthly,
            new DateOnly(2026, 9, 1)));
        repository.Items.Add(new(Guid.NewGuid(), categoryId, new Money(200), RecurrenceFrequency.Weekly,
            new DateOnly(2026, 8, 1)));
        var service = new RecurringPaymentService(new FakeCategoryRepository(), new FakeTagRepository(),
            new FakeExpenseRepository(), repository, new FakeRecurringOccurrenceProcessor(new FakeExpenseRepository()));

        var result = await service.GetAll();

        Assert.True(result.IsSuccess);
        Assert.Equal(new DateOnly(2026, 8, 1), result.Value[0].NextDueOn);
    }

    [Fact]
    public async Task Preview_ending_a_payment_on_its_next_due_date()
    {
        var (service, payment) = await ServiceWithPayment(RecurrenceFrequency.Monthly, new DateOnly(2026, 12, 5));

        var preview = await service.PreviewEnd(payment.RecurringPaymentId);

        Assert.True(preview.IsSuccess);
        Assert.Equal(new DateOnly(2026, 12, 5), preview.Value.LastExpenseOn);
        Assert.Equal("L'ultima spesa sarà quella del 5 dicembre, poi non ne verranno create altre.", preview.Value.Message);
    }

    [Theory]
    [InlineData(2026, 8, 8, "L'ultima spesa sarà quella dell'8 agosto, poi non ne verranno create altre.")]
    [InlineData(2026, 11, 11, "L'ultima spesa sarà quella dell'11 novembre, poi non ne verranno create altre.")]
    [InlineData(2027, 1, 1, "L'ultima spesa sarà quella del 1 gennaio, poi non ne verranno create altre.")]
    [InlineData(2026, 3, 18, "L'ultima spesa sarà quella del 18 marzo, poi non ne verranno create altre.")]
    public async Task Elide_the_article_before_a_date_that_starts_with_a_vowel(int year, int month, int day, string expected)
    {
        var (service, payment) = await ServiceWithPayment(RecurrenceFrequency.Weekly, new DateOnly(year, month, day));

        var preview = await service.PreviewEnd(payment.RecurringPaymentId);

        Assert.Equal(expected, preview.Value.Message);
    }

    [Fact]
    public async Task Preview_ending_ignores_a_later_end_date_the_payment_already_has()
    {
        var (service, payment) = await ServiceWithPayment(RecurrenceFrequency.Monthly, new DateOnly(2026, 12, 5),
            endsOn: new DateOnly(2027, 12, 5));

        var preview = await service.PreviewEnd(payment.RecurringPaymentId);

        Assert.Equal(new DateOnly(2026, 12, 5), preview.Value.LastExpenseOn);
    }

    [Fact]
    public async Task Preview_the_last_expense_ending_does_create()
    {
        var (service, payment, expenses) = await ServiceWithPaymentAndExpenses(RecurrenceFrequency.Monthly, new DateOnly(2026, 12, 5));
        var preview = await service.PreviewEnd(payment.RecurringPaymentId);

        await service.End(payment.RecurringPaymentId, preview.Value.LastExpenseOn);
        await service.ProcessDue(new DateOnly(2027, 6, 1));

        Assert.Equal([preview.Value.LastExpenseOn], expenses.Items.Select(expense => expense.OccurredOn));
    }

    [Fact]
    public async Task Refuse_to_preview_ending_a_payment_that_has_already_ended()
    {
        var (service, payment) = await ServiceWithPayment(RecurrenceFrequency.Monthly, new DateOnly(2026, 12, 5),
            endsOn: new DateOnly(2026, 12, 5));
        payment.MoveToNextOccurrence();

        var preview = await service.PreviewEnd(payment.RecurringPaymentId);

        Assert.Equal("RecurringPayment.AlreadyEnded", preview.Error.Code);
    }

    [Fact]
    public async Task Preview_ending_a_paused_payment_on_the_occurrence_it_will_really_create()
    {
        var (service, payment) = await ServiceWithPayment(RecurrenceFrequency.Monthly, new DateOnly(2026, 10, 5),
            today: new DateOnly(2026, 11, 20));
        await service.Suspend(payment.RecurringPaymentId);

        var preview = await service.PreviewEnd(payment.RecurringPaymentId);

        Assert.Equal(new DateOnly(2026, 12, 5), preview.Value.LastExpenseOn);
        Assert.Equal("L'ultima spesa sarà quella del 5 dicembre, poi non ne verranno create altre.", preview.Value.Message);
        Assert.Equal(new DateOnly(2026, 10, 5), payment.NextDueOn);
        Assert.True(payment.IsSuspended);
    }

    [Fact]
    public async Task Create_the_promised_last_expense_when_a_paused_payment_is_ended()
    {
        var (service, payment, expenses) = await ServiceWithPaymentAndExpenses(
            RecurrenceFrequency.Monthly, new DateOnly(2026, 10, 5), today: new DateOnly(2026, 11, 20));
        await service.Suspend(payment.RecurringPaymentId);
        var preview = await service.PreviewEnd(payment.RecurringPaymentId);

        await service.End(payment.RecurringPaymentId, preview.Value.LastExpenseOn);
        await service.Resume(payment.RecurringPaymentId);
        await service.ProcessDue(new DateOnly(2027, 6, 1));

        Assert.Equal([preview.Value.LastExpenseOn], expenses.Items.Select(expense => expense.OccurredOn));
    }

    [Fact]
    public async Task Show_a_paused_payment_as_ended_once_its_last_expense_is_created()
    {
        var (service, payment, expenses) = await ServiceWithPaymentAndExpenses(
            RecurrenceFrequency.Monthly, new DateOnly(2026, 10, 5), today: new DateOnly(2026, 11, 20));
        await service.Suspend(payment.RecurringPaymentId);
        var preview = await service.PreviewEnd(payment.RecurringPaymentId);

        await service.End(payment.RecurringPaymentId, preview.Value.LastExpenseOn);
        await service.ProcessDue(new DateOnly(2026, 12, 5));

        Assert.Single(expenses.Items);
        Assert.Equal(RecurringPaymentStatus.Ended, payment.Status);
    }

    [Fact]
    public async Task Refuse_to_preview_ending_a_paused_payment_whose_end_date_passed_during_the_pause()
    {
        var (service, payment) = await ServiceWithPayment(RecurrenceFrequency.Monthly, new DateOnly(2026, 6, 15),
            endsOn: new DateOnly(2026, 8, 15), today: new DateOnly(2026, 9, 20));
        await service.Suspend(payment.RecurringPaymentId);

        var preview = await service.PreviewEnd(payment.RecurringPaymentId);

        Assert.Equal("RecurringPayment.AlreadyEnded", preview.Error.Code);
    }

    [Fact]
    public async Task Refuse_to_preview_ending_a_missing_payment_and_propagate_repository_failure()
    {
        var payments = new FakeRecurringPaymentRepository();
        var expenses = new FakeExpenseRepository();
        var service = new RecurringPaymentService(new FakeCategoryRepository(), new FakeTagRepository(), expenses,
            payments, new FakeRecurringOccurrenceProcessor(expenses));

        var missing = await service.PreviewEnd(Guid.NewGuid());
        payments.Failure = InfrastructureErrors.Unexpected;
        var failed = await service.PreviewEnd(Guid.NewGuid());

        Assert.Equal("RecurringPayment.NotFound", missing.Error.Code);
        Assert.Equal("Infrastructure.Unexpected", failed.Error.Code);
    }

    private static async Task<(RecurringPaymentService Service, RecurringPayment Payment)> ServiceWithPayment(
        RecurrenceFrequency frequency, DateOnly nextDueOn, DateOnly? endsOn = null, DateOnly? today = null)
    {
        var (service, payment, _) = await ServiceWithPaymentAndExpenses(frequency, nextDueOn, endsOn, today);
        return (service, payment);
    }

    private static async Task<(RecurringPaymentService Service, RecurringPayment Payment, FakeExpenseRepository Expenses)>
        ServiceWithPaymentAndExpenses(RecurrenceFrequency frequency, DateOnly nextDueOn, DateOnly? endsOn = null,
            DateOnly? today = null)
    {
        var categories = new FakeCategoryRepository();
        var category = new Category(Guid.NewGuid(), "Svago", "leisure", "lilac");
        categories.Items.Add(category);
        var expenses = new FakeExpenseRepository();
        var service = new RecurringPaymentService(categories, new FakeTagRepository(), expenses,
            new FakeRecurringPaymentRepository(), new FakeRecurringOccurrenceProcessor(expenses),
            today.HasValue ? () => today.Value : null);
        var created = await service.Create(new(category.CategoryId, 1_599, frequency, nextDueOn, EndsOn: endsOn));
        return (service, created.Value, expenses);
    }
}
