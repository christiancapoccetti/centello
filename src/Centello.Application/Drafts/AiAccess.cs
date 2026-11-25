using System.Collections.Frozen;
using Centello.Domain;

namespace Centello.Application;

public interface IAiAccessStore
{
    string InstallationId { get; }
    AiAccessState Read();
    void Write(AiAccessState state);
}

public sealed record AiAccessState(int Used, string Invitation);

public interface IAiRemoteConfig
{
    Task<AiRemoteSettings?> Fetch(bool force, CancellationToken cancellationToken);
    void SetRealtimeUpdates(bool enabled);
    event Action<AiRemoteSettings?>? Updated;
}

public sealed record AiRemoteSettings(string SurveyUrl, IReadOnlySet<string> RevokedInviteIds);

public interface IAiInviteVerifier
{
    Result<string> Verify(string invitation, string installationId);
}

public sealed class AiAccess
{
    private const int TrialLimit = 5;
    private static readonly TimeSpan SettingsLifetime = TimeSpan.FromHours(12);
    private readonly Lock _stateLock = new();
    private readonly SemaphoreSlim _draftLock = new(1, 1);
    private readonly IAiAccessStore _store;
    private readonly IAiRemoteConfig _remote;
    private readonly IAiInviteVerifier _verifier;
    private readonly TimeProvider _clock;
    private AiRemoteSettings? _settings;
    private string _surveyUrl = "";
    private long? _verifiedAt;
    private FetchRequest? _pendingFetch;
    private long _realtimeRevision;
    private bool _wasInvitationRevoked;
    private bool _listeningForUpdates;
    private bool _changePending;

    public event Action? Changed;

    public AiAccess(IAiAccessStore store, IAiRemoteConfig remote, IAiInviteVerifier verifier, TimeProvider clock)
    {
        _store = store;
        _remote = remote;
        _verifier = verifier;
        _clock = clock;
        _remote.Updated += ApplyRealtimeSettings;
    }

    public string InstallationId
    {
        get
        {
            try
            {
                return _store.InstallationId;
            }
            catch (IOException)
            {
                return "";
            }
        }
    }

    public bool HasInvitation
    {
        get
        {
            lock (_stateLock)
                return ReadState() is { } state && state.Invitation.HasValue();
        }
    }

    public bool IsInvitationVerified
    {
        get
        {
            lock (_stateLock)
            {
                return HasFreshSettings() && _settings is { } settings
                    && ReadState() is { } state && state.Invitation.HasValue()
                    && Verify(state.Invitation) is { IsSuccess: true } verification
                    && !settings.RevokedInviteIds.Contains(verification.Value);
            }
        }
    }

    public int TrialsRemaining
    {
        get
        {
            lock (_stateLock)
                return ReadState() is { } state ? TrialLimit - state.Used : 0;
        }
    }

    public bool WasInvitationRevoked
    {
        get
        {
            lock (_stateLock)
                return _wasInvitationRevoked;
        }
    }

    public string SurveyUrl
    {
        get
        {
            lock (_stateLock)
                return _surveyUrl;
        }
    }

    public Task BeginSession()
    {
        lock (_stateLock)
        {
            SetVerification(null);
            // A response requested before this foreground session cannot authorize access in it.
            _pendingFetch = null;
            var force = ReadState() is { } state && state.Invitation.HasValue();
            return RefreshSettings(force);
        }
    }

    public async Task<Result> Check(CancellationToken cancellationToken = default) => await Authorize(cancellationToken);

    public async Task<Result> Redeem(string invitation, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        invitation = invitation.Trim();
        var verification = Verify(invitation);
        if (verification.IsFailure)
            return verification;

        Task refresh;
        lock (_stateLock)
            refresh = RefreshSettings(force: true);

        await refresh.WaitAsync(cancellationToken);

        lock (_stateLock)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!HasFreshSettings() || _settings is not { } settings)
                return Result.Failure(DraftErrors.InviteVerificationUnavailable);

            if (settings.RevokedInviteIds.Contains(verification.Value))
                return Result.Failure(DraftErrors.InviteRevoked);

            var state = ReadState();
            if (state is null || !WriteState(state with { Invitation = invitation }))
                return Result.Failure(DraftErrors.InviteVerificationUnavailable);

