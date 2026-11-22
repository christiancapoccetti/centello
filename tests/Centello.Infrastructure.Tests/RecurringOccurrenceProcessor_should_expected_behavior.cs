using Centello.Application;
using Centello.Domain;
using Microsoft.EntityFrameworkCore;

namespace Centello.Infrastructure.Tests;

public sealed class RecurringOccurrenceProcessor_should_expected_behavior
{
    private static readonly DateOnly JanuaryDueOn = new(2026, 1, 31);
    private static readonly DateOnly FebruaryDueOn = new(2026, 2, 28);

    [Fact]
    public async Task Persist_occurrence_expense_and_advanced_payment_together()
    {
        using var database = new TestDatabase();
        var payment = await SeedPayment(database.Context);

        var created = await new RecurringOccurrenceProcessor(database.Context)
            .Process(payment.RecurringPaymentId, JanuaryDueOn);

        Assert.Equal(RecurringOccurrenceOutcome.Created, created.Value);
        Assert.Single(await database.Context.RecurrenceOccurrences.ToListAsync());
        var expense = await database.Context.Expenses.SingleAsync();
        Assert.Equal(payment.RecurringPaymentId, expense.RecurringPaymentId);
        Assert.Equal(FebruaryDueOn, (await database.Context.RecurringPayments.SingleAsync()).NextDueOn);
    }

    [Fact]
    public async Task Honor_a_pause_saved_after_the_due_scan()
    {
        using var database = new TestDatabase();
        var payment = await SeedPayment(database.Context);
        var editor = CreateService(database.Context);
        using var automationContext = database.CreateContext();
        var automation = CreateService(automationContext, async () =>
        {
            var paused = await editor.Suspend(payment.RecurringPaymentId);
            Assert.True(paused.IsSuccess);
        });

        var processed = await automation.ProcessDue(new DateOnly(2026, 3, 31));

        Assert.Equal(0, processed.Value);
        Assert.Empty(await database.Context.RecurrenceOccurrences.ToListAsync());
        Assert.Empty(await database.Context.Expenses.ToListAsync());
        var stored = await database.Context.RecurringPayments.SingleAsync();
        Assert.True(stored.IsSuspended);
        Assert.Equal(JanuaryDueOn, stored.NextDueOn);
    }

    [Fact]
    public async Task Catch_up_using_the_edited_schedule_and_details_after_the_due_scan()
    {
        using var database = new TestDatabase();
        var payment = await SeedPayment(database.Context);
        var editor = CreateService(database.Context);
        using var automationContext = database.CreateContext();
        var automation = CreateService(automationContext, async () =>
        {
            var edited = await editor.Update(payment.RecurringPaymentId, new SaveRecurringPaymentRequest(
                payment.CategoryId, 800, RecurrenceFrequency.Weekly, new DateOnly(2026, 2, 5),
                Note: "Nota aggiornata"));
            Assert.True(edited.IsSuccess);
        });
        var throughDate = new DateOnly(2026, 2, 20);

        var processed = await automation.ProcessDue(throughDate);
        var repeated = await automation.ProcessDue(throughDate);

        Assert.Equal(3, processed.Value);
        Assert.Equal(0, repeated.Value);
        var expenses = await database.Context.Expenses
            .OrderBy(expense => expense.OccurredOn)
            .ToListAsync();
        Assert.Equal([new DateOnly(2026, 2, 5), new DateOnly(2026, 2, 12), new DateOnly(2026, 2, 19)],
            expenses.Select(expense => expense.OccurredOn));
        Assert.All(expenses, expense =>
        {
            Assert.Equal(800L, expense.Amount.Cents);
            Assert.Equal("Nota aggiornata", expense.Note);
        });
        var stored = await database.Context.RecurringPayments.SingleAsync();
        Assert.Equal(RecurrenceFrequency.Weekly, stored.Frequency);
        Assert.Equal(new DateOnly(2026, 2, 26), stored.NextDueOn);
        Assert.Equal(3, await database.Context.RecurrenceOccurrences.CountAsync());
    }

