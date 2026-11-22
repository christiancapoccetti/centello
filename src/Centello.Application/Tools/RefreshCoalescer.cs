namespace Centello.Application;

/// <summary>Runs <paramref name="work"/> one run at a time. A request made while a run is going queues exactly one
/// more, which starts after it; requests made before that one starts share it. So a burst of requests costs at most
/// two runs, and a run that starts after a request always sees what the requester had already done.</summary>
public sealed class RefreshCoalescer(Func<Task> work)
{
    private readonly Lock _gate = new();
    private Task _tail = Task.CompletedTask;
    private TaskCompletionSource? _queued;

    /// <summary>Completes after a run that started after this call. A failed run fails the requests it served.</summary>
    public Task Request()
    {
        lock (_gate)
        {
            if (_queued is { } waiting)
                return waiting.Task;

            var run = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _queued = run;
            _tail = _tail.ContinueWith(_ => Run(run), TaskScheduler.Default).Unwrap();
            return run.Task;
        }
    }

    /// <summary>Completes when no run waits or goes.</summary>
    public async Task Settled()
    {
        while (true)
        {
            Task tail;
            lock (_gate)
                tail = _tail;

            await tail;
            lock (_gate)
                if (ReferenceEquals(tail, _tail))
                    return;
        }
    }

    private async Task Run(TaskCompletionSource run)
    {
        lock (_gate)
            _queued = null;

        try
        {
            await work();
            run.SetResult();
        }
        catch (Exception exception)
        {
            run.SetException(exception);
        }
    }
}
