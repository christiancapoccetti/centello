namespace Centesimo.Application;

public enum InsightChangeTone { Higher, Lower, Same, New }

/// <summary>How the spending of a category moved against the compared period: "+18%", "−47%", "uguale" or, with
/// nothing to compare, "nuovo".</summary>
public sealed record InsightChange(string Text, InsightChangeTone Tone);

/// <summary>How Insight words an overview. The period of today is compared to date, so its words name the days or
/// the months that count and never a whole period that was not compared.</summary>
public static class InsightTexts
{
    private const char Minus = '−';

    /// <summary>Three runs so the change can be bold: "Hai speso il " + "6% in meno" + " di ottobre."</summary>
    public static BudgetSummary Summary(InsightsOverview overview)
    {
        var period = overview.Period;
        var previous = period.Previous();
        if (overview.ChangePercentage is not { } change)
            return new BudgetSummary($"Nessuna spesa da confrontare {ItalianFormat.InPeriod(previous)}.", "", "");

        var isMonth = period.Kind == PeriodKind.Month;
        var subject = isMonth
            ? overview.IsToDate ? "Hai speso" : $"{RowLabels.Sentence(ItalianFormat.InPeriod(period))} hai speso"
            : overview.IsToDate ? "Da gennaio a oggi hai speso" : $"Nel {period.Start.Year} hai speso";
        var percent = Percent(change);
        if (percent == 0)
            return new BudgetSummary($"{subject} ", "quanto", !isMonth && overview.IsToDate
                ? $" nello stesso periodo del {previous.Start.Year}."
                : $" {ItalianFormat.InPeriod(previous)}.");

        return new BudgetSummary($"{subject} il ", $"{ItalianFormat.Percent(percent)} {(change < 0 ? "in meno" : "in più")}",
            $" {ComparedTo(period, overview.IsToDate)}.");
    }

    /// <summary>The names of the two amounts that are compared: "1–19 nov" and "1–19 ott" for the month of today,
    /// "Ottobre" and "Settembre" for a past month, "gen–nov 26" and "gen–nov 25" for the year of today, "2025" and
    /// "2024" for a past year.</summary>
    public static (string Current, string Previous) WindowLabels(InsightsOverview overview)
    {
        var period = overview.Period;
        var previous = period.Previous();
        var to = overview.To;
        if (period.Kind == PeriodKind.Month)
            return overview.IsToDate
                ? (Days(to.Day, period.Start.Month), Days(Math.Min(to.Day, previous.End.Day), previous.Start.Month))
                : (ItalianFormat.MonthTitle(period.Start.Month), ItalianFormat.MonthTitle(previous.Start.Month));

        return overview.IsToDate
            ? (Months(to.Month, period.Start.Year), Months(to.Month, previous.Start.Year))
            : (period.Start.Year.ToString(), previous.Start.Year.ToString());
    }

    /// <summary>"32%"</summary>
    public static string Share(double fraction) => ItalianFormat.Percent(Percent(fraction));

    public static InsightChange Change(double? change)
    {
        if (change is not { } value)
            return new InsightChange("nuovo", InsightChangeTone.New);

        var percent = Percent(value);
        if (percent == 0)
            return new InsightChange("uguale", InsightChangeTone.Same);

        return value > 0
            ? new InsightChange($"+{ItalianFormat.Percent(percent)}", InsightChangeTone.Higher)
            : new InsightChange($"{Minus}{ItalianFormat.Percent(percent)}", InsightChangeTone.Lower);
    }

    /// <summary>"di ottobre", "dello stesso periodo del 2025" or "del 2024": what the period is measured against.</summary>
    internal static string ComparedTo(Period period, bool isToDate)
    {
        var previous = period.Previous();
        if (period.Kind == PeriodKind.Month)
            return $"di {ItalianFormat.MonthName(previous.Start.Month)}";

        return isToDate ? $"dello stesso periodo del {previous.Start.Year}" : $"del {previous.Start.Year}";
    }

    /// <summary>"di novembre" or "del 2026": whose spending a share is of.</summary>
    internal static string OfPeriod(Period period) => period.Kind == PeriodKind.Month
        ? $"di {ItalianFormat.MonthName(period.Start.Month)}"
        : $"del {period.Start.Year}";

    private static int Percent(double fraction) =>
        (int)Math.Round(Math.Abs(fraction) * 100, MidpointRounding.AwayFromZero);

    private static string Days(int last, int month) =>
        last == 1 ? $"1 {ItalianFormat.ShortMonth(month)}" : $"1–{last} {ItalianFormat.ShortMonth(month)}";

    private static string Months(int last, int year)
    {
        var shortYear = (year % 100).ToString("00");
        return last == 1 ? $"gen {shortYear}" : $"gen–{ItalianFormat.ShortMonth(last)} {shortYear}";
    }
}
