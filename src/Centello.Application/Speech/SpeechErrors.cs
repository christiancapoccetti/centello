namespace Centello.Application;

public static class SpeechErrors
{
    public static readonly Error EmptyTranscription = new("Speech.EmptyTranscription", "Non ho capito il comando. Riprova.");
    public static readonly Error MicrophoneDenied = new("Speech.MicrophoneDenied", "Consenti l'accesso al microfono per usare i comandi vocali.");
    public static readonly Error StartFailed = new("Speech.StartFailed", "Non riesco ad avviare il riconoscimento vocale.");
    public static readonly Error ProcessingFailed = new("Speech.ProcessingFailed", "Non riesco a elaborare il comando vocale.");
}
