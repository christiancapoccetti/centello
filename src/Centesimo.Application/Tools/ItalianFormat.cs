using System.Globalization;

namespace Centesimo.Application;

/// <summary>it-IT strings for every screen. Spells separators and the no-break space before "€" explicitly, so the
/// output does not depend on the ICU version or on globalization-invariant mode. Amounts are never negative.</summary>
public static class ItalianFormat
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

    /// <summary>"1.367" for 1.366,60: rounded to the euro, half up. Chart labels only; amounts in sentences keep their
    /// cents.</summary>
    public static string WholeEuros(long cents) => Grouped((cents + 50) / 100);

    /// <summary>"1.218" for a count of things.</summary>
    public static string Count(long count) => Grouped(count);

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

    /// <summary>"12 nov", or "12 nov 2027" when the date is in another year than today</summary>
    public static string ShortDate(DateOnly date, DateOnly today) =>
        date.Year == today.Year ? ShortDate(date) : $"{ShortDate(date)} {date.Year}";

    /// <summary>"12 novembre"</summary>
    public static string LongDate(DateOnly date) => $"{date.Day} {MonthName(date.Month)}";

    /// <summary>"12 novembre 2026"</summary>
    public static string FullDate(DateOnly date) => $"{LongDate(date)} {date.Year}";

    /// <summary>"del 5 dicembre", or "dell'8 agosto" and "dell'11 novembre" where the article elides</summary>
    public static string OfLongDate(DateOnly date) => $"{(date.Day is 8 or 11 ? "dell'" : "del ")}{LongDate(date)}";

    /// <summary>"Una spesa di 12,50 € scade il 18 novembre.", or "scade l'8 agosto." where the article elides</summary>
    public static string PaymentDue(long cents, DateOnly dueOn) =>
        $"Una spesa di {Money(cents)} scade {(dueOn.Day is 8 or 11 ? "l'" : "il ")}{LongDate(dueOn)}.";

    /// <summary>"lun 17 nov"</summary>
    public static string WeekdayDate(DateOnly date) => $"{ShortWeekdayNames[(int)date.DayOfWeek]} {ShortDate(date)}";

    /// <summary>"Oggi", "Ieri" or "12 nov"</summary>
    public static string RelativeDay(DateOnly date, DateOnly today) => RelativeDay(date, today, ShortDate);

    /// <summary>"Oggi", "Ieri" or "lun 17 nov"</summary>
    public static string DayHeader(DateOnly date, DateOnly today) => RelativeDay(date, today, WeekdayDate);

    /// <summary>"Novembre" or "Anno": the period without its year</summary>
    public static string PeriodName(Period period) =>
        period.Kind == PeriodKind.Month ? MonthTitle(period.Start.Month) : "Anno";

    /// <summary>"Novembre 2026" or "Anno 2026"</summary>
    public static string PeriodTitle(Period period) => $"{PeriodName(period)} {period.Start.Year}";

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
