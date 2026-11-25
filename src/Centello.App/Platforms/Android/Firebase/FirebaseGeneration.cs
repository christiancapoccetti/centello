using Centello.Application;
using Centello.FirebaseAi.Android;

namespace Centello.App;

public sealed class FirebaseGeneration : IFirebaseGeneration
{
    public async Task<Result<FirebaseGenerationResponse>> Generate(
        string prompt,
        string mediaType,
        ReadOnlyMemory<byte> bytes,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return Result<FirebaseGenerationResponse>.Failure(DraftErrors.Canceled);

        var options = FirebaseAndroidOptions.Current;
        if (options is null)
            return Result<FirebaseGenerationResponse>.Failure(DraftErrors.Unavailable);

        var media = bytes.ToArray();
        if (cancellationToken.IsCancellationRequested)
            return Result<FirebaseGenerationResponse>.Failure(DraftErrors.Canceled);

        var callback = new GenerationCallback();
        using var request = FirebaseAiBridge.Start(
            global::Android.App.Application.Context,
            options.ApplicationId,
            options.ApiKey,
            options.ProjectId,
            options.SenderId,
            mediaType,
            media,
            prompt,
            callback);

        if (request is null)
            return Result<FirebaseGenerationResponse>.Failure(DraftErrors.Unavailable);

        using var registration = cancellationToken.Register(() =>
        {
            callback.OnError("cancelled");
            request.Cancel();
        });

        return await callback.Task.ConfigureAwait(false);
    }

    private sealed class GenerationCallback : Java.Lang.Object, IFirebaseAiCallback
    {
        private readonly TaskCompletionSource<Result<FirebaseGenerationResponse>> _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<Result<FirebaseGenerationResponse>> Task => _completion.Task;

        public void OnResult(string? json, int inputTokens, int outputTokens)
        {
            if (json is null)
            {
                OnError("invalid_response");
                return;
            }

            _completion.TrySetResult(Result<FirebaseGenerationResponse>.Success(
                new FirebaseGenerationResponse(
                    json,
                    inputTokens < 0 ? null : inputTokens,
                    outputTokens < 0 ? null : outputTokens)));
        }

        public void OnError(string? status) =>
            _completion.TrySetResult(Result<FirebaseGenerationResponse>.Failure(status switch
            {
                "blocked" => DraftErrors.Blocked,
                "forbidden" => DraftErrors.AccessDenied,
                "rate_limited" => DraftErrors.RateLimited,
                "timeout" => DraftErrors.TimedOut,
                "invalid_response" => DraftErrors.MalformedResponse,
                "cancelled" => DraftErrors.Canceled,
                _ => DraftErrors.Unavailable
            }));
    }
}
