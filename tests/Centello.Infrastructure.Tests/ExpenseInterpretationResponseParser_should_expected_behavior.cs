using Centello.Application;

namespace Centello.Infrastructure.Tests;

public sealed class ExpenseInterpretationResponseParser_should_expected_behavior
{
    [Fact]
    public void Read_the_structured_values_and_transcript()
    {
        var result = ExpenseInterpretationResponseParser.Parse(
            """{"amount":12.50,"categoryKey":"c1","tagKey":"t1","date":"2026-11-22","note":"Pizza","heard":"  Ieri dodici e cinquanta di pizza  "}""");

        Assert.True(result.IsSuccess);
        Assert.Equal(new ExpenseInterpretation(12.50m, "c1", "t1", "2026-11-22", "Pizza"), result.Value.Interpretation);
        Assert.Equal("Ieri dodici e cinquanta di pizza", result.Value.Heard);
    }

    [Fact]
    public void Read_nulls_as_missing_draft_fields()
    {
        var result = ExpenseInterpretationResponseParser.Parse(
            """{"amount":null,"categoryKey":null,"tagKey":null,"date":null,"note":null,"heard":null}""");

        Assert.True(result.IsSuccess);
        Assert.Equal(new ExpenseInterpretation(null, null, null, null, ""), result.Value.Interpretation);
        Assert.Equal("", result.Value.Heard);
    }

    [Theory]
    [InlineData("")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("""{"amount":"12,50","categoryKey":null,"tagKey":null,"date":null,"note":"","heard":""}""")]
    [InlineData("""{"amount":12.50,"categoryKey":null,"tagKey":null,"date":null,"note":true,"heard":""}""")]
    [InlineData("""{"amount":12.50,"categoryKey":null,"tagKey":null,"date":null,"note":""}""")]
    public void Reject_malformed_or_incomplete_responses(string json)
    {
        var result = ExpenseInterpretationResponseParser.Parse(json);

        Assert.Equal(DraftErrors.MalformedResponse, result.Error);
    }
}
