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
    public static bool IsEnabled(RecapNoticeState state, bool canPost) =>
        state == RecapNoticeState.On || (state == RecapNoticeState.Undecided && canPost);

    /// <summary>The Settings row detail, so the wording is tested.</summary>
    public static string Describe(RecapNoticeState state, bool canPost)
    {
        if (state == RecapNoticeState.On && !canPost)
            return "Consenti le notifiche a Centesimo nelle impostazioni di Android";

        return IsEnabled(state, canPost) ? "Il 1° del mese, alle 9" : "Spento";
    }

    /// <summary>Idempotent: derives everything from the clock, the settings and the data, so running it twice, or
    /// after any crash, converges. Safe from a receiver, from window activation and from Settings.</summary>
    public async Task Sync()
    {
        var now = _now();
        if (!IsEnabled(_settings.State, _board.CanPost))
        {
            await _board.Disarm();
            return;
        }

        if (RecapSchedule.Due(now) is { } closed && _settings.LastHandledKey != RecapSchedule.KeyOf(closed))
            await Announce(closed);

        await _board.ArmAt(RecapSchedule.NextArm(now));
    }

    /// <summary>Writes the state, then syncs.</summary>
    public Task Choose(RecapNoticeState state)
    {
        _settings.State = state;
        return Sync();
    }

    private async Task Announce(Period closed)
    {
        using var scope = _scopeFactory.CreateScope();
        var notice = await NoticeFor(scope.ServiceProvider.GetRequiredService<IRecapService>(), closed);
        if (notice.IsFailure)
            return;

        if (notice.Value is { } due && !await _board.Post(due))
            return;

        _settings.LastHandledKey = RecapSchedule.KeyOf(closed);
    }

    // Null value: nothing to say. A closed year with too little data falls back to its December, so 1 January is
    // still one notice and one key.
    private static async Task<Result<RecapNotice?>> NoticeFor(IRecapService recaps, Period closed)
    {
        var available = await recaps.IsAvailable(closed);
        if (available.IsFailure)
            return Result<RecapNotice?>.Failure(available.Error);

        if (available.Value)
            return Result<RecapNotice?>.Success(RecapSchedule.NoticeFor(closed));

        if (closed.Kind == PeriodKind.Month)
            return Result<RecapNotice?>.Success(null);

        var december = Period.Month(closed.Start.Year, 12);
        var decemberAvailable = await recaps.IsAvailable(december);
        if (decemberAvailable.IsFailure)
            return Result<RecapNotice?>.Failure(decemberAvailable.Error);

        return Result<RecapNotice?>.Success(decemberAvailable.Value ? RecapSchedule.NoticeFor(december) : null);
    }
}
