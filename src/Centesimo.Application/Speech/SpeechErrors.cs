namespace Centesimo.Application;

public static class SpeechErrors
{
    public static readonly Error EmptyApiKey = new("Speech.EmptyApiKey", "Inserisci la chiave API OpenAI.");
    public static readonly Error ConsentRequired = new("Speech.ConsentRequired", "Per usare OpenAI serve il tuo consenso.");
}