    [Fact]
    public async Task Preserve_the_processed_date_when_the_editor_leaves_the_displayed_date_unchanged()
    {
        using var database = new TestDatabase();
        var payment = await SeedPayment(database.Context);
        var editor = CreateService(database.Context);
        using var automationContext = database.CreateContext();
        var automation = CreateService(automationContext);

        var displayed = (await editor.Get(payment.RecurringPaymentId)).Value!;
        Assert.Equal(1, (await automation.ProcessDue(JanuaryDueOn)).Value);
        var edited = await editor.Update(payment.RecurringPaymentId, new SaveRecurringPaymentRequest(
            displayed.CategoryId, displayed.Amount.Cents, displayed.Frequency, displayed.NextDueOn,
            Note: "Nota aggiornata", OriginalNextDueOn: displayed.NextDueOn));

        Assert.True(edited.IsSuccess);
        var stored = (await editor.Get(payment.RecurringPaymentId)).Value!;
        Assert.Equal(FebruaryDueOn, stored.NextDueOn);
        Assert.Equal("Nota aggiornata", stored.Note);
        Assert.Equal(0, (await automation.ProcessDue(JanuaryDueOn)).Value);
        Assert.Equal(1, (await automation.ProcessDue(FebruaryDueOn)).Value);
        var expenses = await database.Context.Expenses
            .OrderBy(expense => expense.OccurredOn)
            .ToListAsync();
        Assert.Equal([JanuaryDueOn, FebruaryDueOn], expenses.Select(expense => expense.OccurredOn));
        Assert.Equal(["Original", "Nota aggiornata"], expenses.Select(expense => expense.Note));
        Assert.Equal(new DateOnly(2026, 3, 31),
            (await editor.Get(payment.RecurringPaymentId)).Value!.NextDueOn);
    }

    [Fact]
    public async Task Reanchor_to_an_explicitly_edited_date_that_automation_already_reached()
    {
        using var database = new TestDatabase();
        var payment = await SeedPayment(database.Context);
        var editor = CreateService(database.Context);
        using var automationContext = database.CreateContext();
        var automation = CreateService(automationContext);

        var displayed = (await editor.Get(payment.RecurringPaymentId)).Value!;
        Assert.Equal(JanuaryDueOn, displayed.NextDueOn);
        Assert.Equal(1, (await automation.ProcessDue(JanuaryDueOn)).Value);
        var edited = await editor.Update(payment.RecurringPaymentId, new SaveRecurringPaymentRequest(
            displayed.CategoryId, displayed.Amount.Cents, displayed.Frequency, FebruaryDueOn,
            OriginalNextDueOn: displayed.NextDueOn));

        Assert.True(edited.IsSuccess);
        Assert.Equal(1, (await automation.ProcessDue(FebruaryDueOn)).Value);
        Assert.Equal(new DateOnly(2026, 3, 28),
            (await editor.Get(payment.RecurringPaymentId)).Value!.NextDueOn);
    }

    [Fact]
    public async Task Record_every_occurrence_once_when_two_passes_scan_the_same_payment()
    {
        using var database = new TestDatabase();
        var payment = await SeedPayment(database.Context);
        var scans = 0;
        var bothScanned = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task WaitForBothScans()
        {
            if (Interlocked.Increment(ref scans) == 2)
                bothScanned.SetResult();

            await bothScanned.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }

        using var firstContext = database.CreateContext();
        using var secondContext = database.CreateContext();
        var first = CreateService(firstContext, WaitForBothScans);
        var second = CreateService(secondContext, WaitForBothScans);

        var results = await Task.WhenAll(first.ProcessDue(FebruaryDueOn), second.ProcessDue(FebruaryDueOn));

        Assert.Equal(2, results.Sum(result => result.Value));
        Assert.Equal(2, await database.Context.RecurrenceOccurrences.CountAsync());
        Assert.Equal(2, await database.Context.Expenses.CountAsync());
        Assert.Equal(new DateOnly(2026, 3, 31),
            (await first.Get(payment.RecurringPaymentId)).Value!.NextDueOn);
    }

