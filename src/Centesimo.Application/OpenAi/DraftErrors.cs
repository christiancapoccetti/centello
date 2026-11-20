namespace Centesimo.Application;

public static class DraftErrors
{
    public static readonly Error Canceled = new("Draft.Canceled", "Operazione annullata.");
    public static readonly Error NoInternet = new("Draft.NoInternet", "Serve una connessione a Internet per usare OpenAI.");
    public static readonly Error MissingApiKey = new("Draft.MissingApiKey", "Configura la chiave API OpenAI nelle impostazioni.");
    public static readonly Error EmptyApiKey = new("Draft.EmptyApiKey", "Inserisci la chiave API OpenAI.");
    public static readonly Error ConsentRequired = new("Draft.ConsentRequired", "Per usare OpenAI serve il tuo consenso.");
}
