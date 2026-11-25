namespace Centello.Application;

public static class DraftErrors
{
    public static readonly Error Canceled = new("Draft.Canceled", "Operazione annullata.");
    public static readonly Error NoInternet = new("Draft.NoInternet", "Serve una connessione a Internet per preparare la bozza. Puoi inserire la spesa a mano.");
    public static readonly Error ConsentRequired = new("Draft.ConsentRequired", "Per usare voce, foto o PDF, attiva «Inserimento spese con l’AI» nelle impostazioni.");
    public static readonly Error AccessDenied = new("Draft.AccessDenied", AiUnavailableMessage);
    public static readonly Error RateLimited = new("Draft.RateLimited", "L’inserimento con l’AI ha raggiunto il limite di richieste. Puoi inserire la spesa a mano o riprovare più tardi.");
    public static readonly Error TimedOut = new("Draft.TimedOut", "L’inserimento con l’AI non ha risposto in tempo. Puoi inserire la spesa a mano.");
    public static readonly Error Unavailable = new("Draft.Unavailable", AiUnavailableMessage);
    public static readonly Error TrialExhausted = new("Draft.TrialExhausted", "Hai usato le cinque bozze gratuite con l’AI. Puoi inserire la spesa a mano.");
    public static readonly Error InviteVerificationUnavailable = new("Draft.InviteVerificationUnavailable", "Non è possibile verificare l’invito AI in questo momento. Puoi inserire la spesa a mano.");
    public static readonly Error InvalidInvite = new("Draft.InvalidInvite", "Questo codice di invito non è valido.");
    public static readonly Error InviteDeviceMismatch = new("Draft.InviteDeviceMismatch", "Questo invito è destinato a un’altra installazione di Centello.");
    public static readonly Error InviteRevoked = new("Draft.InviteRevoked", "Questo invito AI è stato revocato. Puoi inserire la spesa a mano.");
    public static readonly Error Blocked = new("Draft.Blocked", "Il servizio non ha potuto elaborare questo contenuto. Puoi inserire la spesa a mano.");
    public static readonly Error MalformedResponse = new("Draft.MalformedResponse", "Non sono riuscito a leggere la risposta del servizio. Riprova.");
    public static readonly Error RequestFailed = new("Draft.RequestFailed", "Non sono riuscito a preparare la bozza. Riprova.");
    public static readonly Error RequestTooLarge = new("Draft.RequestTooLarge", "La richiesta è troppo grande. Prova con un file più piccolo.");
    public static readonly Error PdfTooManyPages = new("Draft.PdfTooManyPages", "Il PDF ha troppe pagine. Scegli un PDF con al massimo 1000 pagine.");

    private const string AiUnavailableMessage = "L’inserimento con l’AI non è disponibile. Puoi inserire la spesa a mano.";
}
