using Centesimo.Domain;

namespace Centesimo.Application;

public interface IOpenAiApiKeyProvider
{
    /// <returns>The API key, or <c>""</c> when none is configured.</returns>
    Task<string> GetApiKey();
}

/// <summary>Owns the rules for the OpenAI key, so it can never be saved without the user's consent.</summary>
public sealed class OpenAiSettings(IOpenAiApiKeyStore store) : IOpenAiApiKeyProvider
{
    public const string ConsentMessage =
        "Usando OpenAI, l'audio registrato e il testo della spesa vengono inviati a OpenAI per la trascrizione e l'interpretazione. " +
        "Le tue spese restano salvate solo sul dispositivo. " +
        "La chiave è conservata in modo sicuro sul telefono, ma una chiave su un'app non ha le garanzie di un server: usala solo per uso personale.";

    public Task<string> GetApiKey() => store.GetApiKey();

    public async Task<bool> HasApiKey() => (await store.GetApiKey()).HasValue();

    public async Task<Result> SaveApiKey(string apiKey, bool consentAccepted)
    {
        if (!consentAccepted)
            return Result.Failure(SpeechErrors.ConsentRequired);

        if (apiKey.IsEmpty())
            return Result.Failure(SpeechErrors.EmptyApiKey);

        await store.SetApiKey(apiKey.Trim());
        return Result.Success();
    }

    public void ClearApiKey() => store.ClearApiKey();
}
