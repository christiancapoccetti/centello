#if !ANDROID
using Centello.Application;

namespace Centello.App;

internal sealed class UnavailableFirebaseGeneration : IFirebaseGeneration
{
    public Task<Result<FirebaseGenerationResponse>> Generate(
        string prompt, string mediaType, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken) =>
        Task.FromResult(Result<FirebaseGenerationResponse>.Failure(DraftErrors.Unavailable));
}
internal sealed class UnavailablePdfPageCounter : IPdfPageCounter
{
    public Task<Result<int>> Count(ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken) =>
        Task.FromResult(Result<int>.Failure(PhotoErrors.ProcessingFailed));
}
#endif
