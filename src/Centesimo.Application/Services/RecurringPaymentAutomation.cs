using Microsoft.Extensions.DependencyInjection;

namespace Centesimo.Application;

public interface IRecurringPaymentReminder
{
    Task Resync(IReadOnlyList<UpcomingRecurringPaymentReminder> reminders);
}

public sealed class NoOpRecurringPaymentReminder : IRecurringPaymentReminder
{
    public Task Resync(IReadOnlyList<UpcomingRecurringPaymentReminder> reminders) => Task.CompletedTask;
}

/// <summary>The only background writer of the database. One gate serializes its passes with the work that must
/// own the database alone, such as a restore.</summary>
public sealed class RecurringPaymentAutomation(IServiceScopeFactory scopeFactory,
    IRecurringPaymentReminder reminder)
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task ProcessDue()
    {
        if (!await _gate.WaitAsync(0))
            return;

        try
        {
            await RunPass();
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Waits for the gate instead of dropping the call, then runs one pass before releasing it: calls
    /// skipped while the work held the gate are covered, and the pass sees the data the work left.</summary>
    public async Task<TResult> Exclusive<TResult>(Func<Task<TResult>> work)
    {
        await _gate.WaitAsync();
        try
        {
            var result = await work();
            await RunPass();
            return result;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task RunPass()
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var today = DateOnly.FromDateTime(DateTime.Today);
            var service = scope.ServiceProvider.GetRequiredService<RecurringPaymentService>();
            var processed = await service.ProcessDue(today);
            if (processed.IsFailure)
                return;

            var upcoming = scope.ServiceProvider.GetRequiredService<UpcomingRecurringPaymentService>();
            var reminders = await upcoming.GetUpcoming(today);
            if (reminders.IsFailure)
                return;

            await reminder.Resync(reminders.Value);
        }
        catch
        {
        }
    }
}
