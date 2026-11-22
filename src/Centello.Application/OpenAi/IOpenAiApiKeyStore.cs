namespace Centello.Application;

/// <summary>Raw persistence for the API key. Rules live in <see cref="OpenAiSettings"/>; implementations only store.</summary>
public interface IOpenAiApiKeyStore
{
    /// <returns>The stored key and disclosure version, or null if the key is missing or unreadable.</returns>
    Task<StoredOpenAiApiKey?> GetApiKey();

    Task SetApiKey(StoredOpenAiApiKey apiKey);
    void ClearApiKey();
}

public sealed class StoredOpenAiApiKey(string value, int consentVersion)
{
    public string Value { get; } = value;
    public int ConsentVersion { get; } = consentVersion;
}
