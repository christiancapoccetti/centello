using Centello.Domain;

namespace Centello.Application;

public interface IRecurringOccurrenceProcessor
{
    Task<Result<bool>> Process(
        RecurringPayment advancedPayment,
        Expense expense,
        RecurrenceOccurrence occurrence,
        CancellationToken cancellationToken = default);
}

