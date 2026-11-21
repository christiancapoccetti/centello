using Microsoft.Extensions.DependencyInjection;

namespace Centesimo.Application;

public sealed class RecapNoticeAutomation(IServiceScopeFactory scopeFactory, IRecapNoticeBoard board,
    IRecapNoticeSettings settings, Func<DateTime>? now = null)
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly IRecapNoticeBoard _board = board;
    private readonly IRecapNoticeSettings _settings = settings;
    private readonly Func<DateTime> _now = now ?? (() => DateTime.Now);

    /// <summary>On, or Undecided with the permission already granted. Off, or Undecided without the permission,
    /// disarms.</summary>
    public static bool IsEnabled(RecapNoticeState state, bool canPost) => throw new NotImplementedException();

    /// <summary>The Settings row detail, so the wording is tested.</summary>
    public static string Describe(RecapNoticeState state, bool canPost) => throw new NotImplementedException();

    /// <summary>Idempotent: derives everything from the clock, the settings and the data, so running it twice, or
    /// after any crash, converges. Safe from a receiver, from window activation and from Settings.</summary>
    public Task Sync() => throw new NotImplementedException();

    /// <summary>Writes the state, then syncs.</summary>
    public Task Choose(RecapNoticeState state) => throw new NotImplementedException();
}
