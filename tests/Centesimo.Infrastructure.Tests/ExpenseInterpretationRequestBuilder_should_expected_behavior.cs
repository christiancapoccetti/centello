using System.Text.Json.Nodes;
using Centesimo.Application;

namespace Centesimo.Infrastructure.Tests;

public sealed class ExpenseInterpretationRequestBuilder_should_expected_behavior
{
    private static readonly DateOnly Today = new(2026, 11, 19);

    private static readonly ExpenseInterpretationCatalog Catalog = new(
    [
        new DraftCategory(Guid.NewGuid(), "Auto", [new DraftTag(Guid.NewGuid(), "Carburante"), new DraftTag(Guid.NewGuid(), "Tagliando")]),
        new DraftCategory(Guid.NewGuid(), "Casa", [new DraftTag(Guid.NewGuid(), "Affitto")])
    ]);

    private static JsonObject Build(ExpenseInterpretationCatalog? catalog = null, string text = "Ho speso 100 euro di carburante") =>
        ExpenseInterpretationRequestBuilder.Build(text, catalog ?? Catalog, Today);

    private static JsonObject Schema(JsonObject request) =>
        request["text"]!["format"]!["schema"]!.AsObject();

    private static IReadOnlyList<string?> AllowedValues(JsonObject request, string property) =>
        [.. Schema(request)["properties"]![property]!["enum"]!.AsArray().Select(node => node?.GetValue<string>())];

    private static string SystemInstructions(JsonObject request) =>
        request["input"]![0]!["content"]!.GetValue<string>();

    private static readonly byte[] PhotoBytes = [0xFF, 0xD8, 0xFF, 0xE0, 0x01, 0x02, 0x03];

    private static JsonObject BuildPhoto(ExpenseInterpretationCatalog? catalog = null) =>
        ExpenseInterpretationRequestBuilder.Build(ExpenseDocument.Read(PhotoBytes).Value, catalog ?? Catalog, Today);

    [Fact]
    public void Target_the_interpretation_model_without_reasoning_or_storage()
    {
        var request = Build();

        Assert.Equal("gpt-6-luna", request["model"]!.GetValue<string>());
        Assert.Equal("none", request["reasoning"]!["effort"]!.GetValue<string>());
        Assert.False(request["store"]!.GetValue<bool>());
    }

    [Fact]
    public void Send_the_instructions_then_the_user_text()
    {
        var request = Build(text: "Ho speso 100 euro di carburante");

        var input = request["input"]!.AsArray();
        Assert.Equal(2, input.Count);
        Assert.Equal("system", input[0]!["role"]!.GetValue<string>());
        Assert.Equal("user", input[1]!["role"]!.GetValue<string>());
        Assert.Equal("Ho speso 100 euro di carburante", input[1]!["content"]!.GetValue<string>());
    }

    [Fact]
    public void Request_a_strict_json_schema_output()
    {
        var format = Build()["text"]!["format"]!;

        Assert.Equal("json_schema", format["type"]!.GetValue<string>());
        Assert.Equal("expense_draft", format["name"]!.GetValue<string>());
        Assert.True(format["strict"]!.GetValue<bool>());
    }

    [Fact]
    public void Require_every_property_and_forbid_extra_ones()
    {
        var schema = Schema(Build());

        Assert.Equal(
            ["amount", "categoryKey", "tagKey", "date", "note"],
            schema["required"]!.AsArray().Select(node => node!.GetValue<string>()));
        Assert.Equal(
            ["amount", "categoryKey", "tagKey", "date", "note"],
            schema["properties"]!.AsObject().Select(property => property.Key));
        Assert.False(schema["additionalProperties"]!.GetValue<bool>());
    }

    [Fact]
    public void Admit_only_the_catalog_category_keys_or_null()
    {
        Assert.Equal(["c1", "c2", null], AllowedValues(Build(), "categoryKey"));
    }

    [Fact]
    public void Admit_only_the_catalog_tag_keys_or_null()
    {
        Assert.Equal(["t1", "t2", "t3", null], AllowedValues(Build(), "tagKey"));
    }

    [Fact]
    public void Allow_null_for_every_optional_field()
    {
        var properties = Schema(Build())["properties"]!;

        Assert.Equal(["number", "null"], properties["amount"]!["type"]!.AsArray().Select(node => node!.GetValue<string>()));
        Assert.Equal(["string", "null"], properties["categoryKey"]!["type"]!.AsArray().Select(node => node!.GetValue<string>()));
        Assert.Equal(["string", "null"], properties["tagKey"]!["type"]!.AsArray().Select(node => node!.GetValue<string>()));
        Assert.Equal(["string", "null"], properties["date"]!["type"]!.AsArray().Select(node => node!.GetValue<string>()));
        Assert.Equal("string", properties["note"]!["type"]!.GetValue<string>());
    }

