using System.Text.Json;

namespace Centello.App;

internal sealed record FirebaseAndroidOptions(string ApplicationId, string ApiKey, string ProjectId, string SenderId)
{
    private const string ConfigAsset = "firebase-android.json";
    private static readonly Lazy<FirebaseAndroidOptions?> Options = new(ReadOptions);

    public static FirebaseAndroidOptions? Current => Options.Value;

    private static FirebaseAndroidOptions? ReadOptions()
    {
        try
        {
            using var stream = global::Android.App.Application.Context.Assets!.Open(ConfigAsset);
            using var document = JsonDocument.Parse(stream);
            var root = document.RootElement;

            return root.TryGetProperty("applicationId", out var id) && id.GetString() is { Length: > 0 } appId &&
                root.TryGetProperty("apiKey", out var key) && key.GetString() is { Length: > 0 } apiKey &&
                root.TryGetProperty("projectId", out var project) && project.GetString() is { Length: > 0 } projectId &&
                root.TryGetProperty("gcmSenderId", out var sender) && sender.GetString() is { Length: > 0 } senderId
                ? new FirebaseAndroidOptions(appId, apiKey, projectId, senderId)
                : null;
        }
        catch (Exception exception) when (exception is IOException or Java.IO.IOException
                                          or JsonException or InvalidOperationException)
        {
            return null;
        }
    }
}
