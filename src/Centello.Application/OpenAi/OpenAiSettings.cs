using Centello.Domain;

namespace Centello.Application;

public interface IOpenAiApiKeyProvider
{
    /// <returns>The API key authorized for the current disclosure, or <c>""</c> otherwise.</returns>
    Task<string> GetApiKey();
}

public enum OpenAiApiKeyStatus { Missing, ConsentRequired, Active }

/// <summary>Only exposes a key to cloud requests after the user accepts the current disclosure.</summary>
public sealed class OpenAiSettings(IOpenAiApiKeyStore store) : IOpenAiApiKeyProvider
{
    public const int CurrentConsentVersion = 1;

    public const string ConsentMessage =
        "Quando detti una spesa, Centello invia a OpenAI l'audio registrato e i nomi di categorie e tag attivi per la trascrizione. " +
        "Per preparare la bozza, invia il testo trascritto oppure la foto o il PDF che hai scelto, insieme ai nomi delle categorie e dei tag attivi e alla data di oggi. " +
        "Scontrini e fatture possono contenere importi, acquisti e dati personali. I PDF vengono inviati per intero. " +
        "Le richieste usano la tua chiave API, salvata sul telefono. Centello tiene l'audio in memoria e può usare la cache del telefono per foto e PDF. " +
        "Puoi inserire le spese a mano senza OpenAI e disattivare questi invii rimuovendo la chiave.";

    public async Task<string> GetApiKey() =>
        await store.GetApiKey() is { ConsentVersion: CurrentConsentVersion } key ? key.Value : "";

    public async Task<bool> HasApiKey() => (await GetApiKey()).HasValue();

    public async Task<OpenAiApiKeyStatus> GetApiKeyStatus() => await store.GetApiKey() switch
    {
        { Value.Length: > 0, ConsentVersion: CurrentConsentVersion } => OpenAiApiKeyStatus.Active,
        { Value.Length: > 0 } => OpenAiApiKeyStatus.ConsentRequired,
        _ => OpenAiApiKeyStatus.Missing
    };

    public async Task<Result> SaveApiKey(string apiKey, bool consentAccepted)
    {
        if (!consentAccepted)
            return Result.Failure(DraftErrors.ConsentRequired);

        if (apiKey.IsEmpty())
            return Result.Failure(DraftErrors.EmptyApiKey);

        await store.SetApiKey(new StoredOpenAiApiKey(apiKey.Trim(), CurrentConsentVersion));
        return Result.Success();
    }

    public async Task<Result> AuthorizeStoredApiKey(bool consentAccepted)
    {
        if (!consentAccepted)
            return Result.Failure(DraftErrors.ConsentRequired);

        var key = await store.GetApiKey();
        if (key is null)
            return Result.Failure(DraftErrors.MissingApiKey);

        if (key.ConsentVersion == CurrentConsentVersion)
            return Result.Success();

        return await SaveApiKey(key.Value, consentAccepted);
    }

    public void ClearApiKey() => store.ClearApiKey();
}
