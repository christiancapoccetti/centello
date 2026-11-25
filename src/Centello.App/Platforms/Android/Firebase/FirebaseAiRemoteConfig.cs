using Centello.Application;
using Centello.FirebaseAi.Android;

namespace Centello.App;

internal sealed class FirebaseAiRemoteConfig : IAiRemoteConfig
{
    private const string Tag = "CentelloRemoteConfig";
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(8);
    private readonly SemaphoreSlim _fetchGate = new(1);
    private readonly object _bridgeGate = new();
    private FirebaseRemoteConfigBridge? _bridge;
    private volatile bool _listen;

    public event Action<AiRemoteSettings?>? Updated;

    public async Task<AiRemoteSettings?> Fetch(bool force, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(Timeout);
        var token = timeout.Token;
        try
        {
            await _fetchGate.WaitAsync(token).ConfigureAwait(false);
            // Let the SDK finish writing its cache even when the caller stops waiting.
            var request = Task.Run(() => FinishFetch(force, token));
            return await request.WaitAsync(token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            global::Android.Util.Log.Warn(Tag, $"Managed fetch deadline exceeded (force={force}).");
            return null;
        }
    }

    public void SetRealtimeUpdates(bool enabled)
    {
        if (_listen == enabled)
            return;

        _listen = enabled;
        _ = Task.Run(UpdateListener);
    }

    private async Task<AiRemoteSettings?> FinishFetch(bool force, CancellationToken cancellationToken)
    {
        try
        {
            if (cancellationToken.IsCancellationRequested)
                return null;

            var bridge = GetBridge();
            if (bridge is null)
            {
                global::Android.Util.Log.Warn(Tag, "Firebase options or Remote Config bridge unavailable.");
                return null;
            }

            var completion = new TaskCompletionSource<AiRemoteSettings?>(TaskCreationOptions.RunContinuationsAsynchronously);
            bridge.Fetch(force, new SettingsCallback(settings => completion.TrySetResult(settings)));
            return await completion.Task.ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            global::Android.Util.Log.Warn(Tag, $"Managed fetch failed: {exception.GetType().Name}");
            return null;
        }
        finally
        {
            _fetchGate.Release();
        }
    }

    private FirebaseRemoteConfigBridge? GetBridge()
    {
        var options = FirebaseAndroidOptions.Current;
        if (options is null)
            return null;

        lock (_bridgeGate)
        {
            return _bridge ??= FirebaseRemoteConfigBridge.Create(
                global::Android.App.Application.Context,
                options.ApplicationId,
                options.ApiKey,
                options.ProjectId,
                options.SenderId);
        }
    }

    private void UpdateListener()
    {
        lock (_bridgeGate)
        {
            if (_bridge is null)
                return;

            try
            {
                if (_listen)
                    _bridge.Listen(new SettingsCallback(PublishUpdate));
                else
                    _bridge.StopListening();
            }
            catch (Exception exception)
            {
                global::Android.Util.Log.Warn(Tag, $"Managed listener failed: {exception.GetType().Name}");
            }
        }
    }

    private void PublishUpdate(AiRemoteSettings? settings)
    {
        if (_listen)
            Updated?.Invoke(settings);
    }

    private sealed class SettingsCallback(Action<AiRemoteSettings?> publish) : Java.Lang.Object, IFirebaseRemoteConfigCallback
    {
        public void OnSettings(string? surveyUrl, string? revokedInviteIds)
        {
            var settings = FirebaseAiSettings.Parse(surveyUrl, revokedInviteIds);
            global::Android.Util.Log.Info(Tag, $"Managed settings received: valid={settings is not null}.");
            publish(settings);
        }
    }
}
