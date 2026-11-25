#if !ANDROID
using Centello.Application;

namespace Centello.App;

internal sealed class UnavailableAiAccessPlatform : IAiAccessStore, IAiRemoteConfig
{
    public string InstallationId => "";

    public AiAccessState Read() => throw new IOException("AI access storage is unavailable on this platform.");

    public void Write(AiAccessState state) => throw new IOException("AI access storage is unavailable on this platform.");

    public Task<AiRemoteSettings?> Fetch(bool force, CancellationToken cancellationToken) =>
        Task.FromResult<AiRemoteSettings?>(null);

    public void SetRealtimeUpdates(bool enabled) { }

    public event Action<AiRemoteSettings?>? Updated
    {
        add { }
        remove { }
    }
}
#endif