    [Fact]
    public void Admit_only_null_tags_when_the_catalog_has_none()
    {
        var catalog = new ExpenseInterpretationCatalog([new DraftCategory(Guid.NewGuid(), "Auto", [])]);

        var request = Build(catalog);

        Assert.Equal(["c1", null], AllowedValues(request, "categoryKey"));
        Assert.Equal([null], AllowedValues(request, "tagKey"));
    }

    [Fact]
    public void Admit_only_null_keys_without_categories()
    {
        var request = Build(new ExpenseInterpretationCatalog([]));

        Assert.Equal([null], AllowedValues(request, "categoryKey"));
        Assert.Equal([null], AllowedValues(request, "tagKey"));
        Assert.Contains("(nessuna categoria)", SystemInstructions(request));
    }

    [Fact]
    public void Tell_the_model_todays_date_and_weekday()
    {
        var instructions = SystemInstructions(Build());

        Assert.Contains("2026-11-19", instructions);
        Assert.Contains("giovedì", instructions);
    }

    [Fact]
    public void List_the_catalog_with_its_keys_in_the_instructions()
    {
        var instructions = SystemInstructions(Build());

        foreach (var line in Catalog.PromptLines)
            Assert.Contains(line, instructions);
    }

    [Fact]
    public void Ask_for_null_instead_of_guessing()
    {
        var instructions = SystemInstructions(Build());

        Assert.Contains("Non indovinare", instructions);
        Assert.Contains("null", instructions);
    }

    [Fact]
    public void Serialize_to_valid_json_with_non_ascii_names_intact()
    {
        var catalog = new ExpenseInterpretationCatalog([new DraftCategory(Guid.NewGuid(), "Città \"vecchia\"", [])]);

        var reparsed = JsonNode.Parse(Build(catalog).ToJsonString())!.AsObject();

        Assert.Contains("c1: Città \"vecchia\"", SystemInstructions(reparsed));
    }

    [Fact]
    public void Send_the_photo_as_an_image_data_url()
    {
        var input = BuildPhoto()["input"]!.AsArray();

        Assert.Equal(2, input.Count);
        Assert.Equal("system", input[0]!["role"]!.GetValue<string>());
        Assert.Equal("user", input[1]!["role"]!.GetValue<string>());
        var part = Assert.Single(input[1]!["content"]!.AsArray())!;
        Assert.Equal("input_image", part["type"]!.GetValue<string>());
        Assert.Equal("high", part["detail"]!.GetValue<string>());
        var url = part["image_url"]!.GetValue<string>();
        const string prefix = "data:image/jpeg;base64,";
        Assert.StartsWith(prefix, url);
        Assert.Equal(PhotoBytes, Convert.FromBase64String(url[prefix.Length..]));
    }

    [Fact]
    public void Use_the_same_schema_for_a_photo_and_a_phrase()
    {
        Assert.Equal(
            Build()["text"]!["format"]!.ToJsonString(),
            BuildPhoto()["text"]!["format"]!.ToJsonString());
    }

    [Fact]
    public void Ask_for_the_total_paid_on_a_receipt()
    {
        var instructions = SystemInstructions(BuildPhoto());

        Assert.Contains("TOTALE", instructions);
        Assert.Contains("in euro", instructions);
        Assert.DoesNotContain("frase in italiano", instructions);
    }

    [Fact]
    public void Ask_for_null_instead_of_guessing_on_a_photo()
    {
        var instructions = SystemInstructions(BuildPhoto());

        Assert.Contains("Non indovinare", instructions);
        Assert.Contains("null", instructions);
        Assert.Contains("Non usare la data di oggi.", instructions);
    }

    [Fact]
    public void Tell_the_model_todays_date_for_a_photo()
    {
        var instructions = SystemInstructions(BuildPhoto());

        Assert.Contains("2026-11-19", instructions);
        Assert.Contains("giovedì", instructions);
    }

    [Fact]
    public void List_the_catalog_for_a_photo()
    {
        var instructions = SystemInstructions(BuildPhoto());

        foreach (var line in Catalog.PromptLines)
            Assert.Contains(line, instructions);
    }

    [Fact]
    public void Admit_only_null_keys_for_a_photo_without_categories()
    {
        var request = BuildPhoto(new ExpenseInterpretationCatalog([]));

        Assert.Equal([null], AllowedValues(request, "categoryKey"));
        Assert.Contains("(nessuna categoria)", SystemInstructions(request));
    }

