using System.Globalization;

namespace Centello.Application;

/// <summary>How Insight words an overview. The period of today is compared to date, so its words name the days or
/// the months that count and never a whole period that was not compared.</summary>
public static class InsightTexts
{
    private const char Minus = '−';

    /// <summary>Three runs so the difference can be bold: "Fino a oggi hai speso " + "82,00 € in meno" + " degli
    /// stessi giorni di ottobre." The amount keeps its cents: it states how far the total above it moved.</summary>
    public static BudgetSummary Summary(Period period, bool isToDate, long spentCents, long? comparedCents)
    {
        if (comparedCents is not { } compared)
            return new BudgetSummary($"Nessuna spesa da confrontare {InComparedPeriod(period, isToDate)}.", "", "");

        var subject = $"{SubjectOf(period, isToDate)} ";
        var difference = spentCents - compared;
        if (difference == 0)
            return new BudgetSummary(subject, "quanto", $" {InComparedPeriod(period, isToDate)}.");

        return new BudgetSummary(subject, $"{ItalianFormat.Money(Math.Abs(difference))} {(difference < 0 ? "in meno" : "in più")}",
            $" {ComparedTo(period, isToDate)}.");
    }

    public static string Share(double fraction) => Percent((decimal)fraction);

    public static string Variation(long spent, long? prior) => prior switch
    {
        null => "—",
        0 => "Nuovo",
        _ => Signed(spent - prior.Value) + Percent((decimal)(spent - prior.Value) / prior.Value)
    };

    public static string Delta(long cents) => $"{Signed(cents)}{Money(Math.Abs(cents))}";

    public static string Money(long cents) => $"{ItalianFormat.Budget(cents)} €";

    public static string DateRange(DateOnly from, DateOnly to)
    {
        if (from == to)
            return ItalianFormat.LongDate(to);

        var start = from.Month == to.Month ? from.Day.ToString() : ItalianFormat.LongDate(from);
        return $"{start}–{ItalianFormat.LongDate(to)}";
    }

    /// <summary>"degli stessi giorni di ottobre" for the month of today, "di settembre" for a past month, "dello stesso
    /// periodo del 2025" for the year of today and "del 2024" for a past year: what the period is measured against.</summary>
    internal static string ComparedTo(Period period, bool isToDate)
    {
        var previous = period.Previous();
        if (period.Kind == PeriodKind.Month)
            return isToDate
                ? $"degli stessi giorni di {ItalianFormat.MonthName(previous.Start.Month)}"
                : $"di {ItalianFormat.MonthName(previous.Start.Month)}";

        return isToDate ? $"dello stesso periodo del {previous.Start.Year}" : $"del {previous.Start.Year}";
    }

    /// <summary>"negli stessi giorni di ottobre", "a settembre", "nello stesso periodo del 2025" or "nel 2024": where
    /// the compared spending is looked at. The compared window of the period of today covers only the days that have
    /// passed, so it is never named as the whole month or year.</summary>
    internal static string InComparedPeriod(Period period, bool isToDate)
    {
        var previous = period.Previous();
        if (!isToDate)
            return ItalianFormat.InPeriod(previous);

        return period.Kind == PeriodKind.Month
            ? $"negli stessi giorni di {ItalianFormat.MonthName(previous.Start.Month)}"
            : $"nello stesso periodo del {previous.Start.Year}";
    }

    /// <summary>"di novembre" or "del 2026": whose spending a share is of.</summary>
    internal static string OfPeriod(Period period) => period.Kind == PeriodKind.Month
        ? $"di {ItalianFormat.MonthName(period.Start.Month)}"
        : $"del {period.Start.Year}";

    /// <summary>"Ottobre" or "2025": a period as a key item names it.</summary>
    internal static string Name(Period period) => period.Kind == PeriodKind.Month
        ? ItalianFormat.MonthTitle(period.Start.Month)
        : period.Start.Year.ToString();

    private static string SubjectOf(Period period, bool isToDate)
    {
        if (period.Kind == PeriodKind.Month)
            return isToDate ? "Fino a oggi hai speso" : $"{RowLabels.Sentence(ItalianFormat.InPeriod(period))} hai speso";

        return isToDate ? "Da gennaio a oggi hai speso" : $"Nel {period.Start.Year} hai speso";
    }

    private static string Percent(decimal fraction)
    {
        var percent = decimal.Round(Math.Abs(fraction) * 100, 1, MidpointRounding.AwayFromZero);
        if (percent == 0 && fraction != 0)
            return "<0,1%";

        return percent.ToString("0.#", CultureInfo.InvariantCulture).Replace('.', ',') + "%";
    }

    private static string Signed(long cents) => cents switch
    {
        < 0 => Minus.ToString(),
        > 0 => "+",
        _ => ""
    };
}
