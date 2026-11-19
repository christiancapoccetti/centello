using System.Text.Json;
using Centesimo.Application;

namespace Centesimo.Infrastructure.Tests;

public sealed class ExpenseInterpretationResponseParser_should_expected_behavior
{
    private static void AssertMalformed(Result<ExpenseInterpretation> result)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(OpenAiErrors.MalformedResponse, result.Error);
    }

    [Fact]
    public void Parse_a_completed_message_after_a_reasoning_item()
    {
        var body = OpenAiResponses.Interpretation(amount: "100", categoryKey: "c1", tagKey: "t2", date: "2026-11-18", note: "pieno");

        var result = ExpenseInterpretationResponseParser.Parse(body);

        Assert.True(result.IsSuccess);
        Assert.Equal(new ExpenseInterpretation(100m, "c1", "t2", "2026-11-18", "pieno"), result.Value);
    }

    [Fact]
    public void Parse_a_decimal_amount()
    {
        var result = ExpenseInterpretationResponseParser.Parse(OpenAiResponses.Interpretation(amount: "12.5"));

        Assert.Equal(12.5m, result.Value.Amount);
    }

    [Fact]
    public void Parse_explicit_nulls()
    {
        var result = ExpenseInterpretationResponseParser.Parse(
            OpenAiResponses.Interpretation(amount: "null", categoryKey: null, tagKey: null, date: null));

        Assert.True(result.IsSuccess);
        Assert.Equal(new ExpenseInterpretation(null, null, null, null, ""), result.Value);
    }

    [Fact]
    public void Reject_a_refusal()
    {
        AssertMalformed(ExpenseInterpretationResponseParser.Parse(OpenAiResponses.Refusal("Non posso aiutarti.")));
    }

    [Theory]
    [InlineData("incomplete")]
    [InlineData("failed")]
    [InlineData("in_progress")]
    public void Reject_a_response_that_is_not_completed(string status)
    {
        var body = OpenAiResponses.Interpretation().Replace("\"status\":\"completed\",\"output\"", $"\"status\":\"{status}\",\"output\"");

        Assert.Contains(status, body);
        AssertMalformed(ExpenseInterpretationResponseParser.Parse(body));
    }

    [Fact]
    public void Reject_a_response_without_status()
    {
        AssertMalformed(ExpenseInterpretationResponseParser.Parse("""{"output":[]}"""));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("")]
    [InlineData("{")]
    public void Reject_a_body_that_is_not_json(string body)
    {
        AssertMalformed(ExpenseInterpretationResponseParser.Parse(body));
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("\"completed\"")]
    [InlineData("null")]
    public void Reject_a_body_that_is_not_an_object(string body)
    {
        AssertMalformed(ExpenseInterpretationResponseParser.Parse(body));
    }

    [Fact]
    public void Reject_a_response_without_output_text()
    {
        var body = """{"status":"completed","output":[{"type":"message","content":[]}]}""";

        AssertMalformed(ExpenseInterpretationResponseParser.Parse(body));
    }

    [Fact]
    public void Reject_a_response_without_a_message()
    {
        var body = """{"status":"completed","output":[{"type":"reasoning","summary":[]}]}""";

        AssertMalformed(ExpenseInterpretationResponseParser.Parse(body));
    }

    [Fact]
    public void Reject_a_response_without_output()
    {
        AssertMalformed(ExpenseInterpretationResponseParser.Parse("""{"status":"completed"}"""));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("")]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("""{"amount":100""")]
    public void Reject_output_text_that_is_not_the_expected_object(string outputText)
    {
        AssertMalformed(ExpenseInterpretationResponseParser.Parse(OpenAiResponses.Completed(outputText)));
    }

    [Theory]
    [InlineData("""{"categoryKey":"c1","tagKey":null,"date":null,"note":""}""")]
    [InlineData("""{"amount":1,"tagKey":null,"date":null,"note":""}""")]
    [InlineData("""{"amount":1,"categoryKey":null,"date":null,"note":""}""")]
    [InlineData("""{"amount":1,"categoryKey":null,"tagKey":null,"note":""}""")]
    [InlineData("""{"amount":1,"categoryKey":null,"tagKey":null,"date":null}""")]
    public void Reject_output_with_a_missing_field(string outputText)
    {
        AssertMalformed(ExpenseInterpretationResponseParser.Parse(OpenAiResponses.Completed(outputText)));
    }

    [Fact]
    public void Reject_a_null_note()
    {
        var outputText = """{"amount":1,"categoryKey":null,"tagKey":null,"date":null,"note":null}""";

        AssertMalformed(ExpenseInterpretationResponseParser.Parse(OpenAiResponses.Completed(outputText)));
    }

    [Theory]
    [InlineData("""{"amount":"cento","categoryKey":null,"tagKey":null,"date":null,"note":""}""")]
    [InlineData("""{"amount":1,"categoryKey":5,"tagKey":null,"date":null,"note":""}""")]
    [InlineData("""{"amount":1,"categoryKey":null,"tagKey":null,"date":null,"note":7}""")]
    public void Reject_output_with_a_wrongly_typed_field(string outputText)
    {
        AssertMalformed(ExpenseInterpretationResponseParser.Parse(OpenAiResponses.Completed(outputText)));
    }

    [Fact]
    public void Use_the_first_output_text_when_several_are_present()
    {
        var first = JsonSerializer.Serialize(new { amount = 1, categoryKey = "c1", tagKey = (string?)null, date = (string?)null, note = "uno" });
        var second = JsonSerializer.Serialize(new { amount = 2, categoryKey = "c1", tagKey = (string?)null, date = (string?)null, note = "due" });
        var body = JsonSerializer.Serialize(new
        {
            status = "completed",
            output = new object[]
            {
                new
                {
                    type = "message",
                    content = new object[]
                    {
                        new { type = "output_text", text = first },
                        new { type = "output_text", text = second }
                    }
                }
            }
        });

        var result = ExpenseInterpretationResponseParser.Parse(body);

        Assert.Equal("uno", result.Value.Note);
    }

    [Fact]
    public void Skip_a_commentary_message_before_the_final_answer()
    {
        var answer = JsonSerializer.Serialize(new { amount = 100, categoryKey = "c1", tagKey = (string?)null, date = (string?)null, note = "" });
        var body = JsonSerializer.Serialize(new
        {
            status = "completed",
            output = new object[]
            {
                new { type = "message", phase = "commentary", content = new object[] { new { type = "output_text", text = "Analizzo la frase." } } },
                new { type = "message", phase = "final_answer", content = new object[] { new { type = "output_text", text = answer } } }
            }
        });

        var result = ExpenseInterpretationResponseParser.Parse(body);

        Assert.True(result.IsSuccess);
        Assert.Equal(100m, result.Value.Amount);
    }

    [Fact]
    public void Use_the_last_answer_message_when_several_are_present()
    {
        var draft = JsonSerializer.Serialize(new { amount = 1, categoryKey = "c1", tagKey = (string?)null, date = (string?)null, note = "bozza" });
        var answer = JsonSerializer.Serialize(new { amount = 2, categoryKey = "c1", tagKey = (string?)null, date = (string?)null, note = "finale" });
        var body = JsonSerializer.Serialize(new
        {
            status = "completed",
            output = new object[]
            {
                new { type = "message", content = new object[] { new { type = "output_text", text = draft } } },
                new { type = "message", content = new object[] { new { type = "output_text", text = answer } } }
            }
        });

        var result = ExpenseInterpretationResponseParser.Parse(body);

        Assert.Equal("finale", result.Value.Note);
    }
}
