using System.Text.Json;

namespace Centesimo.Infrastructure.Tests;

internal static class OpenAiResponses
{
    /// <summary>A completed Responses API body whose message carries <paramref name="outputText"/>, after a reasoning item.</summary>
    public static string Completed(string outputText) => JsonSerializer.Serialize(new
    {
        id = "resp_1",
        status = "completed",
        output = new object[]
        {
            new { type = "reasoning", id = "rs_1", summary = Array.Empty<object>() },
            new
            {
                type = "message",
                id = "msg_1",
                status = "completed",
                role = "assistant",
                content = new object[] { new { type = "output_text", text = outputText, annotations = Array.Empty<object>() } }
            }
        }
    });

    public static string Refusal(string refusal) => JsonSerializer.Serialize(new
    {
        id = "resp_1",
        status = "completed",
        output = new object[]
        {
            new
            {
                type = "message",
                id = "msg_1",
                status = "completed",
                role = "assistant",
                content = new object[] { new { type = "refusal", refusal } }
            }
        }
    });

    public static string Interpretation(
        string amount = "100",
        string? categoryKey = "c1",
        string? tagKey = "t1",
        string? date = null,
        string note = "") =>
        Completed(JsonSerializer.Serialize(new
        {
            amount = JsonDocument.Parse(amount).RootElement,
            categoryKey,
            tagKey,
            date,
            note
        }));
}
