using Centello.Domain;

namespace Centello.Domain.Tests;

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

    [Theory]
    [InlineData(2026, 28)]
    [InlineData(2028, 29)]
    public void Preserve_month_end_anchor_when_only_the_note_changes(int year, int februaryLastDay)
    {
        var payment = Create(RecurrenceFrequency.Monthly, new DateOnly(year, 1, 31));
        payment.MoveToNextOccurrence();
        Assert.Equal(new DateOnly(year, 2, februaryLastDay), payment.NextDueOn);

        payment.Update(payment.CategoryId, payment.Amount, payment.Frequency, payment.NextDueOn,
            payment.TagId, "Nota aggiornata", payment.EndsOn);
        payment.MoveToNextOccurrence();

        Assert.Equal("Nota aggiornata", payment.Note);
        Assert.Equal(new DateOnly(year, 3, 31), payment.NextDueOn);
    }

    [Fact]
    public void Reset_the_month_end_anchor_when_the_due_date_changes()
    {
        var payment = Create(RecurrenceFrequency.Monthly, new DateOnly(2026, 1, 31));
        payment.MoveToNextOccurrence();

        payment.Update(payment.CategoryId, payment.Amount, payment.Frequency, new DateOnly(2026, 2, 24),
            payment.TagId, payment.Note, payment.EndsOn);
        payment.MoveToNextOccurrence();

        Assert.Equal(new DateOnly(2026, 3, 24), payment.NextDueOn);
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
        resumed.Resume(new DateOnly(2026, 12, 5));
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

    [Theory]
    [InlineData(RecurrenceFrequency.Monthly, "2026-06-15", "2026-09-10", "2026-09-15")]
    [InlineData(RecurrenceFrequency.Monthly, "2026-06-15", "2026-09-15", "2026-09-15")]
    [InlineData(RecurrenceFrequency.Monthly, "2026-06-15", "2026-09-20", "2026-10-15")]
    [InlineData(RecurrenceFrequency.Monthly, "2026-06-15", "2026-06-01", "2026-06-15")]
    [InlineData(RecurrenceFrequency.Weekly, "2026-06-01", "2026-06-23", "2026-06-29")]
    [InlineData(RecurrenceFrequency.Weekly, "2026-06-01", "2026-06-22", "2026-06-22")]
    [InlineData(RecurrenceFrequency.Yearly, "2024-03-10", "2026-05-01", "2027-03-10")]
    [InlineData(RecurrenceFrequency.Yearly, "2024-03-10", "2026-03-10", "2026-03-10")]
    public void Skip_the_occurrences_missed_while_paused_when_resumed(
        RecurrenceFrequency frequency, string dueOn, string today, string expected)
    {
        var payment = Create(frequency, DateOnly.Parse(dueOn));
        payment.Suspend();

        payment.Resume(DateOnly.Parse(today));

        Assert.False(payment.IsSuspended);
        Assert.Equal(DateOnly.Parse(expected), payment.NextDueOn);
    }

    [Fact]
    public void Keep_the_month_end_anchor_while_skipping_missed_occurrences()
    {
        var payment = Create(RecurrenceFrequency.Monthly, new DateOnly(2026, 1, 31));
        payment.Suspend();

        payment.Resume(new DateOnly(2026, 3, 1));

        Assert.Equal(new DateOnly(2026, 3, 31), payment.NextDueOn);
    }

    [Fact]
    public void End_when_the_end_date_passes_while_paused()
    {
        var payment = Create(RecurrenceFrequency.Monthly, new DateOnly(2026, 6, 15), endsOn: new DateOnly(2026, 8, 15));
        payment.Suspend();

        payment.Resume(new DateOnly(2026, 9, 20));

        Assert.Equal(RecurringPaymentStatus.Ended, payment.Status);
    }

    [Fact]
    public void Keep_the_last_occurrence_due_when_resumed_on_the_end_date()
    {
        var payment = Create(RecurrenceFrequency.Monthly, new DateOnly(2026, 6, 15), endsOn: new DateOnly(2026, 9, 15));
        payment.Suspend();

        payment.Resume(new DateOnly(2026, 9, 15));

        Assert.False(payment.IsEnded);
        Assert.Equal(new DateOnly(2026, 9, 15), payment.NextDueOn);
    }

    [Fact]
    public void Name_the_first_occurrence_on_or_after_today_without_moving_the_schedule()
    {
        var payment = Create(RecurrenceFrequency.Monthly, new DateOnly(2026, 10, 5));

        Assert.Equal(new DateOnly(2026, 12, 5), payment.UpcomingDueOn(new DateOnly(2026, 11, 20)));
        Assert.Equal(new DateOnly(2026, 10, 5), payment.UpcomingDueOn(new DateOnly(2026, 10, 5)));
        Assert.Equal(new DateOnly(2026, 10, 5), payment.NextDueOn);
    }

    [Fact]
    public void Leave_the_schedule_of_an_active_payment_untouched_when_resumed()
    {
        var payment = Create(RecurrenceFrequency.Monthly, new DateOnly(2026, 6, 15));

        payment.Resume(new DateOnly(2026, 9, 20));

        Assert.Equal(new DateOnly(2026, 6, 15), payment.NextDueOn);
    }

    private static RecurringPayment Create(RecurrenceFrequency frequency, DateOnly nextDueOn, DateOnly? endsOn = null) =>
        new(Guid.NewGuid(), Guid.NewGuid(), new Money(100), frequency, nextDueOn, endsOn: endsOn);
}
