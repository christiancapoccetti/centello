using System.Globalization;

namespace Centello.Application;

/// <summary>A value label as it is drawn: the words and their size.</summary>
public readonly record struct WaterfallLabel(string Text, double SizeSp);

/// <summary>What a value label of the waterfall shows in a column of a given width. The chart has up to eight columns
/// on a phone, so a four-digit change or a five-digit total can be wider than its column: the label first keeps its
/// digits and shrinks, and only then gives up digits for a thousands or millions mark, which reads as the same number
/// and never as a smaller one.</summary>
public static class WaterfallLabels
{
    public const double NormalSizeSp = 12;
    public const double SmallestSizeSp = 10;

    private const char Minus = '−';

    /// <summary>The first of the full text and its compact form, at 12, 11 and 10sp, whose width
    /// (<paramref name="widthDp"/> of the text at the size) fits <paramref name="columnDp"/>. When none does, the compact
    /// form at the smallest size.</summary>
    public static WaterfallLabel Fit(string value, double columnDp, Func<string, double, double> widthDp)
    {
        var compact = Compact(value);
        foreach (var text in new[] { value, compact }.Distinct())
            for (var size = NormalSizeSp; size >= SmallestSizeSp; size--)
                if (widthDp(text, size) <= columnDp)
                    return new WaterfallLabel(text, size);

        return new WaterfallLabel(compact, SmallestSizeSp);
    }

    /// <summary>"+1.527" as "+1,5k", "−12.400" as "−12k", "2.131.824" as "2,1M". A value under a thousand is kept.</summary>
    public static string Compact(string value)
    {
        var sign = value.Length > 0 && (value[0] == '+' || value[0] == Minus) ? value[..1] : "";
        if (!long.TryParse(value[sign.Length..].Replace(".", ""), NumberStyles.None, CultureInfo.InvariantCulture, out var euros)
            || euros < 1000)
            return value;

        var thousands = Math.Round(euros / 1000d, euros < 10_000 ? 1 : 0, MidpointRounding.AwayFromZero);
        if (euros < 1_000_000 && thousands < 1000)
            return $"{sign}{Scaled(thousands)}k";

        var millions = Math.Round(euros / 1_000_000d, euros < 10_000_000 ? 1 : 0, MidpointRounding.AwayFromZero);
        return $"{sign}{Scaled(millions)}M";
    }

    private static string Scaled(double amount) => amount.ToString("0.#", CultureInfo.InvariantCulture).Replace('.', ',');
}
