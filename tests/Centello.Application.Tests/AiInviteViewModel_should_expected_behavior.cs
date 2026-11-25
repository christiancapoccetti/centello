using Centello.App.ViewModels;

namespace Centello.Application.Tests;

public sealed class AiInviteViewModel_should_expected_behavior
{
    private static readonly AiRemoteSettings Available = new("", new HashSet<string>());
    private const string InviteId = "aabbccddaabbccddaabbccddaabbccdd";

    [Fact]
    public async Task Show_the_active_invitation_after_verifying_the_pasted_code()
    {
        var verification = new TaskCompletionSource<AiRemoteSettings?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var viewModel = Create(new Config { Response = verification.Task });
        viewModel.Code = "  signed-code  ";

        var activating = viewModel.Activate(CancellationToken.None);
        Assert.True(viewModel.IsBusy);
        Assert.False(viewModel.CanActivate);

        verification.SetResult(Available);
        await activating;

        Assert.False(viewModel.IsBusy);
        Assert.False(viewModel.ShowForm);
        Assert.False(viewModel.HasError);
        Assert.True(viewModel.IsActive);
        Assert.Equal("", viewModel.Code);
    }

    [Theory]
    [InlineData("invalid", "Codice non valido.")]
    [InlineData("wrong-device", "Questo codice è per un altro telefono.")]
    [InlineData("signed-code", "Questo invito è stato revocato.")]
    public async Task Explain_why_the_code_was_rejected_and_keep_it_in_the_field(string code, string feedback)
    {
        var config = new Config { Response = Task.FromResult<AiRemoteSettings?>(new AiRemoteSettings("", new HashSet<string> { InviteId })) };
        var viewModel = Create(config);
        viewModel.Code = code;

        await viewModel.Activate(CancellationToken.None);

        Assert.False(viewModel.IsActive);
        Assert.True(viewModel.ShowForm);
        Assert.StartsWith(feedback, viewModel.ErrorMessage);
        Assert.Equal(code, viewModel.Code);
    }

    [Fact]
    public async Task Retry_verification_of_a_saved_invitation_without_pasting_the_code_again()
    {
        var config = new Config();
        var viewModel = Create(config, "signed-code");

        await viewModel.Refresh(CancellationToken.None);

        Assert.False(viewModel.IsActive);
        Assert.StartsWith("Non riesco a verificare l’invito.", viewModel.ErrorMessage);
        Assert.Equal("Riprova", viewModel.ActivateText);
        Assert.True(viewModel.CanActivate);

        config.Response = Task.FromResult<AiRemoteSettings?>(Available);
        await viewModel.Activate(CancellationToken.None);

        Assert.True(viewModel.IsActive);
        Assert.False(viewModel.HasError);
    }

    [Fact]
    public async Task Refresh_a_visible_revocation_without_requesting_another_remote_check()
    {
        var config = new Config { Response = Task.FromResult<AiRemoteSettings?>(Available) };
        var viewModel = Create(config, "signed-code");
        await viewModel.Refresh(CancellationToken.None);

        config.Update(new AiRemoteSettings("", new HashSet<string> { InviteId }));
        viewModel.UpdateAccess();

        Assert.False(viewModel.IsActive);
        Assert.True(viewModel.ShowForm);
        Assert.StartsWith("Questo invito è stato revocato.", viewModel.ErrorMessage);
        Assert.Equal(1, config.Fetches);
    }

    [Fact]
    public async Task Keep_the_code_and_its_feedback_when_remote_settings_change()
    {
        var config = new Config();
        var viewModel = Create(config);
        viewModel.Code = "wrong-device";
        await viewModel.Activate(CancellationToken.None);

        config.Update(new AiRemoteSettings("https://forms.gle/example", new HashSet<string>()));
        viewModel.UpdateAccess();

        Assert.Equal("wrong-device", viewModel.Code);
        Assert.StartsWith("Questo codice è per un altro telefono.", viewModel.ErrorMessage);
        Assert.Equal(0, config.Fetches);
    }

    private static AiInviteViewModel Create(Config config, string savedInvite = "") =>
        new(new AiAccess(new Store(savedInvite), config, new Verifier(), TimeProvider.System));

    private sealed class Store(string savedInvite) : IAiAccessStore
    {
        private AiAccessState _state = new(0, savedInvite);
        public string InstallationId => "new-installation";
        public AiAccessState Read() => _state;
        public void Write(AiAccessState state) => _state = state;
    }

    private sealed class Config : IAiRemoteConfig
    {
        public Task<AiRemoteSettings?> Response { get; set; } = Task.FromResult<AiRemoteSettings?>(null);
        public int Fetches { get; private set; }
        public event Action<AiRemoteSettings?>? Updated;
        public Task<AiRemoteSettings?> Fetch(bool force, CancellationToken cancellationToken)
        {
            Fetches++;
            return Response;
        }
        public void SetRealtimeUpdates(bool active)
        {
        }

        public void Update(AiRemoteSettings settings) => Updated?.Invoke(settings);
    }

    private sealed class Verifier : IAiInviteVerifier
    {
        public Result<string> Verify(string invitation, string installationId) => invitation switch
        {
            "signed-code" => Result<string>.Success(InviteId),
            "wrong-device" => Result<string>.Failure(DraftErrors.InviteDeviceMismatch),
            _ => Result<string>.Failure(DraftErrors.InvalidInvite)
        };
    }
}
