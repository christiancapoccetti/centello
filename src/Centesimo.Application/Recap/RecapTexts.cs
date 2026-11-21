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

    /// <summary>What a change is measured against, ready after a name: "rispetto a ottobre", "rispetto a novembre
    /// 2025", "rispetto al 2025" and, for the year so far, "rispetto al 2025 fino al 20 novembre".</summary>
    public static string Versus(RecapSpan span)
    {
        var compared = span.Period.Previous();
        if (compared.Kind == PeriodKind.Year)
            return span.IsSoFar
                ? $"rispetto al {compared.Start.Year} fino {ItalianFormat.UntilLongDate(span.Compared.To)}"
                : $"rispetto al {compared.Start.Year}";

        var month = ItalianFormat.MonthName(compared.Start.Month);
        return compared.Start.Year == span.Period.Start.Year ? $"rispetto a {month}" : $"rispetto a {month} {compared.Start.Year}";
    }

    /// <summary>"il 4", "l'8", "l'11": a day of the month with its article.</summary>
    public static string TheDay(int day) => day is 8 or 11 ? $"l'{day}" : $"il {day}";

    /// <summary>"Il " or "L'" before a percentage: "Il 31%", "L'8%", "L'11%", "L'85%".</summary>
    public static string ThePercent(int percent) => percent is 8 or 11 or (>= 80 and <= 89) ? "L'" : "Il ";

    /// <summary>"a", "a e b", "a, b e c".</summary>
    public static string List(IReadOnlyList<string> items) => items.Count switch
    {
        0 => "",
        1 => items[0],
        _ => $"{string.Join(", ", items.Take(items.Count - 1))} e {items[^1]}"
    };

    internal static string Capitalized(string text) => char.ToUpperInvariant(text[0]) + text[1..];
}
