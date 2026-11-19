namespace Centesimo.Application;

public static class SpeechErrors
{
    public static readonly Error Canceled = new("Speech.Canceled", "Operazione annullata.");
    public static readonly Error EmptyTranscription = new("Speech.EmptyTranscription", "Non ho capito il comando. Riprova.");
    public static readonly Error MissingApiKey = new("Speech.MissingApiKey", "Configura la chiave API OpenAI nelle impostazioni.");
    public static readonly Error EmptyApiKey = new("Speech.EmptyApiKey", "Inserisci la chiave API OpenAI.");
    public static readonly Error ConsentRequired = new("Speech.ConsentRequired", "Per usare OpenAI serve il tuo consenso.");
}
