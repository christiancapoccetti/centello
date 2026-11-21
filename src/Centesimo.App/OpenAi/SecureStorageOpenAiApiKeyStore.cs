using Centesimo.Application;
using Microsoft.Maui.Storage;

namespace Centesimo.App;

public sealed class SecureStorageOpenAiApiKeyStore : IOpenAiApiKeyStore
{
    private const string ApiKeyName = "openai_api_key";

    public async Task<string> GetApiKey()
    {
        try
        {
            return await SecureStorage.Default.GetAsync(ApiKeyName) ?? "";
        }
        catch (Exception)
        {
            // The Android keystore can invalidate stored values (for example after a restore); that must read as "no key".
            return "";
        }
    }

    public Task SetApiKey(string apiKey) => SecureStorage.Default.SetAsync(ApiKeyName, apiKey);

    public void ClearApiKey() => SecureStorage.Default.Remove(ApiKeyName);
}
