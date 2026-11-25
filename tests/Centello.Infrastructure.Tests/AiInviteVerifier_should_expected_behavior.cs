using Centello.Application;

namespace Centello.Infrastructure.Tests;

public sealed class AiInviteVerifier_should_expected_behavior
{
    // The first RFC 8032 test key signed this synthetic Centello payload with Node's crypto library.
    private const string PublicKey = "11qYAYKxCrfVS/7TyWQHOg7hcvPapiMlrwIaaPcHURo=";
    private const string Installation = "22222222222222222222222222222222";
    private const string InviteId = "11111111111111111111111111111111";
    private const string Token = "cti1." + InviteId + "." + Installation
        + ".0NNrcaflGkG97O_5VK21gZZVnHAym3yN6D2xmFua43szo9dyeSeuKWadYcmeCdKAWVN1SgrYzJpz488hm5e2AA";

    [Fact]
    public void Verify_an_independently_signed_installation_bound_token()
    {
        var verified = new AiInviteVerifier(PublicKey).Verify(Token, Installation);
        Assert.Equal(InviteId, verified.Value);
    }

    [Fact]
    public void Distinguish_an_authentic_invite_for_another_phone_from_edited_claims()
    {
        var verifier = new AiInviteVerifier(PublicKey);
        var other = new string('3', 32);
        Assert.Equal(DraftErrors.InviteDeviceMismatch, verifier.Verify(Token, other).Error);
        Assert.Equal(DraftErrors.InvalidInvite, verifier.Verify(Token.Replace(Installation, other), other).Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("cti1.fake.fake.fake")]
    [InlineData(Token + ".extra")]
    public void Reject_malformed_input(string token) =>
        Assert.Equal(DraftErrors.InvalidInvite, new AiInviteVerifier(PublicKey).Verify(token, Installation).Error);

    [Fact]
    public void Reject_noncanonical_base64url_even_if_it_decodes_to_the_same_signature() =>
        Assert.Equal(DraftErrors.InvalidInvite, new AiInviteVerifier(PublicKey).Verify(Token[..^1] + "B", Installation).Error);

    [Fact]
    public void Reject_an_invite_when_the_issuer_is_unconfigured() =>
        Assert.Equal(DraftErrors.InviteVerificationUnavailable, new AiInviteVerifier("").Verify(Token, Installation).Error);
}
