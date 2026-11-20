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

    /// <summary>"a ottobre", "nello stesso periodo di ottobre" or "nel 2025": where the compared spending is looked
    /// at. The compared window of the period of today covers only the days that have passed, so it is never named as
    /// the whole month or year.</summary>
    internal static string InComparedPeriod(Period period, bool isToDate)
    {
        var previous = period.Previous();
        if (!isToDate)
            return ItalianFormat.InPeriod(previous);

        return period.Kind == PeriodKind.Month
            ? $"nello stesso periodo di {ItalianFormat.MonthName(previous.Start.Month)}"
            : $"nello stesso periodo del {previous.Start.Year}";
    }

    /// <summary>"di novembre" or "del 2026": whose spending a share is of.</summary>
    internal static string OfPeriod(Period period) => period.Kind == PeriodKind.Month
        ? $"di {ItalianFormat.MonthName(period.Start.Month)}"
        : $"del {period.Start.Year}";

    private static int Percent(double fraction) =>
        (int)Math.Round(Math.Abs(fraction) * 100, MidpointRounding.AwayFromZero);
}
