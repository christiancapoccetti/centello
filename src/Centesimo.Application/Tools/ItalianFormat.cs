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

    // Indexed by DayOfWeek, which starts on Sunday.
    private static readonly string[] ShortWeekdayNames = ["dom", "lun", "mar", "mer", "gio", "ven", "sab"];

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

    /// <summary>"lun 17 nov"</summary>
    public static string WeekdayDate(DateOnly date) => $"{ShortWeekdayNames[(int)date.DayOfWeek]} {ShortDate(date)}";

    /// <summary>"Oggi", "Ieri" or "12 nov"</summary>
    public static string RelativeDay(DateOnly date, DateOnly today) => RelativeDay(date, today, ShortDate);

    /// <summary>"Oggi", "Ieri" or "lun 17 nov"</summary>
    public static string DayHeader(DateOnly date, DateOnly today) => RelativeDay(date, today, WeekdayDate);

    /// <summary>"Novembre 2026" or "Anno 2026"</summary>
    public static string PeriodTitle(Period period) => period.Kind == PeriodKind.Month
        ? $"{MonthTitle(period.Start.Month)} {period.Start.Year}"
        : $"Anno {period.Start.Year}";

    /// <summary>"a novembre", "ad aprile" or "nel 2026"</summary>
    public static string InPeriod(Period period)
    {
        if (period.Kind == PeriodKind.Year)
            return $"nel {period.Start.Year}";

        var month = MonthName(period.Start.Month);
        return month[0] == 'a' ? $"ad {month}" : $"a {month}";
    }

    /// <summary>"6%"</summary>
    public static string Percent(int percent) => $"{percent}%";

    private static string RelativeDay(DateOnly date, DateOnly today, Func<DateOnly, string> otherwise) => date == today
        ? "Oggi"
        : date == today.AddDays(-1) ? "Ieri" : otherwise(date);

    private static string Grouped(long units) => units.ToString("N0", CultureInfo.InvariantCulture).Replace(',', '.');
}
