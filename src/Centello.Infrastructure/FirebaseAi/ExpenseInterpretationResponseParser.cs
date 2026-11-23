using System.Text.Json;
using Centello.Application;

namespace Centello.Infrastructure;

public static class ExpenseInterpretationResponseParser
{
    public static Result<(ExpenseInterpretation Interpretation, string Heard)> Parse(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var amount = root.GetProperty("amount");
            var interpretation = new ExpenseInterpretation(
                amount.ValueKind == JsonValueKind.Null ? null : amount.GetDecimal(),
                root.GetProperty("categoryKey").GetString(),
                root.GetProperty("tagKey").GetString(),
                root.GetProperty("date").GetString(),
                root.GetProperty("note").GetString() ?? "");
            var heard = root.GetProperty("heard").GetString()?.Trim() ?? "";
            return Result<(ExpenseInterpretation, string)>.Success((interpretation, heard));
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException or FormatException)
        {
            return Result<(ExpenseInterpretation, string)>.Failure(DraftErrors.MalformedResponse);
        }
    }
}
