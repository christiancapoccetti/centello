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
}
