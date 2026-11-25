namespace Centello.Application.Tests;

public sealed class AiAccess_should_expected_behavior
{
    private const string InviteId = "a1b2c3d4e5f600000000000000000000";
    private const string Invitation = "signed-invitation";
    private readonly FakeAiAccessStore _store = new();
    private readonly FakeAiRemoteConfig _remote = new();
    private readonly FakeAiInviteVerifier _verifier = new();
    private readonly AiAccess _access;

    public AiAccess_should_expected_behavior()
    {
        _access = new AiAccess(_store, _remote, _verifier, TimeProvider.System);
        _verifier.Valid[Invitation] = InviteId;
    }

    [Fact]
    public async Task Allow_five_lifetime_trial_uses_per_installation()
    {
        Assert.Equal(5, _access.TrialsRemaining);
        Assert.True((await _access.Check()).IsSuccess);

        _store.State = new AiAccessState(5, "");
        var reopened = new AiAccess(_store, _remote, _verifier, TimeProvider.System);

        Assert.Equal(0, reopened.TrialsRemaining);
        Assert.Equal(DraftErrors.TrialExhausted, (await reopened.Check()).Error);
        Assert.Empty(_remote.Requests);
    }

    [Fact]
    public async Task Fetch_the_optional_survey_in_a_regular_foreground_session()
    {
        _remote.Settings = new AiRemoteSettings("https://forms.gle/example", new HashSet<string>());

        await _access.BeginSession();

        Assert.Equal([false], _remote.Requests);
        Assert.Equal("https://forms.gle/example", _access.SurveyUrl);
        Assert.False(_remote.ListenerEnabled);
    }

    [Fact]
    public async Task Keep_the_last_good_survey_when_a_refresh_is_unavailable()
    {
        _remote.Settings = new AiRemoteSettings("https://docs.google.com/forms/d/example/viewform", new HashSet<string>());
        await _access.BeginSession();
        _remote.Settings = null;

        await _access.BeginSession();

        Assert.Equal("https://docs.google.com/forms/d/example/viewform", _access.SurveyUrl);
    }

    [Theory]
    [InlineData("http://forms.gle/example")]
    [InlineData("https://docs.google.com/document/example")]
    [InlineData("https://forms.gle.evil.example/example")]
    [InlineData("https://user@forms.gle/example")]
    [InlineData("https://forms.gle:8443/example")]
    public async Task Ignore_a_survey_url_outside_https_google_forms(string url)
    {
        _remote.Settings = new AiRemoteSettings(url, new HashSet<string>());

        await _access.BeginSession();

        Assert.Empty(_access.SurveyUrl);
    }

    [Fact]
    public async Task Redeem_only_after_validating_the_signature_installation_and_fresh_revocations()
    {
        _store.State = new AiAccessState(5, "");

        Assert.Equal(DraftErrors.InvalidInvite, (await _access.Redeem("tampered")).Error);
        _verifier.Error = DraftErrors.InviteDeviceMismatch;
        Assert.Equal(DraftErrors.InviteDeviceMismatch, (await _access.Redeem("another-device")).Error);
        Assert.Empty(_remote.Requests);
        Assert.False(_access.HasInvitation);

        var result = await _access.Redeem($" {Invitation} ");

        Assert.True(result.IsSuccess);
        Assert.Equal([true], _remote.Requests);
        Assert.Equal(_store.InstallationId, _verifier.InstallationIdSeen);
        Assert.Equal(new AiAccessState(5, Invitation), _store.State);
        Assert.True(_remote.ListenerEnabled);
        Assert.True(_access.IsInvitationVerified);
        Assert.True((await _access.Check()).IsSuccess);
    }

    [Fact]
    public async Task Share_one_fresh_check_and_fail_closed_when_returning_to_the_foreground()
    {
        _store.State = new AiAccessState(4, Invitation);
        Assert.True((await _access.Check()).IsSuccess);
        var response = new TaskCompletionSource<AiRemoteSettings?>();
        _remote.Response = response.Task;

        var foreground = _access.BeginSession();
        Assert.False(_access.IsInvitationVerified);
        var first = _access.Check();
        using var canceled = new CancellationTokenSource();
        var second = _access.Check(canceled.Token);
        canceled.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => second);
        Assert.False(first.IsCompleted);
        Assert.Equal([true, true], _remote.Requests);
        response.SetResult(null);
        await foreground;

