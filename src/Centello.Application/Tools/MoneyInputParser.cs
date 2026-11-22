using System.Globalization;
using Centello.Domain;

namespace Centello.Application;

public static class MoneyInputParser
{
    public static readonly Error InvalidMoney = new(
        "Money.InvalidInput",
        "Inserisci un importo valido con massimo due decimali.");

    public static readonly Error AmountRequired = new(
        "Money.AmountRequired",
        "Inserisci un importo maggiore di zero.");

    /// <summary>An amount that has to be there and above zero, as an expense or a recurring payment needs.</summary>
    public static Result<Money> ParsePositive(string? value, CultureInfo culture)
    {
        var parsed = ParseOptional(value, culture);
        if (parsed.IsFailure)
            return Result<Money>.Failure(parsed.Error);

        return parsed.Value is { Cents: > 0 } amount
            ? Result<Money>.Success(amount)
            : Result<Money>.Failure(AmountRequired);
    }

    public static Result<Money?> ParseOptional(string? value, CultureInfo culture)
    {
        if (value.IsEmpty())
            return Result<Money?>.Success(null);

        var input = value!.Trim();
        var decimalSeparator = culture.NumberFormat.NumberDecimalSeparator;
        if (decimalSeparator == ",")
            input = input.Replace('.', ',');

        var separatorIndex = input.IndexOf(decimalSeparator, StringComparison.Ordinal);
        if (separatorIndex >= 0 && input.Length - separatorIndex - decimalSeparator.Length > 2)
            return Result<Money?>.Failure(InvalidMoney);

        const NumberStyles style = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;
        if (!decimal.TryParse(input, style, culture, out var amount) || amount < 0)
            return Result<Money?>.Failure(InvalidMoney);

        try
        {
            return Result<Money?>.Success(Money.FromDecimal(amount));
        }
        catch (OverflowException)
        {
            return Result<Money?>.Failure(InvalidMoney);
        }
    }
}
