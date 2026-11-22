using Centello.Domain;

namespace Centello.Application;

public sealed record RecurringPaymentReminderSchedule(DateTime? NotifyAt, DateTime? NextDueAt);

public sealed class UpcomingRecurringPaymentService(IRecurringPaymentRepository recurringPayments)
{
    private static readonly TimeOnly ReminderTime = new(9, 0);

    public async Task<Result<RecurringPaymentReminderSchedule>> GetSchedule(DateOnly today,
        CancellationToken cancellationToken = default)
    {
        var payments = await recurringPayments.GetAll(cancellationToken);
        if (payments.IsFailure)
            return Result<RecurringPaymentReminderSchedule>.Failure(payments.Error);

        // The due pass has advanced the payment, so its recorded occurrence is the source for today's reminder.
        var dueToday = await recurringPayments.HasRecordedOccurrenceOn(today, cancellationToken);
        if (dueToday.IsFailure)
            return Result<RecurringPaymentReminderSchedule>.Failure(dueToday.Error);

        var next = payments.Value
            .Where(payment => payment.Status == RecurringPaymentStatus.Active && payment.NextDueOn > today)
            .Select(payment => (DateOnly?)payment.NextDueOn)
            .Min();
        return Result<RecurringPaymentReminderSchedule>.Success(new(
            dueToday.Value ? today.ToDateTime(ReminderTime) : null,
            next?.ToDateTime(ReminderTime)));
    }
}
