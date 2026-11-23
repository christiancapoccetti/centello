using Centello.Domain;

namespace Centello.Application.Tests;

public sealed class UpcomingRecurringPaymentService_should_expected_behavior
{
    private static readonly DateOnly Today = new(2026, 7, 20);

    [Fact]
    public async Task Schedule_the_next_active_payment_even_when_it_is_more_than_a_week_away()
    {
        var repository = new FakeRecurringPaymentRepository();
        var suspended = Payment(Today.AddDays(1));
        suspended.Suspend();
        var ended = Payment(Today.AddDays(2));
        ended.End(Today.AddDays(-1));
        repository.Items.AddRange([suspended, ended, Payment(Today.AddDays(32)), Payment(Today.AddDays(30))]);

        var result = await new UpcomingRecurringPaymentService(repository).GetSchedule(Today);

        Assert.Null(result.Value.Notice);
        Assert.Equal(Today.AddDays(30).ToDateTime(new TimeOnly(9, 0)), result.Value.NextDueAt);
    }

    [Fact]
    public async Task Keep_todays_reminder_when_processing_advanced_past_the_last_occurrence()
    {
        var repository = new FakeRecurringPaymentRepository();
        var payment = Payment(Today, "Netflix");
        payment.End(Today);
        payment.MoveToNextOccurrence();
        repository.Items.Add(payment);
        repository.RecordedOccurrences.Add(new RecurrenceOccurrence(payment.RecurringPaymentId, Today));
        var service = new UpcomingRecurringPaymentService(repository);

        var first = await service.GetSchedule(Today);
        var resync = await service.GetSchedule(Today);

        var notice = Assert.IsType<RecurringPaymentNotice>(first.Value.Notice);
        Assert.Equal(Today.ToDateTime(new TimeOnly(9, 0)), notice.NotifyAt);
        Assert.Equal("Pagamento ricorrente registrato", notice.Title);
        Assert.Equal("Netflix", notice.Text);
        Assert.Null(first.Value.NextDueAt);
        Assert.Equal(first.Value, resync.Value);
        Assert.Null((await service.GetSchedule(Today.AddDays(1))).Value.Notice);
    }

    [Fact]
    public async Task Count_each_recorded_payment_even_when_they_have_the_same_name()
    {
        var repository = new FakeRecurringPaymentRepository();
        foreach (var payment in new[] { Payment(Today, "Netflix"), Payment(Today, "Netflix") })
        {
            repository.Items.Add(payment);
            repository.RecordedOccurrences.Add(new RecurrenceOccurrence(payment.RecurringPaymentId, Today));
        }

        var result = await new UpcomingRecurringPaymentService(repository).GetSchedule(Today);

        var notice = Assert.IsType<RecurringPaymentNotice>(result.Value.Notice);
        Assert.Equal("Pagamenti ricorrenti registrati", notice.Title);
        Assert.Equal("2 pagamenti registrati oggi.", notice.Text);
    }

    private static RecurringPayment Payment(DateOnly nextDueOn, string name = "") =>
        new(Guid.NewGuid(), Guid.NewGuid(), new Money(1_200), RecurrenceFrequency.Monthly, nextDueOn, note: name);
}
