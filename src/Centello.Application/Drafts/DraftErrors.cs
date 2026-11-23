namespace Centello.Application;

public static class DraftErrors
{
    public static readonly Error Canceled = new("Draft.Canceled", "Operazione annullata.");
    public static readonly Error NoInternet = new("Draft.NoInternet", "Serve una connessione a Internet per preparare la bozza.");
    public static readonly Error ConsentRequired = new("Draft.ConsentRequired", "Per usare voce, foto o PDF, attiva le bozze assistite nelle impostazioni.");
    public static readonly Error AccessDenied = new("Draft.AccessDenied", "Questo dispositivo non può accedere al servizio per le bozze.");
    public static readonly Error RateLimited = new("Draft.RateLimited", "Il servizio ha raggiunto il limite di richieste. Riprova più tardi.");
    public static readonly Error TimedOut = new("Draft.TimedOut", "Il servizio ha impiegato troppo tempo. Riprova.");
    public static readonly Error Unavailable = new("Draft.Unavailable", "Il servizio per le bozze assistite non è disponibile.");
    public static readonly Error Blocked = new("Draft.Blocked", "Il servizio non ha potuto elaborare questo contenuto. Puoi inserire la spesa a mano.");
    public static readonly Error MalformedResponse = new("Draft.MalformedResponse", "Non sono riuscito a leggere la risposta del servizio. Riprova.");
    public static readonly Error RequestFailed = new("Draft.RequestFailed", "Non sono riuscito a preparare la bozza. Riprova.");
    public static readonly Error RequestTooLarge = new("Draft.RequestTooLarge", "La richiesta è troppo grande. Prova con un file più piccolo.");
    public static readonly Error PdfTooManyPages = new("Draft.PdfTooManyPages", "Il PDF ha troppe pagine. Scegli un PDF con al massimo 1000 pagine.");
}
