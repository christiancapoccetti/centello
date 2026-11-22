using System.Globalization;
using Centello.Application;
using Microsoft.Maui.Storage;

namespace Centello.App;

public sealed class SecureStorageOpenAiApiKeyStore : IOpenAiApiKeyStore
{
    private const string LegacyApiKeyName = "openai_api_key";
    private const string VersionedApiKeyName = "openai_api_key_with_consent";

    public async Task<StoredOpenAiApiKey?> GetApiKey()
    {
        try
        {
            var stored = await SecureStorage.Default.GetAsync(VersionedApiKeyName);
            if (stored is not null)
                return ParseVersionedKey(stored);

            var legacyKey = await SecureStorage.Default.GetAsync(LegacyApiKeyName);
            return string.IsNullOrWhiteSpace(legacyKey) ? null : new StoredOpenAiApiKey(legacyKey, consentVersion: 0);
        }
        catch (Exception)
        {
            // The Android keystore can invalidate stored values (for example after a restore); that must read as "no key".
            return null;
        }
    }

    public async Task SetApiKey(StoredOpenAiApiKey apiKey)
    {
        // Store the consent version and key together so a partial write cannot authorize a key.
        var stored = $"{apiKey.ConsentVersion.ToString(CultureInfo.InvariantCulture)}\n{apiKey.Value}";
        await SecureStorage.Default.SetAsync(VersionedApiKeyName, stored);
        SecureStorage.Default.Remove(LegacyApiKeyName);
    }

    public void ClearApiKey()
    {
        SecureStorage.Default.Remove(LegacyApiKeyName);
        SecureStorage.Default.Remove(VersionedApiKeyName);
    }

    private static StoredOpenAiApiKey? ParseVersionedKey(string stored)
    {
        var separator = stored.IndexOf('\n');
        if (separator < 0 || !int.TryParse(stored.AsSpan(0, separator), NumberStyles.None,
                CultureInfo.InvariantCulture, out var consentVersion))
            return null;

        var value = stored[(separator + 1)..];
        return string.IsNullOrWhiteSpace(value) ? null : new StoredOpenAiApiKey(value, consentVersion);
    }
}
