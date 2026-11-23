using System.Globalization;
using Centello.Application;

namespace Centello.Infrastructure;

public static class ExpenseInterpretationRequestBuilder
{
    private static readonly string[] ItalianDays = ["domenica", "lunedì", "martedì", "mercoledì", "giovedì", "venerdì", "sabato"];

    private static string Day(DateOnly today) =>
        $"{ItalianDays[(int)today.DayOfWeek]} {today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

    private static string Keys(ExpenseInterpretationCatalog catalog) =>
        catalog.PromptLines.Count == 0
            ? "(nessuna categoria)"
            : string.Join('\n', catalog.PromptLines);

    public static string Voice(ExpenseInterpretationCatalog catalog, DateOnly today) => $"""
        Compili la bozza di una spesa dall’audio di una frase dettata in italiano.
        Oggi è {Day(today)}.

        Categorie e tag dell'utente. Usa solo queste chiavi:
        {Keys(catalog)}

        Regole:
        - heard: trascrivi solo ciò che senti. Se non si capiscono parole, restituisci null e lascia vuoti gli altri campi.
        - amount: importo in euro come numero. "cento euro" è 100, "12,50" è 12.5. null se manca o non è chiaro.
        - categoryKey: chiave della categoria che corrisponde chiaramente alla frase. null se nessuna corrisponde o se più categorie sono plausibili. Non indovinare.
        - tagKey: chiave di un tag della categoria scelta, solo se corrisponde chiaramente. Altrimenti null.
        - date: data della spesa nel formato AAAA-MM-GG. Risolvi espressioni come "ieri" o "lunedì scorso" a partire da oggi. null se la frase non indica una data.
        - note: solo i dettagli che non sono già importo, categoria, tag o data. Altrimenti stringa vuota.
        """;

    public static string Document(ExpenseInterpretationCatalog catalog, DateOnly today) => $"""
        Compili la bozza di una spesa a partire dalla foto o dal PDF di uno scontrino, di una ricevuta, di una fattura o della schermata di un pagamento.
        Oggi è {Day(today)}.

        Categorie e tag dell'utente. Usa solo queste chiavi:
        {Keys(catalog)}

        Regole:
        - heard: null.
        - Un documento è una sola spesa. Se ne mostra più di una, usa quella più in evidenza.
        - amount: il totale effettivamente pagato, in euro, come numero (di solito la riga TOTALE, TOTALE EURO o IMPORTO PAGATO). Non usare subtotali, righe IVA, sconti, contanti versati o resto. null se il totale non è leggibile o se la valuta non è l'euro.
        - categoryKey: chiave della categoria che corrisponde chiaramente al negozio o a ciò che è stato comprato. null se nessuna corrisponde o se più categorie sono plausibili. Non indovinare.
        - tagKey: chiave di un tag della categoria scelta, solo se corrisponde chiaramente. Altrimenti null.
        - date: data del documento nel formato AAAA-MM-GG. Sui documenti italiani la data è scritta GG/MM/AAAA. null se manca o non è leggibile. Non usare la data di oggi.
        - note: il nome del negozio o del fornitore, se leggibile. Altrimenti stringa vuota.
        - Se il documento non mostra una spesa, tutti i campi sono null e note è una stringa vuota.
        """;
}
