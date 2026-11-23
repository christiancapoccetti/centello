using Centello.Domain;

namespace Centello.Application;

public sealed record RecurringPaymentNotice(DateTime NotifyAt, string Title, string Text);

public sealed record RecurringPaymentReminderSchedule(RecurringPaymentNotice? Notice, DateTime? NextDueAt);

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
        var recorded = await recurringPayments.GetRecordedPaymentLabels(today, cancellationToken);
        if (recorded.IsFailure)
            return Result<RecurringPaymentReminderSchedule>.Failure(recorded.Error);

        var notifyAt = today.ToDateTime(ReminderTime);
        var notice = recorded.Value.Count switch
        {
            0 => null,
            1 => new RecurringPaymentNotice(notifyAt, "Pagamento ricorrente registrato", recorded.Value[0].Name),
            var count => new RecurringPaymentNotice(notifyAt, "Pagamenti ricorrenti registrati",
                $"{count} pagamenti registrati oggi.")
        };
        var next = payments.Value
            .Where(payment => payment.Status == RecurringPaymentStatus.Active && payment.NextDueOn > today)
            .Select(payment => (DateOnly?)payment.NextDueOn)
            .Min();
        return Result<RecurringPaymentReminderSchedule>.Success(new(notice, next?.ToDateTime(ReminderTime)));
    }
}
