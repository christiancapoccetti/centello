using Centello.Domain;

namespace Centello.Application;

public interface IRecurringPaymentRepository
{
    Task<Result> Add(RecurringPayment payment, CancellationToken cancellationToken = default);
    Task<Result<RecurringPayment?>> Get(Guid recurringPaymentId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<RecurringPayment>>> GetAll(CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<RecurringPayment>>> GetDue(DateOnly throughDate,
        CancellationToken cancellationToken = default);
    Task<Result<bool>> HasRecordedOccurrenceOn(DateOnly dueOn, CancellationToken cancellationToken = default);
    Task<Result> Update(Guid recurringPaymentId, Func<RecurringPayment, Result> update,
        CancellationToken cancellationToken = default);
}


