using System.Globalization;

namespace Centesimo.Application;

/// <summary>it-IT strings for the home. Spells separators and the no-break space before "€" explicitly, so the
/// output does not depend on the ICU version or on globalization-invariant mode. Amounts are never negative.</summary>
internal static class ItalianFormat
{
    private static readonly string[] MonthNames =
    [
        "gennaio", "febbraio", "marzo", "aprile", "maggio", "giugno",
        "luglio", "agosto", "settembre", "ottobre", "novembre", "dicembre"
    ];

    private static readonly string[] ShortMonthNames =
        ["gen", "feb", "mar", "apr", "mag", "giu", "lug", "ago", "set", "ott", "nov", "dic"];

    /// <summary>"1.284,60"</summary>
    public static string Amount(long cents) => $"{Grouped(cents / 100)},{cents % 100:00}";

    /// <summary>"1.284,60 €"</summary>
    public static string Money(long cents) => $"{Amount(cents)} €";

    /// <summary>"1.600" for whole euros, "1.600,50" otherwise.</summary>
    public static string Budget(long cents) => cents % 100 == 0 ? Grouped(cents / 100) : Amount(cents);

    /// <summary>"novembre"</summary>
    public static string MonthName(int month) => MonthNames[month - 1];

    /// <summary>"Novembre"</summary>
    public static string MonthTitle(int month) => char.ToUpperInvariant(MonthName(month)[0]) + MonthName(month)[1..];

    /// <summary>"nov"</summary>
    public static string ShortMonth(int month) => ShortMonthNames[month - 1];

    /// <summary>"12 nov"</summary>
    public static string ShortDate(DateOnly date) => $"{date.Day} {ShortMonth(date.Month)}";

    /// <summary>"12 novembre"</summary>
    public static string LongDate(DateOnly date) => $"{date.Day} {MonthName(date.Month)}";

    /// <summary>"6%"</summary>
    public static string Percent(int percent) => $"{percent}%";

    private static string Grouped(long units) => units.ToString("N0", CultureInfo.InvariantCulture).Replace(',', '.');
}
