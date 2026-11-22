using Centello.Domain;

namespace Centello.Application;

public interface IOpenAiApiKeyProvider
{
    /// <returns>The API key, or <c>""</c> when none is configured.</returns>
    Task<string> GetApiKey();
}

/// <summary>Owns the rules for the OpenAI key, so it can never be saved without the user's consent.</summary>
public sealed class OpenAiSettings(IOpenAiApiKeyStore store) : IOpenAiApiKeyProvider
{
    public const string ConsentMessage =
        "Quando detti una spesa, Centello invia a OpenAI l'audio registrato e i nomi di categorie e tag attivi per la trascrizione. " +
        "Per preparare la bozza, invia il testo trascritto oppure la foto o il PDF che hai scelto, insieme ai nomi delle categorie e dei tag attivi e alla data di oggi. " +
        "Scontrini e fatture possono contenere importi, acquisti e dati personali. I PDF vengono inviati per intero. " +
        "Le richieste usano la tua chiave API, salvata sul telefono. Centello tiene l'audio in memoria e può usare la cache del telefono per foto e PDF. " +
        "Puoi inserire le spese a mano senza OpenAI e disattivare questi invii rimuovendo la chiave.";

    public Task<string> GetApiKey() => store.GetApiKey();

    public async Task<bool> HasApiKey() => (await store.GetApiKey()).HasValue();

    public async Task<Result> SaveApiKey(string apiKey, bool consentAccepted)
    {
        if (!consentAccepted)
            return Result.Failure(DraftErrors.ConsentRequired);

        if (apiKey.IsEmpty())
            return Result.Failure(DraftErrors.EmptyApiKey);

        await store.SetApiKey(apiKey.Trim());
        return Result.Success();
    }

    public void ClearApiKey() => store.ClearApiKey();
}
