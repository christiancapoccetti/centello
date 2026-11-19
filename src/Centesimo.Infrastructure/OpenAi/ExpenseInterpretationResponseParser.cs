using System.Text.Json;
using Centesimo.Application;

namespace Centesimo.Infrastructure;

/// <summary>Extracts the structured expense from a Responses API body. Anything but a complete, well-formed answer is a failure.</summary>
public static class ExpenseInterpretationResponseParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true
    };

    public static Result<ExpenseInterpretation> Parse(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            var text = FindOutputText(document.RootElement);
            if (text is null)
                return Result<ExpenseInterpretation>.Failure(OpenAiErrors.MalformedResponse);

            var interpretation = JsonSerializer.Deserialize<ExpenseInterpretation>(text, JsonOptions);
            return interpretation is null
                ? Result<ExpenseInterpretation>.Failure(OpenAiErrors.MalformedResponse)
                : Result<ExpenseInterpretation>.Success(interpretation);
        }
        catch (JsonException)
        {
            return Result<ExpenseInterpretation>.Failure(OpenAiErrors.MalformedResponse);
        }
    }

    private static string? FindOutputText(JsonElement response)
    {
        if (GetString(response, "status") != "completed")
            return null;

        // The model can emit commentary messages before the final answer; only the last answer message carries the schema.
        var answer = GetItems(response, "output")
            .LastOrDefault(item => GetString(item, "type") == "message" && GetString(item, "phase") != "commentary");
        var parts = GetItems(answer, "content").ToList();
        if (parts.Any(part => GetString(part, "type") == "refusal"))
            return null;

        return parts
            .Where(part => GetString(part, "type") == "output_text")
            .Select(part => GetString(part, "text"))
            .FirstOrDefault(text => text is not null);
    }

    private static string? GetString(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static IEnumerable<JsonElement> GetItems(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray()
            : [];
}