    [Fact]
    public async Task Continue_past_an_already_recorded_due_date_without_creating_a_duplicate()
    {
        using var database = new TestDatabase();
        var payment = await SeedPayment(database.Context);
        database.Context.Add(new RecurrenceOccurrence(payment.RecurringPaymentId, JanuaryDueOn));
        database.Context.Add(payment.CreateExpense(Guid.NewGuid()));
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();
        using var automationContext = database.CreateContext();
        var automation = CreateService(automationContext);

        var processed = await automation.ProcessDue(FebruaryDueOn);
        var repeated = await automation.ProcessDue(FebruaryDueOn);

        Assert.Equal(1, processed.Value);
        Assert.Equal(0, repeated.Value);
        var expenseDates = await database.Context.Expenses
            .OrderBy(expense => expense.OccurredOn)
            .Select(expense => expense.OccurredOn)
            .ToListAsync();
        Assert.Equal([JanuaryDueOn, FebruaryDueOn], expenseDates);
        Assert.Equal(2, await database.Context.RecurrenceOccurrences.CountAsync());
        Assert.Equal(new DateOnly(2026, 3, 31),
            (await database.Context.RecurringPayments.SingleAsync()).NextDueOn);
    }

    [Fact]
    public async Task Roll_back_all_changes_when_expense_cannot_be_saved()
    {
        using var database = new TestDatabase();
        var payment = await SeedPayment(database.Context);
        await database.Context.Database.ExecuteSqlRawAsync("""
            CREATE TRIGGER BlockExpenseInsert BEFORE INSERT ON Expenses
            BEGIN
                SELECT RAISE(ABORT, 'Test expense insert failure');
            END
            """);
        var processor = new RecurringOccurrenceProcessor(database.Context);

        var failed = await processor.Process(payment.RecurringPaymentId, JanuaryDueOn);

        Assert.Equal("Infrastructure.PersistenceFailure", failed.Error.Code);
        Assert.Empty(await database.Context.RecurrenceOccurrences.ToListAsync());
        Assert.Empty(await database.Context.Expenses.ToListAsync());
        var stored = await database.Context.RecurringPayments
            .AsNoTracking()
            .SingleAsync();
        Assert.Equal(JanuaryDueOn, stored.NextDueOn);

        await database.Context.Database.ExecuteSqlRawAsync("DROP TRIGGER BlockExpenseInsert");
        Assert.Equal(RecurringOccurrenceOutcome.Created,
            (await processor.Process(payment.RecurringPaymentId, JanuaryDueOn)).Value);
    }

    private static async Task<RecurringPayment> SeedPayment(CentelloDbContext context)
    {
        var category = new Category(Guid.NewGuid(), "Bills", "receipt", "#123456");
        var payment = new RecurringPayment(Guid.NewGuid(), category.CategoryId, new Money(100),
            RecurrenceFrequency.Monthly, JanuaryDueOn, note: "Original");
        context.AddRange(category, payment);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return payment;
    }

    private static RecurringPaymentService CreateService(CentelloDbContext context, Func<Task>? edit = null)
    {
        IRecurringOccurrenceProcessor processor = new RecurringOccurrenceProcessor(context);
        if (edit is not null)
            processor = new EditBeforeFirstOccurrence(processor, edit);

        return new RecurringPaymentService(new CategoryRepository(context), new TagRepository(context),
            new ExpenseRepository(context), new RecurringPaymentRepository(context), processor);
    }

    private sealed class EditBeforeFirstOccurrence(IRecurringOccurrenceProcessor processor, Func<Task> edit)
        : IRecurringOccurrenceProcessor
    {
        private bool _edited;

        public async Task<Result<RecurringOccurrenceOutcome>> Process(Guid recurringPaymentId,
            DateOnly throughDate, CancellationToken cancellationToken = default)
        {
            if (!_edited)
            {
                _edited = true;
                await edit();
            }

            return await processor.Process(recurringPaymentId, throughDate, cancellationToken);
        }
    }
}
