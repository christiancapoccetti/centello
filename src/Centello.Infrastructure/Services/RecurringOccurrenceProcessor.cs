using Centello.Application;
using Centello.Domain;
using Microsoft.EntityFrameworkCore;

namespace Centello.Infrastructure;

public sealed class RecurringOccurrenceProcessor(CentelloDbContext context)
    : RepositoryBase(context), IRecurringOccurrenceProcessor
{
    public Task<Result<RecurringOccurrenceOutcome>> Process(Guid recurringPaymentId, DateOnly throughDate,
        CancellationToken cancellationToken = default) =>
        SaveContextInTransaction(async (db, token) =>
        {
            var payment = await db.RecurringPayments.SingleOrDefaultAsync(
                item => item.RecurringPaymentId == recurringPaymentId, token);
            if (payment is null || !payment.IsDueThrough(throughDate))
                return RecurringOccurrenceOutcome.NotDue;

            var dueOn = payment.NextDueOn;
            var exists = await db.RecurrenceOccurrences.AnyAsync(
                item => item.RecurringPaymentId == recurringPaymentId && item.DueOn == dueOn, token);
            if (!exists)
            {
                db.RecurrenceOccurrences.Add(new RecurrenceOccurrence(recurringPaymentId, dueOn));
                db.Expenses.Add(payment.CreateExpense(Guid.CreateVersion7()));
            }

            payment.MoveToNextOccurrence();
            return exists ? RecurringOccurrenceOutcome.AlreadyRecorded : RecurringOccurrenceOutcome.Created;
        }, cancellationToken, shouldSave: outcome => outcome != RecurringOccurrenceOutcome.NotDue);
}
