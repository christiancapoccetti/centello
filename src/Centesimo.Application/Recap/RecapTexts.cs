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
}
