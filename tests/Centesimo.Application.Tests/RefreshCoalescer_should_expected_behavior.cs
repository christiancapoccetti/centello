namespace Centesimo.Application.Tests;

public sealed class RefreshCoalescer_should_expected_behavior
{
    private sealed class Gate
    {
        private readonly List<TaskCompletionSource> _releases = [];
        private readonly List<TaskCompletionSource> _starts = [];
        private int _running;

        public int Runs { get; private set; }
        public int MostAtOnce { get; private set; }

        public async Task Work()
        {
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource start;
            lock (_releases)
            {
                Runs++;
                MostAtOnce = Math.Max(MostAtOnce, ++_running);
                _releases.Add(release);
                start = StartOf(Runs);
            }

            start.TrySetResult();
            await release.Task;
            lock (_releases)
                _running--;
        }

        public Task Started(int run)
        {
            lock (_releases)
                return StartOf(run).Task;
        }

        public void Release(int run)
        {
            lock (_releases)
                _releases[run - 1].SetResult();
        }

        private TaskCompletionSource StartOf(int run)
        {
            while (_starts.Count < run)
                _starts.Add(new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));

            return _starts[run - 1];
        }
    }

    private static Task Within(Task task) => task.WaitAsync(TimeSpan.FromSeconds(5));

    [Fact]
    public async Task Should_complete_a_request_after_a_run()
    {
        var runs = 0;
        var coalescer = new RefreshCoalescer(() =>
        {
            runs++;
            return Task.CompletedTask;
        });

        await Within(coalescer.Request());

        Assert.Equal(1, runs);
    }

    [Fact]
    public async Task Should_fold_the_requests_made_during_a_run_into_one_more_run()
    {
        var gate = new Gate();
        var coalescer = new RefreshCoalescer(gate.Work);
        var first = coalescer.Request();
        await Within(gate.Started(1));

        var second = coalescer.Request();
        var third = coalescer.Request();
        var fourth = coalescer.Request();

        Assert.Same(second, third);
        Assert.Same(third, fourth);
        gate.Release(1);
        await Within(first);
        await Within(gate.Started(2));
        gate.Release(2);
        await Within(Task.WhenAll(second, third, fourth));
        await Within(coalescer.Settled());
        Assert.Equal(2, gate.Runs);
    }

    [Fact]
    public async Task Should_not_complete_a_request_made_during_a_run_before_the_run_that_follows_it()
    {
        var gate = new Gate();
        var coalescer = new RefreshCoalescer(gate.Work);
        var first = coalescer.Request();
        await Within(gate.Started(1));
        var second = coalescer.Request();

        gate.Release(1);
        await Within(first);
        await Within(gate.Started(2));

        Assert.False(second.IsCompleted);
        gate.Release(2);
        await Within(second);
    }

    [Fact]
    public async Task Should_never_run_two_at_once()
    {
        var gate = new Gate();
        var coalescer = new RefreshCoalescer(gate.Work);
        var first = coalescer.Request();
        await Within(gate.Started(1));
        _ = coalescer.Request();
        gate.Release(1);
        await Within(gate.Started(2));
        gate.Release(2);

        await Within(coalescer.Settled());
        await Within(first);

        Assert.Equal(1, gate.MostAtOnce);
    }

    [Fact]
    public async Task Should_settle_only_when_the_queued_run_is_done()
    {
        var gate = new Gate();
        var coalescer = new RefreshCoalescer(gate.Work);
        _ = coalescer.Request();
        await Within(gate.Started(1));
        _ = coalescer.Request();
        var settled = coalescer.Settled();

        gate.Release(1);
        await Within(gate.Started(2));

        Assert.False(settled.IsCompleted);
        gate.Release(2);
        await Within(settled);
    }

    [Fact]
    public async Task Should_settle_at_once_when_nothing_was_requested() =>
        await Within(new RefreshCoalescer(() => Task.CompletedTask).Settled());

    [Fact]
    public async Task Should_fail_the_request_of_a_failed_run_and_serve_the_next()
    {
        var attempts = 0;
        var coalescer = new RefreshCoalescer(() =>
            ++attempts == 1 ? Task.FromException(new InvalidOperationException("boom")) : Task.CompletedTask);

        await Assert.ThrowsAsync<InvalidOperationException>(() => Within(coalescer.Request()));
        await Within(coalescer.Request());
        await Within(coalescer.Settled());

        Assert.Equal(2, attempts);
    }
}
