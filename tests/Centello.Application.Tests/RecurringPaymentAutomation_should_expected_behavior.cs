using Centello.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace Centello.Application.Tests;

public sealed class RecurringPaymentAutomation_should_expected_behavior
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Today);

    private readonly FakeExpenseRepository _expenses = new();
    private readonly FakeRecurringPaymentRepository _payments = new();
    private readonly GatedReminder _reminder = new();
    private readonly RecurringPaymentAutomation _automation;

    public RecurringPaymentAutomation_should_expected_behavior()
    {
        var provider = new FakeServiceProvider(
            new RecurringPaymentService(new FakeCategoryRepository(), new FakeTagRepository(), _expenses, _payments,
                new FakeRecurringOccurrenceProcessor(_expenses)),
            new UpcomingRecurringPaymentService(_payments));
        _automation = new RecurringPaymentAutomation(new FakeScopeFactory(provider), _reminder);
    }

    private void AddPaymentDueSince(int daysAgo) =>
        _payments.Items.Add(new RecurringPayment(Guid.NewGuid(), Guid.NewGuid(), new Money(1_000),
            RecurrenceFrequency.Weekly, Today.AddDays(-daysAgo)));

    [Fact]
    public async Task Create_the_due_expenses_and_resync_the_reminders()
    {
        AddPaymentDueSince(7);

        await _automation.ProcessDue();

        Assert.Equal(2, _expenses.Items.Count);
        var resynced = Assert.Single(_reminder.Resynced);
        Assert.Single(resynced);
    }

    [Fact]
    public async Task Skip_a_call_made_while_a_pass_is_running()
    {
        AddPaymentDueSince(7);
        _reminder.Hold();
        var running = _automation.ProcessDue();
        await _reminder.Entered;

        await _automation.ProcessDue();
        _reminder.Release();
        await running;

        Assert.Single(_reminder.Resynced);
    }

    [Fact]
    public async Task Wait_for_a_running_pass_before_the_exclusive_work()
    {
        _reminder.Hold();
        var running = _automation.ProcessDue();
        await _reminder.Entered;
        var workStarted = false;

        var exclusive = _automation.Exclusive(() =>
        {
            workStarted = true;
            return Task.FromResult(1);
        });
        await Task.Delay(50);

        Assert.False(workStarted);
        _reminder.Release();
        await running;
        Assert.Equal(1, await exclusive);
        Assert.True(workStarted);
    }

    [Fact]
    public async Task Run_a_pass_after_the_exclusive_work_even_when_calls_were_skipped_meanwhile()
    {
        var result = await _automation.Exclusive(async () =>
        {
            AddPaymentDueSince(7);
            await _automation.ProcessDue();
            return _expenses.Items.Count;
        });

        Assert.Equal(0, result);
        Assert.Equal(2, _expenses.Items.Count);
        Assert.Single(_reminder.Resynced);
    }

    [Fact]
    public async Task Return_the_work_result_when_the_pass_fails()
    {
        _reminder.FailOnResync = true;
        AddPaymentDueSince(7);

        var result = await _automation.Exclusive(() => Task.FromResult("done"));

        Assert.Equal("done", result);
    }

    [Fact]
    public async Task Release_the_gate_when_the_exclusive_work_throws()
    {
        AddPaymentDueSince(7);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _automation.Exclusive<int>(() => throw new InvalidOperationException()));
        await _automation.ProcessDue();

        Assert.Equal(2, _expenses.Items.Count);
    }

    private sealed class GatedReminder : IRecurringPaymentReminder
    {
        private TaskCompletionSource? _release;
        private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public List<IReadOnlyList<UpcomingRecurringPaymentReminder>> Resynced { get; } = [];
        public bool FailOnResync { get; set; }
        public Task Entered => _entered.Task;

        public void Hold() => _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Release() => _release!.SetResult();

        public async Task Resync(IReadOnlyList<UpcomingRecurringPaymentReminder> reminders)
        {
            if (FailOnResync)
                throw new InvalidOperationException();

            Resynced.Add(reminders);
            _entered.TrySetResult();
            if (_release is not null)
                await _release.Task;
        }
    }
}
