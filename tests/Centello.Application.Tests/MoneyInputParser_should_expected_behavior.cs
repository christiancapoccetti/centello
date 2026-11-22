using System.Globalization;

namespace Centello.Application.Tests;

public sealed class MoneyInputParser_should_expected_behavior
{
    [Fact]
    public void Parse_optional_italian_decimal_to_cents()
    {
        var result = MoneyInputParser.ParseOptional("123,45", CultureInfo.GetCultureInfo("it-IT"));

        Assert.Equal(12_345, result.Value?.Cents);
    }

    [Fact]
    public void Treat_blank_budget_as_missing() =>
        Assert.Null(MoneyInputParser.ParseOptional(" ", CultureInfo.InvariantCulture).Value);

    [Theory]
    [InlineData("-1")]
    [InlineData("1.001")]
    [InlineData("abc")]
    public void Reject_invalid_budget(string value) =>
        Assert.Equal("Money.InvalidInput",
            MoneyInputParser.ParseOptional(value, CultureInfo.InvariantCulture).Error.Code);

    [Fact]
    public void Parse_a_positive_italian_amount_to_cents()
    {
        var result = MoneyInputParser.ParsePositive("12,5", CultureInfo.GetCultureInfo("it-IT"));

        Assert.Equal(1_250, result.Value.Cents);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("0")]
    [InlineData("0,00")]
    public void Ask_for_an_amount_above_zero_when_it_is_missing_or_zero(string value)
    {
        var result = MoneyInputParser.ParsePositive(value, CultureInfo.GetCultureInfo("it-IT"));

        Assert.Equal(MoneyInputParser.AmountRequired, result.Error);
        Assert.Equal("Inserisci un importo maggiore di zero.", result.Error.Message);
    }

    [Theory]
    [InlineData("12,345")]
    [InlineData("abc")]
    public void Say_how_to_write_a_positive_amount_that_cannot_be_read(string value) =>
        Assert.Equal(MoneyInputParser.InvalidMoney,
            MoneyInputParser.ParsePositive(value, CultureInfo.GetCultureInfo("it-IT")).Error);
}
