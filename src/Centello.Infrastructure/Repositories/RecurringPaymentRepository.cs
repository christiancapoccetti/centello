using Centello.Application;
using Centello.Domain;
using Microsoft.EntityFrameworkCore;

namespace Centello.Infrastructure;

public sealed class RecurringPaymentRepository(CentelloDbContext context)
    : RepositoryBase(context), IRecurringPaymentRepository
{
    public Task<Result> Add(RecurringPayment payment, CancellationToken cancellationToken = default) =>
        SaveContext(db => db.RecurringPayments.Add(payment), cancellationToken);

    public Task<Result<RecurringPayment?>> Get(Guid recurringPaymentId,
        CancellationToken cancellationToken = default) =>
        UseContext((db, token) => db.RecurringPayments.AsNoTracking()
            .SingleOrDefaultAsync(payment => payment.RecurringPaymentId == recurringPaymentId, token),
            cancellationToken);

    public Task<Result<IReadOnlyList<RecurringPayment>>> GetAll(
        CancellationToken cancellationToken = default) =>
        UseContext<IReadOnlyList<RecurringPayment>>(async (db, token) =>
            await db.RecurringPayments.AsNoTracking()
                .OrderBy(payment => payment.NextDueOn)
                .ToListAsync(token), cancellationToken);

    public Task<Result<IReadOnlyList<RecurringPayment>>> GetDue(DateOnly throughDate,
        CancellationToken cancellationToken = default) =>
        UseContext<IReadOnlyList<RecurringPayment>>(async (db, token) =>
            await db.RecurringPayments.AsNoTracking()
                .Where(payment => !payment.IsSuspended
                    && payment.NextDueOn <= throughDate
                    && (!payment.EndsOn.HasValue || payment.NextDueOn <= payment.EndsOn.Value))
                .ToListAsync(token), cancellationToken);

    public Task<Result<IReadOnlyList<RecordedPaymentLabel>>> GetRecordedPaymentLabels(DateOnly dueOn,
        CancellationToken cancellationToken = default) =>
        UseContext<IReadOnlyList<RecordedPaymentLabel>>(async (db, token) =>
        {
            var labels = from occurrence in db.RecurrenceOccurrences.AsNoTracking()
                         join payment in db.RecurringPayments on occurrence.RecurringPaymentId equals payment.RecurringPaymentId
                         join category in db.Categories on payment.CategoryId equals category.CategoryId
                         where occurrence.DueOn == dueOn && !payment.IsSuspended
                         select new RecordedPaymentLabel(payment.Note, category.Name);
            return await labels.ToListAsync(token);
        }, cancellationToken);

    public async Task<Result> Update(Guid recurringPaymentId, Func<RecurringPayment, Result> update,
        CancellationToken cancellationToken = default)
    {
        var saved = await SaveContextInTransaction(async (db, token) =>
        {
            var tracked = await db.RecurringPayments.SingleOrDefaultAsync(
                payment => payment.RecurringPaymentId == recurringPaymentId, token);
            return tracked is null
                ? Result.Failure(ApplicationErrors.RecurringPaymentNotFound)
                : update(tracked);
        }, cancellationToken, shouldSave: result => result.IsSuccess);

        return saved.IsSuccess ? saved.Value : Result.Failure(saved.Error);
    }
}
