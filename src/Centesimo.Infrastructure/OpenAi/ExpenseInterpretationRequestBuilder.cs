using System.Globalization;
using System.Text.Json.Nodes;
using Centesimo.Application;

namespace Centesimo.Infrastructure;

/// <summary>Builds the Responses API request: instructions listing the user's keys, and a strict schema that only admits those keys.</summary>
public static class ExpenseInterpretationRequestBuilder
{
    private static readonly string[] ItalianDays = ["domenica", "lunedì", "martedì", "mercoledì", "giovedì", "venerdì", "sabato"];

    public static JsonObject Build(string text, ExpenseInterpretationCatalog catalog, DateOnly today) => new()
    {
        ["model"] = OpenAiModels.Interpretation,
        ["reasoning"] = new JsonObject { ["effort"] = "none" },
        ["store"] = false,
        ["input"] = new JsonArray(
            Message("system", Instructions(catalog, today)),
            Message("user", text)),
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

    private static JsonObject Message(string role, string content) => new()
    {
        ["role"] = role,
        ["content"] = content
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

    private static string Instructions(ExpenseInterpretationCatalog catalog, DateOnly today)
    {
        var date = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var day = ItalianDays[(int)today.DayOfWeek];
        var keys = catalog.PromptLines.Count == 0
            ? "(nessuna categoria)"
            : string.Join('\n', catalog.PromptLines);
        return $"""
            Compili la bozza di una spesa a partire da una frase in italiano, scritta o dettata.
            Oggi è {day} {date}.

            Categorie e tag dell'utente. Usa solo queste chiavi:
            {keys}

            Regole:
            - amount: importo in euro come numero. "cento euro" è 100, "12,50" è 12.5. null se manca o non è chiaro.
            - categoryKey: chiave della categoria che corrisponde chiaramente alla frase. null se nessuna corrisponde o se più categorie sono plausibili. Non indovinare.
            - tagKey: chiave di un tag della categoria scelta, solo se corrisponde chiaramente. Altrimenti null.
            - date: data della spesa nel formato AAAA-MM-GG. Risolvi espressioni come "ieri" o "lunedì scorso" a partire da oggi. null se la frase non indica una data.
            - note: solo i dettagli che non sono già importo, categoria, tag o data. Altrimenti stringa vuota.
            """;
    }
}
