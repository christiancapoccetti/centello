using Centello.Domain;

namespace Centello.Application;

public sealed record RecordedPaymentLabel(string Note, string CategoryName)
{
    public string Name => RowLabels.Title(Note, CategoryName).Title;
}

public interface IRecurringPaymentRepository
{
    Task<Result> Add(RecurringPayment payment, CancellationToken cancellationToken = default);
    Task<Result<RecurringPayment?>> Get(Guid recurringPaymentId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<RecurringPayment>>> GetAll(CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<RecurringPayment>>> GetDue(DateOnly throughDate,
        CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<RecordedPaymentLabel>>> GetRecordedPaymentLabels(DateOnly dueOn,
        CancellationToken cancellationToken = default);
    Task<Result> Update(Guid recurringPaymentId, Func<RecurringPayment, Result> update,
        CancellationToken cancellationToken = default);
}


