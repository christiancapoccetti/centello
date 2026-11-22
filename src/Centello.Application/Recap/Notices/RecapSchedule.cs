namespace Centello.Application;

/// <summary>When a recap is announced and what the notification says. Pure, so every rule is tested without a device.
/// The alarm never depends on data: it is always the next 1st at <see cref="At"/>, and whether a notice is owed is
/// decided when it fires.</summary>
public static class RecapSchedule
{
    public static readonly TimeOnly At = new(9, 0);

    /// <summary>A notice stays owed through the 3rd, so a phone that was off on the 1st still hears about it.</summary>
    public const int GraceDays = 3;

    /// <summary>The period announced at <paramref name="now"/>: the year that closed on 1 to 3 January, the month that
    /// closed otherwise, from <see cref="At"/> on the 1st until the end of the 3rd. Never both on 1 January. Null
    /// otherwise.</summary>
    public static Period? Due(DateTime now)
    {
        if (now.Day > GraceDays)
            return null;

        if (now.Day == 1 && TimeOnly.FromDateTime(now) < At)
            return null;

        return now.Month == 1
            ? Period.Year(now.Year - 1)
            : Period.Month(now.Year, now.Month).Previous();
    }

    /// <summary>"recap-month-2026-11" or "recap-year-2026": the stable key of a closed period, for the dedupe and the
    /// notification id.</summary>
    public static string KeyOf(Period period) => period.Kind == PeriodKind.Year
        ? $"recap-year-{period.Start.Year:0000}"
        : $"recap-month-{period.Start.Year:0000}-{period.Start.Month:00}";

    /// <summary>"Il tuo novembre è pronto" / "Rivedi le spese del mese, in poche schede." and "Il tuo 2026 è pronto" /
    /// "Un anno di spese, in poche schede."</summary>
    public static RecapNotice NoticeFor(Period period) => period.Kind == PeriodKind.Year
        ? new RecapNotice(KeyOf(period), period, $"Il tuo {period.Start.Year} è pronto",
            "Un anno di spese, in poche schede.")
        : new RecapNotice(KeyOf(period), period, $"Il tuo {ItalianFormat.MonthName(period.Start.Month)} è pronto",
            "Rivedi le spese del mese, in poche schede.");

    /// <summary>The next 1st at <see cref="At"/> strictly after <paramref name="now"/>, in local time.</summary>
    public static DateTime NextArm(DateTime now)
    {
        var thisMonth = new DateTime(now.Year, now.Month, 1).Add(At.ToTimeSpan());
        return thisMonth > now ? thisMonth : thisMonth.AddMonths(1);
    }
}
