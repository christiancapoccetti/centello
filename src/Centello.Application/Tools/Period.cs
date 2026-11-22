namespace Centello.Application;

public enum PeriodKind
{
    Month,
    Year
}

/// <summary>A calendar month or year. <see cref="Start"/> is always its first day; the private
/// constructor makes an unnormalized period unrepresentable.</summary>
public sealed record Period
{
    private Period(PeriodKind kind, DateOnly start)
    {
        Kind = kind;
        Start = start;
    }

    public PeriodKind Kind { get; }
    public DateOnly Start { get; }
    public DateOnly End => Kind == PeriodKind.Month
        ? Start.AddMonths(1).AddDays(-1)
        : new DateOnly(Start.Year, 12, 31);

    public static Period Month(int year, int month) => new(PeriodKind.Month, new DateOnly(year, month, 1));
    public static Period Year(int year) => new(PeriodKind.Year, new DateOnly(year, 1, 1));
    public static Period MonthOf(DateOnly day) => Month(day.Year, day.Month);

    public Period Previous() => Shift(-1);
    public Period Next() => Shift(1);
    public bool Contains(DateOnly day) => day >= Start && day <= End;

    private Period Shift(int steps) => Kind == PeriodKind.Month
        ? new Period(Kind, Start.AddMonths(steps))
        : new Period(Kind, Start.AddYears(steps));
}
