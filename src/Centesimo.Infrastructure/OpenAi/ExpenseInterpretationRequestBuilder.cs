using System.Globalization;
using System.Text.Json.Nodes;
using Centesimo.Application;

namespace Centesimo.Infrastructure;

public static class ExpenseInterpretationRequestBuilder
{
    private static readonly string[] ItalianDays = ["domenica", "lunedì", "martedì", "mercoledì", "giovedì", "venerdì", "sabato"];

    public static JsonObject Build(string text, ExpenseInterpretationCatalog catalog, DateOnly today) =>
        Request(PhraseInstructions(catalog, today), text, catalog);

    public static JsonObject Build(ExpensePhoto photo, ExpenseInterpretationCatalog catalog, DateOnly today) =>
        Request(PhotoInstructions(catalog, today), new JsonArray(ImagePart(photo)), catalog);

    private static JsonObject Request(string instructions, JsonNode userContent, ExpenseInterpretationCatalog catalog) => new()
    {
        ["model"] = OpenAiModels.Interpretation,
        ["reasoning"] = new JsonObject { ["effort"] = "none" },
        ["store"] = false,
        ["input"] = new JsonArray(
            Message("system", instructions),
            Message("user", userContent)),
        ["text"] = new JsonObject
        {
            ["format"] = new JsonObject
            {
                ["type"] = "json_schema",
                ["name"] = "expense_draft",
                ["strict"] = true,
                ["schema"] = Schema(catalog)
            }
        }
    };

    private static JsonObject Message(string role, JsonNode content) => new()
    {
        ["role"] = role,
        ["content"] = content
    };

    // Receipts print small; "high" keeps the total legible after OpenAI scales the image.
    private static JsonObject ImagePart(ExpensePhoto photo) => new()
    {
        ["type"] = "input_image",
        ["image_url"] = $"data:{photo.MediaType};base64,{Convert.ToBase64String(photo.Bytes.Span)}",
        ["detail"] = "high"
    };

    // Strict structured outputs require every property to be listed as required and nullable ones to admit "null".
    private static JsonObject Schema(ExpenseInterpretationCatalog catalog) => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["amount"] = new JsonObject { ["type"] = new JsonArray("number", "null") },
            ["categoryKey"] = NullableKey(catalog.CategoryKeys),
            ["tagKey"] = NullableKey(catalog.TagKeys),
            ["date"] = new JsonObject { ["type"] = new JsonArray("string", "null") },
            ["note"] = new JsonObject { ["type"] = "string" }
        },
        ["required"] = new JsonArray("amount", "categoryKey", "tagKey", "date", "note"),
        ["additionalProperties"] = false
    };

    private static JsonObject NullableKey(IReadOnlyList<string> keys)
    {
        var allowed = new JsonArray();
        foreach (var key in keys)
            allowed.Add(key);

        allowed.Add(null);
        return new JsonObject
        {
            ["type"] = new JsonArray("string", "null"),
            ["enum"] = allowed
        };
    }

    private static string Day(DateOnly today) =>
        $"{ItalianDays[(int)today.DayOfWeek]} {today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

    private static string Keys(ExpenseInterpretationCatalog catalog) =>
        catalog.PromptLines.Count == 0
            ? "(nessuna categoria)"
            : string.Join('\n', catalog.PromptLines);

    private static string PhraseInstructions(ExpenseInterpretationCatalog catalog, DateOnly today) => $"""
        Compili la bozza di una spesa a partire da una frase in italiano, scritta o dettata.
        Oggi è {Day(today)}.

        Categorie e tag dell'utente. Usa solo queste chiavi:
        {Keys(catalog)}

        Regole:
        - amount: importo in euro come numero. "cento euro" è 100, "12,50" è 12.5. null se manca o non è chiaro.
        - categoryKey: chiave della categoria che corrisponde chiaramente alla frase. null se nessuna corrisponde o se più categorie sono plausibili. Non indovinare.
        - tagKey: chiave di un tag della categoria scelta, solo se corrisponde chiaramente. Altrimenti null.
        - date: data della spesa nel formato AAAA-MM-GG. Risolvi espressioni come "ieri" o "lunedì scorso" a partire da oggi. null se la frase non indica una data.
        - note: solo i dettagli che non sono già importo, categoria, tag o data. Altrimenti stringa vuota.
        """;

    private static string PhotoInstructions(ExpenseInterpretationCatalog catalog, DateOnly today) => $"""
        Compili la bozza di una spesa a partire dalla foto di uno scontrino, di una ricevuta, di una fattura o della schermata di un pagamento.
        Oggi è {Day(today)}.

        Categorie e tag dell'utente. Usa solo queste chiavi:
        {Keys(catalog)}

        Regole:
        - Una foto è una sola spesa. Se ne mostra più di una, usa quella più in evidenza.
        - amount: il totale effettivamente pagato, in euro, come numero (di solito la riga TOTALE, TOTALE EURO o IMPORTO PAGATO). Non usare subtotali, righe IVA, sconti, contanti versati o resto. null se il totale non è leggibile o se la valuta non è l'euro.
        - categoryKey: chiave della categoria che corrisponde chiaramente al negozio o a ciò che è stato comprato. null se nessuna corrisponde o se più categorie sono plausibili. Non indovinare.
        - tagKey: chiave di un tag della categoria scelta, solo se corrisponde chiaramente. Altrimenti null.
        - date: data del documento nel formato AAAA-MM-GG. Sui documenti italiani la data è scritta GG/MM/AAAA. null se manca o non è leggibile. Non usare la data di oggi.
        - note: il nome del negozio o del fornitore, se leggibile. Altrimenti stringa vuota.
        - Se la foto non mostra una spesa, tutti i campi sono null e note è una stringa vuota.
        """;
}
