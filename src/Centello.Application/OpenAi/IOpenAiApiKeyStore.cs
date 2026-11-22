namespace Centello.Application;

/// <summary>Raw persistence for the API key. Rules live in <see cref="OpenAiSettings"/>; implementations only store.</summary>
public interface IOpenAiApiKeyStore
{
    /// <returns>The stored key, or <c>""</c> when there is none or it cannot be read.</returns>
    Task<string> GetApiKey();

    Task SetApiKey(string apiKey);
    void ClearApiKey();
}
