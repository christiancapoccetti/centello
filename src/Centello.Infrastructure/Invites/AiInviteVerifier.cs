using System.Buffers;
using System.Buffers.Text;
using System.Text;
using Centello.Application;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math.EC.Rfc8032;

namespace Centello.Infrastructure;

public sealed class AiInviteVerifier(string publicKey) : IAiInviteVerifier
{
    private const string Version = "cti1";
    private const int IdentifierLength = 32;
    private const int SignatureLength = 64;
    private const int EncodedSignatureLength = 86;
    private const int TokenLength = 4 + IdentifierLength * 2 + EncodedSignatureLength + 3;

    private readonly Ed25519PublicKeyParameters? _publicKey = ReadKey(publicKey);

    public Result<string> Verify(string invitation, string installationId)
    {
        if (_publicKey is null)
            return Result<string>.Failure(DraftErrors.InviteVerificationUnavailable);

        if (invitation is not { Length: TokenLength })
            return Result<string>.Failure(DraftErrors.InvalidInvite);

        var parts = invitation.Split('.');
        if (parts.Length != 4 || parts[0] != Version || !IsIdentifier(parts[1]) || !IsIdentifier(parts[2]))
            return Result<string>.Failure(DraftErrors.InvalidInvite);

        var encodedSignature = parts[3];
        var signature = new byte[SignatureLength];
        if (encodedSignature.Length != EncodedSignatureLength
            || Base64Url.DecodeFromChars(encodedSignature, signature, out _, out var written) != OperationStatus.Done
            || written != SignatureLength
            || Base64Url.EncodeToString(signature) != encodedSignature)
            return Result<string>.Failure(DraftErrors.InvalidInvite);

        var payload = Encoding.ASCII.GetBytes(invitation[..^(EncodedSignatureLength + 1)]);
        var signer = new Ed25519Signer();
        signer.Init(false, _publicKey);
        signer.BlockUpdate(payload, 0, payload.Length);
        if (!signer.VerifySignature(signature))
            return Result<string>.Failure(DraftErrors.InvalidInvite);

        return parts[2] == installationId
            ? Result<string>.Success(parts[1])
            : Result<string>.Failure(DraftErrors.InviteDeviceMismatch);
    }

    private static bool IsIdentifier(string identifier) =>
        identifier.Length == IdentifierLength && identifier.All(value => value is >= '0' and <= '9' or >= 'a' and <= 'f');

    private static Ed25519PublicKeyParameters? ReadKey(string publicKey)
    {
        var bytes = new byte[Ed25519PublicKeyParameters.KeySize];
        if (!Convert.TryFromBase64String(publicKey, bytes, out var written) || written != bytes.Length
            || Convert.ToBase64String(bytes) != publicKey || !Ed25519.ValidatePublicKeyFull(bytes, 0))
            return null;

        return new Ed25519PublicKeyParameters(bytes, 0);
    }
}
