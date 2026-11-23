namespace Centello.Application;

public interface IFirebaseGeneration
{
    Task<Result<FirebaseGenerationResponse>> Generate(
        string prompt, string mediaType, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken);
}

public sealed record FirebaseGenerationResponse(string Json, int? InputTokens = null, int? OutputTokens = null);
