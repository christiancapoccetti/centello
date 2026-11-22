namespace Centello.Application.Tests;

public sealed class RecapSchedule_should_expected_behavior
{
    public static TheoryData<DateTime, Period?> DueCases => new()
    {
        { new DateTime(2026, 12, 1, 8, 59, 0), null },
        { new DateTime(2026, 12, 1, 9, 0, 0), Period.Month(2026, 11) },
        { new DateTime(2026, 12, 3, 23, 59, 0), Period.Month(2026, 11) },
        { new DateTime(2026, 12, 4, 0, 0, 0), null },
        { new DateTime(2026, 12, 15, 12, 0, 0), null },
        { new DateTime(2026, 3, 1, 9, 0, 0), Period.Month(2026, 2) },
        { new DateTime(2027, 1, 1, 8, 59, 0), null },
        { new DateTime(2027, 1, 1, 9, 0, 0), Period.Year(2026) },
        { new DateTime(2027, 1, 3, 23, 59, 0), Period.Year(2026) },
        { new DateTime(2027, 1, 4, 9, 0, 0), null },
        { new DateTime(2027, 2, 2, 10, 0, 0), Period.Month(2027, 1) }
    };

    [Theory]
    [MemberData(nameof(DueCases))]
    public void Announce_the_closed_period_from_nine_on_the_first_through_the_third(DateTime now, Period? expected) =>
        Assert.Equal(expected, RecapSchedule.Due(now));

    public static TheoryData<DateTime, DateTime> NextArmCases => new()
    {
        { new DateTime(2026, 11, 21, 12, 0, 0), new DateTime(2026, 12, 1, 9, 0, 0) },
        { new DateTime(2026, 12, 1, 8, 59, 0), new DateTime(2026, 12, 1, 9, 0, 0) },
        { new DateTime(2026, 12, 1, 9, 0, 0), new DateTime(2027, 1, 1, 9, 0, 0) },
        { new DateTime(2026, 12, 31, 23, 59, 0), new DateTime(2027, 1, 1, 9, 0, 0) },
        { new DateTime(2026, 1, 31, 10, 0, 0), new DateTime(2026, 2, 1, 9, 0, 0) }
    };

    [Theory]
    [MemberData(nameof(NextArmCases))]
    public void Arm_the_next_first_at_nine_strictly_after_now(DateTime now, DateTime expected) =>
        Assert.Equal(expected, RecapSchedule.NextArm(now));

    [Fact]
    public void Key_a_month_and_a_year_apart()
    {
        Assert.Equal("recap-month-2026-11", RecapSchedule.KeyOf(Period.Month(2026, 11)));
        Assert.Equal("recap-year-2026", RecapSchedule.KeyOf(Period.Year(2026)));
    }

    [Fact]
    public void Word_a_month_notice_without_amounts()
    {
        var notice = RecapSchedule.NoticeFor(Period.Month(2026, 11));

        Assert.Equal("recap-month-2026-11", notice.Key);
        Assert.Equal("Il tuo novembre è pronto", notice.Title);
        Assert.Equal("Rivedi le spese del mese, in poche schede.", notice.Text);
        Assert.Equal(new AppEntry.Screen.Recap(Period.Month(2026, 11)), notice.Entry);
    }

    [Fact]
    public void Word_a_year_notice_without_amounts()
    {
        var notice = RecapSchedule.NoticeFor(Period.Year(2026));

        Assert.Equal("recap-year-2026", notice.Key);
        Assert.Equal("Il tuo 2026 è pronto", notice.Title);
        Assert.Equal("Un anno di spese, in poche schede.", notice.Text);
        Assert.Equal(new AppEntry.Screen.Recap(Period.Year(2026)), notice.Entry);
    }
}
