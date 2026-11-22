using Centello.Application;
using Centello.Domain;
using Microsoft.EntityFrameworkCore;

namespace Centello.Infrastructure;

public sealed class RecurringOccurrenceProcessor(CentelloDbContext context)
    : RepositoryBase(context), IRecurringOccurrenceProcessor
{
    public Task<Result<bool>> Process(
        RecurringPayment advancedPayment,
        Expense expense,
        RecurrenceOccurrence occurrence,
        CancellationToken cancellationToken = default) =>
        SaveContextInTransaction(async (db, token) =>
        {
            var exists = await db.RecurrenceOccurrences.AnyAsync(
                item => item.RecurringPaymentId == occurrence.RecurringPaymentId
                    && item.DueOn == occurrence.DueOn, token);
            var tracked = await db.RecurringPayments.SingleAsync(
                payment => payment.RecurringPaymentId == advancedPayment.RecurringPaymentId, token);
            db.Entry(tracked).CurrentValues.SetValues(advancedPayment);

            if (!exists)
            {
                db.RecurrenceOccurrences.Add(occurrence);
                db.Expenses.Add(expense);
            }

            return !exists;
        }, cancellationToken);
}
