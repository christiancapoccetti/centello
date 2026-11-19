using Centesimo.Domain;

namespace Centesimo.Domain.Tests;

public sealed class RecurringPayment_should_expected_behavior
{
    [Fact]
    public void Preserve_month_end_anchor_across_short_months()
    {
        var payment = Create(RecurrenceFrequency.Monthly, new DateOnly(2026, 1, 31));

        payment.MoveToNextOccurrence();
        Assert.Equal(new DateOnly(2026, 2, 28), payment.NextDueOn);
        payment.MoveToNextOccurrence();

        Assert.Equal(new DateOnly(2026, 3, 31), payment.NextDueOn);
    }

    [Fact]
    public void Preserve_leap_day_anchor_across_non_leap_years()
    {
        var payment = Create(RecurrenceFrequency.Yearly, new DateOnly(2024, 2, 29));

        payment.MoveToNextOccurrence();
        payment.MoveToNextOccurrence();
        payment.MoveToNextOccurrence();
        payment.MoveToNextOccurrence();

        Assert.Equal(new DateOnly(2028, 2, 29), payment.NextDueOn);
    }

    [Fact]
    public void Report_what_it_is_doing_now()
    {
        var active = Create(RecurrenceFrequency.Monthly, new DateOnly(2026, 12, 5));
        var paused = Create(RecurrenceFrequency.Monthly, new DateOnly(2026, 12, 5));
        paused.Suspend();
        var resumed = Create(RecurrenceFrequency.Monthly, new DateOnly(2026, 12, 5));
        resumed.Suspend();
        resumed.Resume();
        var ended = Create(RecurrenceFrequency.Monthly, new DateOnly(2026, 12, 5));
        ended.End(new DateOnly(2026, 12, 5));
        ended.MoveToNextOccurrence();
        var pausedThenEnded = Create(RecurrenceFrequency.Monthly, new DateOnly(2026, 12, 5));
        pausedThenEnded.Suspend();
        pausedThenEnded.End(new DateOnly(2026, 12, 5));
        pausedThenEnded.MoveToNextOccurrence();

        Assert.Equal(RecurringPaymentStatus.Active, active.Status);
        Assert.Equal(RecurringPaymentStatus.Paused, paused.Status);
        Assert.Equal(RecurringPaymentStatus.Active, resumed.Status);
        Assert.Equal(RecurringPaymentStatus.Ended, ended.Status);
        Assert.Equal(RecurringPaymentStatus.Ended, pausedThenEnded.Status);
    }

    [Fact]
    public void Be_ended_only_once_the_next_occurrence_falls_after_the_end_date()
    {
        var open = Create(RecurrenceFrequency.Monthly, new DateOnly(2026, 12, 5));
        var lastOccurrencePending = Create(RecurrenceFrequency.Monthly, new DateOnly(2026, 12, 5));
        lastOccurrencePending.End(new DateOnly(2026, 12, 5));
        var ended = Create(RecurrenceFrequency.Monthly, new DateOnly(2026, 12, 5));
        ended.End(new DateOnly(2026, 12, 5));
        ended.MoveToNextOccurrence();

        Assert.False(open.IsEnded);
        Assert.False(lastOccurrencePending.IsEnded);
        Assert.True(ended.IsEnded);
    }

    private static RecurringPayment Create(RecurrenceFrequency frequency, DateOnly nextDueOn) =>
        new(Guid.NewGuid(), Guid.NewGuid(), new Money(100), frequency, nextDueOn);
}