            _wasInvitationRevoked = false;
            ListenForUpdates(true);
            return Result.Success();
        }
    }

    internal async Task<Result<ExpenseDraft>> Prepare(
        Func<Task<Result<ExpenseDraft>>> interpret, CancellationToken cancellationToken)
    {
        // Hold the last trial use until its draft result has been persisted or discarded.
        await _draftLock.WaitAsync(cancellationToken);
        try
        {
            var grant = await Authorize(cancellationToken);
            if (grant.IsFailure)
                return Result<ExpenseDraft>.Failure(grant.Error);

            Task<Result<ExpenseDraft>> pending;
            lock (_stateLock)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (grant.Value.HasValue())
                {
                    var status = CheckInvitedDraft(grant.Value);
                    if (status.IsFailure)
                        return Result<ExpenseDraft>.Failure(status.Error);
                }

                pending = interpret();
            }

            var result = await pending;
            if (result.IsFailure)
                return result;

            lock (_stateLock)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (grant.Value.HasValue())
                {
                    var status = CheckInvitedDraft(grant.Value);
                    return status.IsSuccess ? result : Result<ExpenseDraft>.Failure(status.Error);
                }

                if (!HasUsableFields(result.Value))
                    return result;

                return ReadState() is { Used: < TrialLimit } state && WriteState(state with { Used = state.Used + 1 })
                    ? result
                    : Result<ExpenseDraft>.Failure(DraftErrors.Unavailable);
            }
        }
        finally
        {
            _draftLock.Release();
        }
    }

    private async Task<Result<string>> Authorize(CancellationToken cancellationToken)
    {
        Task? settled = null;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Task refresh;
            lock (_stateLock)
            {
                var state = ReadState();
                if (state is null)
                    return Result<string>.Failure(DraftErrors.Unavailable);

                if (state.Invitation.IsEmpty())
                    return TrialGrant(state);

                if (HasFreshSettings())
                    return InviteGrant(state);

                if (settled is not null && _pendingFetch is not { Force: true })
                    return Result<string>.Failure(DraftErrors.InviteVerificationUnavailable);

                refresh = RefreshSettings(force: true);
            }

            await refresh.WaitAsync(cancellationToken);
            settled = refresh;
        }
    }

    private Result<string> InviteGrant(AiAccessState state)
    {
        var verified = Verify(state.Invitation);
        if (verified.IsFailure)
            return verified;

        if (_settings!.RevokedInviteIds.Contains(verified.Value))
            return ClearInvitation(state)
                ? TrialGrant(state)
                : Result<string>.Failure(DraftErrors.InviteVerificationUnavailable);

        return verified;
    }

    private static Result<string> TrialGrant(AiAccessState state) =>
        state.Used < TrialLimit ? Result<string>.Success("") : Result<string>.Failure(DraftErrors.TrialExhausted);

    private Result CheckInvitedDraft(string inviteId)
    {
        if (_settings?.RevokedInviteIds.Contains(inviteId) == true)
            return Result.Failure(DraftErrors.InviteRevoked);

        if (!HasFreshSettings() || ReadState() is not { } state || state.Invitation.IsEmpty())
            return Result.Failure(DraftErrors.InviteVerificationUnavailable);

        return Verify(state.Invitation) is { IsSuccess: true } verification && verification.Value == inviteId
            ? Result.Success()
            : Result.Failure(DraftErrors.InviteVerificationUnavailable);
    }

    private static bool HasUsableFields(ExpenseDraft draft) =>
        draft.AmountCents.HasValue || draft.CategoryId.HasValue || draft.OccurredOn.HasValue || draft.Note.HasValue();

    private Result<string> Verify(string invitation)
    {
        var installationId = InstallationId;
        return installationId.HasValue()
            ? _verifier.Verify(invitation, installationId)
            : Result<string>.Failure(DraftErrors.InviteVerificationUnavailable);
    }

    private bool HasFreshSettings() =>
        _verifiedAt is { } at && _clock.GetElapsedTime(at) < SettingsLifetime;

    // Call with _stateLock held, including when publishing a newly started request.
    private Task RefreshSettings(bool force)
    {
        if (_pendingFetch is { } pending && (!force || pending.Force))
            return pending.Completion.Task;

        if (force)
            SetVerification(null);

        var request = new FetchRequest(force, _realtimeRevision);
        _pendingFetch = request;
        _ = CompleteFetch(request);
        return request.Completion.Task;
    }

    private async Task CompleteFetch(FetchRequest request)
    {
        AiRemoteSettings? settings;
        try
        {
            settings = ValidateSettings(await _remote.Fetch(request.Force, CancellationToken.None).ConfigureAwait(false));
        }
        catch (Exception)
        {
            settings = null;
        }

        lock (_stateLock)
        {
            if (ReferenceEquals(_pendingFetch, request))
            {
                _pendingFetch = null;
                // A fetch in flight may finish after a newer realtime update.
                if (request.RealtimeRevision == _realtimeRevision)
                    SetSettings(settings);

                if (request.Force && settings is not null && _settings is { } current)
                {
                    if (request.RealtimeDenials.Count != 0 || request.RealtimeRevision != _realtimeRevision)
                    {
                        // An overlapping response may have observed a revocation absent from the other snapshot.
                        current = current with
                        {
                            RevokedInviteIds = current.RevokedInviteIds.Concat(settings.RevokedInviteIds)
                                .Concat(request.RealtimeDenials)
                                .ToFrozenSet(StringComparer.Ordinal)
                        };
                        _settings = current;
                    }

                    SetVerification(_clock.GetTimestamp());
                    ObserveInvitation(current);
                }
                else if (request.Force)
                {
                    if (settings is not null)
                        ObserveInvitation(settings);

                    ListenForUpdates(false);
                }
            }
        }

        request.Completion.SetResult();
    }

    private void ApplyRealtimeSettings(AiRemoteSettings? settings)
    {
        lock (_stateLock)
        {
            _realtimeRevision++;
            SetSettings(ValidateSettings(settings));
            if (_settings is null)
            {
                SetVerification(null);
                ListenForUpdates(false);
                return;
            }

            if (_pendingFetch is { Force: true } request)
                request.RealtimeDenials.UnionWith(_settings.RevokedInviteIds);

            ObserveInvitation(_settings);
        }
    }

    private void ListenForUpdates(bool listen)
    {
        if (_listeningForUpdates == listen)
            return;

        _listeningForUpdates = listen;
        _remote.SetRealtimeUpdates(listen);
    }

    private void SetSettings(AiRemoteSettings? settings)
    {
        _settings = settings;
        if (settings is null || settings.SurveyUrl == _surveyUrl)
            return;

        _surveyUrl = settings.SurveyUrl;
        NotifyChanged();
    }

    private void SetVerification(long? verifiedAt)
    {
        var wasFresh = HasFreshSettings();
        _verifiedAt = verifiedAt;
        if (wasFresh != HasFreshSettings() && ReadState() is { } state && state.Invitation.HasValue())
            NotifyChanged();
    }

    private void ObserveInvitation(AiRemoteSettings settings)
    {
        if (ReadState() is not { } state || state.Invitation.IsEmpty())
        {
            ListenForUpdates(false);
            return;
        }

        var verified = Verify(state.Invitation);
        if (verified.IsFailure)
        {
            ListenForUpdates(false);
            return;
        }

        if (settings.RevokedInviteIds.Contains(verified.Value))
        {
            ListenForUpdates(false);
            NotifyChanged();
            ClearInvitation(state);
            return;
        }

        if (HasFreshSettings())
            ListenForUpdates(true);
    }

    private bool ClearInvitation(AiAccessState state)
    {
        if (!WriteState(state with { Invitation = "" }))
            return false;

        _wasInvitationRevoked = true;
        ListenForUpdates(false);
        return true;
    }

    private AiAccessState? ReadState()
    {
        try
        {
            var state = _store.Read();
            return state is { Used: >= 0 and <= TrialLimit } && state.Invitation is not null ? state : null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    private bool WriteState(AiAccessState state)
    {
        try
        {
            _store.Write(state);
            NotifyChanged();
            return true;
        }
        catch (IOException)
        {
            return false;
        }
    }

    // Coalesce mutations from the same operation and let observers read the finished state.
    private void NotifyChanged()
    {
        if (_changePending || Changed is null)
            return;

        _changePending = true;
        _ = Task.Run(() =>
        {
            Action? changed;
            lock (_stateLock)
            {
                _changePending = false;
                changed = Changed;
            }

            changed?.Invoke();
        });
    }

    private static AiRemoteSettings? ValidateSettings(AiRemoteSettings? settings)
    {
        if (settings?.RevokedInviteIds is not { } revoked || revoked.Any(id => !IsInviteId(id)))
            return null;

        return new AiRemoteSettings(ValidateSurveyUrl(settings.SurveyUrl), revoked.ToFrozenSet(StringComparer.Ordinal));
    }

    private static bool IsInviteId(string id) =>
        id is { Length: 32 } && id.All(value => value is >= '0' and <= '9' or >= 'a' and <= 'f');

    private static string ValidateSurveyUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps || !uri.IsDefaultPort || uri.UserInfo.HasValue())
            return "";

        return uri.IdnHost == "forms.gle" && uri.AbsolutePath.Length > 1
            || uri.IdnHost == "docs.google.com" && uri.AbsolutePath.StartsWith("/forms/", StringComparison.Ordinal)
            ? uri.AbsoluteUri
            : "";
    }

    private sealed record FetchRequest(bool Force, long RealtimeRevision)
    {
        public TaskCompletionSource Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public HashSet<string> RealtimeDenials { get; } = new(StringComparer.Ordinal);
    }
}
