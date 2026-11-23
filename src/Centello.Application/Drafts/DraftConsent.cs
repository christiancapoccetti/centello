namespace Centello.Application;

public interface IDraftConsentStore
{
    int AcceptedVersion { get; set; }
}

public sealed class DraftConsent(IDraftConsentStore store)
{
    public const int CurrentVersion = 1;

    public const string Disclosure =
        "Quando usi la voce, una foto o un PDF per preparare una bozza, Centello invia a Google tramite Firebase AI Logic " +
        "l’audio registrato, la foto o il PDF intero, i nomi di categorie e tag attivi e la data di oggi. " +
        "Scontrini e fatture possono contenere importi, acquisti e dati personali. " +
        "La registrazione è tenuta in memoria sul telefono prima dell’invio. Foto e PDF possono essere copiati nella cache temporanea. " +
        "Puoi disattivare gli invii dalle impostazioni e continuare a inserire le spese a mano.";

    public bool IsEnabled => store.AcceptedVersion == CurrentVersion;

    public void SetEnabled(bool enabled) => store.AcceptedVersion = enabled ? CurrentVersion : 0;
}
