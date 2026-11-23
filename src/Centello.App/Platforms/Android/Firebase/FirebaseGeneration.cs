using System.Text.Json;
using Centello.Application;
using Centello.FirebaseAi.Android;

namespace Centello.App;

public sealed class FirebaseGeneration : IFirebaseGeneration
{
    private const string ConfigAsset = "firebase-android.json";
    private static readonly Lazy<FirebaseOptions?> Options = new(ReadOptions);

    public async Task<Result<FirebaseGenerationResponse>> Generate(
        string prompt,
        string mediaType,
        ReadOnlyMemory<byte> bytes,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return Result<FirebaseGenerationResponse>.Failure(DraftErrors.Canceled);

        var options = Options.Value;
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

    private static FirebaseOptions? ReadOptions()
    {
        try
        {
            using var stream = global::Android.App.Application.Context.Assets!.Open(ConfigAsset);
            using var document = JsonDocument.Parse(stream);
            var root = document.RootElement;

            return root.TryGetProperty("applicationId", out var id) && id.GetString() is { Length: > 0 } appId
                   && root.TryGetProperty("apiKey", out var key) && key.GetString() is { Length: > 0 } apiKey
                   && root.TryGetProperty("projectId", out var project) && project.GetString() is { Length: > 0 } projectId
                   && root.TryGetProperty("gcmSenderId", out var sender) && sender.GetString() is { Length: > 0 } senderId
                ? new FirebaseOptions(appId, apiKey, projectId, senderId)
                : null;
        }
        catch (Exception exception) when (exception is IOException or Java.IO.IOException
                                          or JsonException or InvalidOperationException)
        {
            return null;
        }
    }

    private sealed record FirebaseOptions(string ApplicationId, string ApiKey, string ProjectId, string SenderId);

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