        Assert.Equal(DraftErrors.InviteVerificationUnavailable, (await first).Error);
        Assert.Equal(new AiAccessState(4, Invitation), _store.State);
        Assert.False(_remote.ListenerEnabled);
    }

    [Fact]
    public async Task Clear_a_revoked_invite_and_keep_the_remaining_trial()
    {
        _store.State = new AiAccessState(4, Invitation);
        Assert.True((await _access.Check()).IsSuccess);

        _remote.Publish(new AiRemoteSettings("", new HashSet<string> { InviteId }));

        Assert.False(_access.HasInvitation);
        Assert.False(_access.IsInvitationVerified);
        Assert.True(_access.WasInvitationRevoked);
        Assert.False(_remote.ListenerEnabled);
        Assert.Equal(new AiAccessState(4, ""), _store.State);
        Assert.True((await _access.Check()).IsSuccess);
        Assert.Equal(DraftErrors.InviteRevoked, (await _access.Redeem(Invitation)).Error);

        _verifier.Valid["replacement"] = "00000000000000000000000000000000";
        Assert.True((await _access.Redeem("replacement")).IsSuccess);
        Assert.False(_access.WasInvitationRevoked);
    }

    [Fact]
    public async Task Keep_realtime_revocation_when_a_pending_fetch_returns_an_older_snapshot()
    {
        var fetch = new TaskCompletionSource<AiRemoteSettings?>();
        _remote.Response = fetch.Task;
        var redeem = _access.Redeem(Invitation);

        _remote.Publish(new AiRemoteSettings("", new HashSet<string> { InviteId }));
        fetch.SetResult(new AiRemoteSettings("", new HashSet<string>()));

        Assert.Equal(DraftErrors.InviteRevoked, (await redeem).Error);
        Assert.False(_access.HasInvitation);
    }

    [Fact]
    public async Task Keep_a_forced_fetch_revocation_even_if_a_realtime_update_arrives_during_the_fetch()
    {
        _store.State = new AiAccessState(4, Invitation);
        Assert.True((await _access.Check()).IsSuccess);
        var response = new TaskCompletionSource<AiRemoteSettings?>();
        _remote.Response = response.Task;
        var refresh = _access.BeginSession();

        _remote.Publish(new AiRemoteSettings("https://forms.gle/newer", new HashSet<string>()));
        response.SetResult(new AiRemoteSettings("https://forms.gle/older", new HashSet<string> { InviteId }));
        await refresh;

        Assert.False(_access.HasInvitation);
        Assert.True(_access.WasInvitationRevoked);
        Assert.Equal("https://forms.gle/newer", _access.SurveyUrl);
        Assert.Equal(new AiAccessState(4, ""), _store.State);
    }

    [Fact]
    public async Task Keep_a_forced_fetch_denial_for_an_invite_being_redeemed()
    {
        var response = new TaskCompletionSource<AiRemoteSettings?>();
        _remote.Response = response.Task;
        var redeem = _access.Redeem(Invitation);

        _remote.Publish(new AiRemoteSettings("", new HashSet<string>()));
        response.SetResult(new AiRemoteSettings("", new HashSet<string> { InviteId }));

        Assert.Equal(DraftErrors.InviteRevoked, (await redeem).Error);
        Assert.False(_access.HasInvitation);
    }

    [Fact]
    public async Task Notify_observers_when_the_survey_changes()
    {
        var changed = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        _access.Changed += () => changed.TrySetResult(_access.SurveyUrl);
        _remote.Settings = new AiRemoteSettings("https://forms.gle/newer", new HashSet<string>());

        await _access.BeginSession();

        Assert.Equal("https://forms.gle/newer", await changed.Task.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task Reject_a_malformed_revocation_snapshot()
    {
        _store.State = new AiAccessState(4, Invitation);
        _remote.Settings = new AiRemoteSettings("", new HashSet<string> { "malformed" });

        Assert.Equal(DraftErrors.InviteVerificationUnavailable, (await _access.Check()).Error);
        Assert.Equal(new AiAccessState(4, Invitation), _store.State);
    }

    [Fact]
    public async Task Stop_listening_and_reverify_on_a_malformed_realtime_update()
    {
        _store.State = new AiAccessState(4, Invitation);
        Assert.True((await _access.Check()).IsSuccess);

        _remote.Publish(null);

        Assert.False(_remote.ListenerEnabled);
        Assert.Equal(DraftErrors.InviteVerificationUnavailable, (await _access.Check()).Error);
        Assert.Equal([true, true], _remote.Requests);
        Assert.Equal(new AiAccessState(4, Invitation), _store.State);
    }

    [Fact]
    public async Task Ignore_a_fetch_started_before_the_current_foreground_session()
    {
        _store.State = new AiAccessState(4, Invitation);
        var previousResponse = new TaskCompletionSource<AiRemoteSettings?>();
        _remote.Response = previousResponse.Task;
        var previous = _access.BeginSession();
        _remote.Response = null;
        _remote.Settings = null;
        await _access.BeginSession();

        previousResponse.SetResult(new AiRemoteSettings("", new HashSet<string>()));
        await previous;

        Assert.False(_remote.ListenerEnabled);
        Assert.Equal(DraftErrors.InviteVerificationUnavailable, (await _access.Check()).Error);
    }
}

internal sealed class FakeAiAccessStore : IAiAccessStore
{
    public string InstallationId => "aa112233445566778899aabbccddeeff";
    public AiAccessState State { get; set; } = new(0, "");
    public bool FailWrite { get; set; }

    public AiAccessState Read() => State;

    public void Write(AiAccessState state)
    {
        if (FailWrite)
            throw new IOException();

        State = state;
    }
}

internal sealed class FakeAiRemoteConfig : IAiRemoteConfig
{
    public AiRemoteSettings? Settings { get; set; } = new("", new HashSet<string>());
    public Task<AiRemoteSettings?>? Response { get; set; }
    public List<bool> Requests { get; } = [];
    public bool ListenerEnabled { get; private set; }
    public event Action<AiRemoteSettings?>? Updated;

    public Task<AiRemoteSettings?> Fetch(bool force, CancellationToken cancellationToken)
    {
        Requests.Add(force);
        return Response ?? Task.FromResult(Settings);
    }

    public void SetRealtimeUpdates(bool enabled) => ListenerEnabled = enabled;

    public void Publish(AiRemoteSettings? settings)
    {
        Settings = settings;
        Updated?.Invoke(settings);
    }
}

internal sealed class FakeAiInviteVerifier : IAiInviteVerifier
{
    public Dictionary<string, string> Valid { get; } = [];
    public Error? Error { get; set; }
    public string InstallationIdSeen { get; private set; } = "";

    public Result<string> Verify(string invitation, string installationId)
    {
        InstallationIdSeen = installationId;
        return Valid.TryGetValue(invitation, out var inviteId)
            ? Result<string>.Success(inviteId)
            : Result<string>.Failure(Error ?? DraftErrors.InvalidInvite);
    }
}
