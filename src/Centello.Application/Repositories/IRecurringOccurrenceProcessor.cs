namespace Centello.Application;

public enum RecurringOccurrenceOutcome
{
    NotDue,
    AlreadyRecorded,
    Created
}

public interface IRecurringOccurrenceProcessor
{
    Task<Result<RecurringOccurrenceOutcome>> Process(Guid recurringPaymentId, DateOnly throughDate,
        CancellationToken cancellationToken = default);
}
