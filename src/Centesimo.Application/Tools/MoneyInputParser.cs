using System.Globalization;
using Centesimo.Domain;

namespace Centesimo.Application;

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

        if (!decimal.TryParse(value, NumberStyles.Number, culture, out var amount) || amount < 0)
            return Result<Money?>.Failure(InvalidMoney);

        try
        {
            return Result<Money?>.Success(Money.FromDecimal(amount));
        }
        catch (ArgumentException)
        {
            return Result<Money?>.Failure(InvalidMoney);
        }
        catch (OverflowException)
        {
            return Result<Money?>.Failure(InvalidMoney);
        }
    }
}
