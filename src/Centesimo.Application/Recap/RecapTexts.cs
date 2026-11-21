namespace Centesimo.Application;

/// <summary>Wording shared by more than one recap card.</summary>
public static class RecapTexts
{
    /// <summary>"Il tuo novembre", "Il tuo 2026".</summary>
    public static string Title(RecapSpan span) => span.Period.Kind switch
    {
        PeriodKind.Month => $"Il tuo {ItalianFormat.MonthName(span.Period.Start.Month)}",
        PeriodKind.Year => $"Il tuo {span.Period.Start.Year}"
    };

    /// <summary>"2026" for a month, "" for a closed year (the title says it), "finora" for the year so far.</summary>
    public static string Badge(RecapSpan span) => span.Period.Kind switch
    {
        PeriodKind.Month => $"{span.Period.Start.Year}",
        PeriodKind.Year => span.IsSoFar ? "finora" : ""
    };

    /// <summary>"1 spesa", "1.143 spese".</summary>
    public static string Count(long count, string one, string many) =>
        $"{ItalianFormat.Count(count)} {(count == 1 ? one : many)}";

    /// <summary>"1 volta", "22 volte".</summary>
    public static string Times(long count) => Count(count, "volta", "volte");

    /// <summary>"26 giorni su 30", "1 giorno su 30".</summary>
    public static string DaysOf(int days, int of) => $"{Count(days, "giorno", "giorni")} su {of}";

    /// <summary>What "Il secondo mese più leggero" puts before "mese": "" for first, "secondo " and "terzo ".</summary>
    public static string Ordinal(int rank) => rank switch
    {
        1 => "",
        2 => "secondo ",
        3 => "terzo ",
        _ => throw new ArgumentOutOfRangeException(nameof(rank), rank, "Only the first three ranks are worded.")
    };

    /// <summary>"Il sabato", "La domenica".</summary>
    public static string TheWeekday(DayOfWeek day) => day == DayOfWeek.Sunday
        ? $"La {ItalianFormat.Weekday(day)}"
        : $"Il {ItalianFormat.Weekday(day)}";

    /// <summary>"Sabato 14 novembre".</summary>
    public static string WeekdayLongDate(DateOnly day) =>
        $"{Capitalized(ItalianFormat.Weekday(day.DayOfWeek))} {ItalianFormat.LongDate(day)}";

    /// <summary>"da settembre", "da aprile".</summary>
    public static string Since(int month) => $"da {ItalianFormat.MonthName(month)}";

    /// <summary>A month's total keeps its cents ("1.284,60"); a year's is whole euros ("17.955"), as both mockups.</summary>
    public static string Total(RecapSpan span, long cents) => span.Period.Kind switch
    {
        PeriodKind.Month => ItalianFormat.Amount(cents),
        PeriodKind.Year => ItalianFormat.WholeEuros(cents)
    };

    /// <summary><see cref="Total"/> with its euro sign: "1.284,60 €", "17.955 €".</summary>
    public static string TotalMoney(RecapSpan span, long cents) => span.Period.Kind switch
    {
        PeriodKind.Month => ItalianFormat.Money(cents),
        PeriodKind.Year => ItalianFormat.WholeMoney(cents)
    };

    /// <summary>What the span is measured against: "di ottobre", "di novembre 2025", "del 2025" and, for the year so
    /// far, "del 2025 al 20 novembre". A month names its year only when it is not the span's.</summary>
    public static string Against(RecapSpan span, Period compared)
    {
        if (compared.Kind == PeriodKind.Year)
            return span.IsSoFar
                ? $"del {compared.Start.Year} {ItalianFormat.UntilLongDate(span.Compared.To)}"
                : $"del {compared.Start.Year}";

        var month = ItalianFormat.MonthName(compared.Start.Month);
        return compared.Start.Year == span.Period.Start.Year ? $"di {month}" : $"di {month} {compared.Start.Year}";
    }

    internal static string Capitalized(string text) => char.ToUpperInvariant(text[0]) + text[1..];
}
