using System.Net;
using Centesimo.Application;

namespace Centesimo.Infrastructure;

public static class OpenAiErrors
{
    public static readonly Error InvalidApiKey = new("OpenAi.InvalidApiKey", "La chiave API OpenAI non è valida.");
    public static readonly Error RateLimited = new("OpenAi.RateLimited", "Hai raggiunto il limite di OpenAI. Riprova più tardi.");
    public static readonly Error Unreachable = new("OpenAi.Unreachable", "OpenAI non è raggiungibile. Controlla la connessione.");
    public static readonly Error RequestFailed = new("OpenAi.RequestFailed", "OpenAI non ha risposto correttamente. Riprova.");
    public static readonly Error MalformedResponse = new("OpenAi.MalformedResponse", "Non ho capito la risposta di OpenAI. Riprova.");

    public static Error FromStatus(HttpStatusCode status) => status switch
    {
        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => InvalidApiKey,
        HttpStatusCode.TooManyRequests => RateLimited,
        _ => RequestFailed
    };
}