    [Fact]
    public void Keep_the_voice_request_exactly_as_it_is()
    {
        const string instructions = """
            Compili la bozza di una spesa a partire da una frase in italiano, scritta o dettata.
            Oggi è giovedì 2026-11-19.

            Categorie e tag dell'utente. Usa solo queste chiavi:
            (nessuna categoria)

            Regole:
            - amount: importo in euro come numero. "cento euro" è 100, "12,50" è 12.5. null se manca o non è chiaro.
            - categoryKey: chiave della categoria che corrisponde chiaramente alla frase. null se nessuna corrisponde o se più categorie sono plausibili. Non indovinare.
            - tagKey: chiave di un tag della categoria scelta, solo se corrisponde chiaramente. Altrimenti null.
            - date: data della spesa nel formato AAAA-MM-GG. Risolvi espressioni come "ieri" o "lunedì scorso" a partire da oggi. null se la frase non indica una data.
            - note: solo i dettagli che non sono già importo, categoria, tag o data. Altrimenti stringa vuota.
            """;
        var expected = new JsonObject
        {
            ["model"] = "gpt-6-luna",
            ["reasoning"] = new JsonObject { ["effort"] = "none" },
            ["store"] = false,
            ["input"] = new JsonArray(
                new JsonObject { ["role"] = "system", ["content"] = instructions },
                new JsonObject { ["role"] = "user", ["content"] = "ieri 12 euro di pizza" }),
            ["text"] = new JsonObject
            {
                ["format"] = new JsonObject
                {
                    ["type"] = "json_schema",
                    ["name"] = "expense_draft",
                    ["strict"] = true,
                    ["schema"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject
                        {
                            ["amount"] = new JsonObject { ["type"] = new JsonArray("number", "null") },
                            ["categoryKey"] = new JsonObject { ["type"] = new JsonArray("string", "null"), ["enum"] = new JsonArray((JsonNode?)null) },
                            ["tagKey"] = new JsonObject { ["type"] = new JsonArray("string", "null"), ["enum"] = new JsonArray((JsonNode?)null) },
                            ["date"] = new JsonObject { ["type"] = new JsonArray("string", "null") },
                            ["note"] = new JsonObject { ["type"] = "string" }
                        },
                        ["required"] = new JsonArray("amount", "categoryKey", "tagKey", "date", "note"),
                        ["additionalProperties"] = false
                    }
                }
            }
        };

        var request = Build(new ExpenseInterpretationCatalog([]), "ieri 12 euro di pizza");

        Assert.Equal(expected.ToJsonString(), request.ToJsonString());
    }

    private static readonly byte[] PdfBytes = [.. "%PDF-1.7"u8, 0x0A, 0x01, 0x02];

    private static JsonObject BuildPdf(ExpenseInterpretationCatalog? catalog = null) =>
        ExpenseInterpretationRequestBuilder.Build(ExpenseDocument.Read(PdfBytes).Value, catalog ?? Catalog, Today);

    [Fact]
    public void Send_a_pdf_as_a_file_data_url()
    {
        var input = BuildPdf()["input"]!.AsArray();

        Assert.Equal(2, input.Count);
        var part = Assert.Single(input[1]!["content"]!.AsArray())!;
        Assert.Equal("input_file", part["type"]!.GetValue<string>());
        Assert.Equal("spesa.pdf", part["filename"]!.GetValue<string>());
        Assert.Equal("high", part["detail"]!.GetValue<string>());
        Assert.Null(part["image_url"]);
        var url = part["file_data"]!.GetValue<string>();
        const string prefix = "data:application/pdf;base64,";
        Assert.StartsWith(prefix, url);
        Assert.Equal(PdfBytes, Convert.FromBase64String(url[prefix.Length..]));
    }

    [Fact]
    public void Use_the_same_schema_for_a_phrase_a_photo_and_a_pdf()
    {
        var phrase = Build()["text"]!["format"]!.ToJsonString();

        Assert.Equal(phrase, BuildPhoto()["text"]!["format"]!.ToJsonString());
        Assert.Equal(phrase, BuildPdf()["text"]!["format"]!.ToJsonString());
    }

    [Fact]
    public void Name_the_pdf_in_the_document_instructions()
    {
        const string intro = "dalla foto o dal PDF di uno scontrino, di una ricevuta, di una fattura o della schermata di un pagamento.";

        Assert.Contains(intro, SystemInstructions(BuildPdf()));
        Assert.Contains(intro, SystemInstructions(BuildPhoto()));
        Assert.DoesNotContain("PDF", SystemInstructions(Build()));
    }

    [Fact]
    public void Give_a_pdf_the_same_instructions_as_a_photo()
    {
        Assert.Equal(SystemInstructions(BuildPhoto()), SystemInstructions(BuildPdf()));
    }
}
