using Centello.Application;

namespace Centello.App;

public sealed class PreferencesDraftConsentStore : IDraftConsentStore
{
    private const string Key = "firebase_ai_draft_consent_version";

    public int AcceptedVersion
    {
        get => Preferences.Default.Get(Key, 0);
        set => Preferences.Default.Set(Key, value);
    }

    public static void RemoveLegacyCredentials()
    {
        foreach (var key in new[] { "openai_api_key", "openai_api_key_with_consent" })
        {
            try
            {
                SecureStorage.Default.Remove(key);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Unable to remove a legacy credential ({ex.GetType().Name}).");
            }
        }
    }
}
