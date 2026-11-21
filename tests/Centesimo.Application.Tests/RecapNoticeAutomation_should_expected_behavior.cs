namespace Centesimo.Application.Tests;

public sealed class RecapNoticeAutomation_should_expected_behavior
{
    private static readonly Period November = Period.Month(2026, 11);
    private static readonly Period December = Period.Month(2026, 12);
    private static readonly Period Year2026 = Period.Year(2026);

    private readonly FakeRecapService _recaps = new();
    private readonly FakeRecapNoticeBoard _board = new();
    private readonly FakeRecapNoticeSettings _settings = new();
    private DateTime _now = new(2026, 12, 1, 9, 0, 0);

    private RecapNoticeAutomation Automation() =>
        new(new FakeScopeFactory(new FakeServiceProvider(_recaps)), _board, _settings, () => _now);

    [Theory]
    [InlineData(RecapNoticeState.On, true, true)]
    [InlineData(RecapNoticeState.On, false, true)]
    [InlineData(RecapNoticeState.Undecided, true, true)]
    [InlineData(RecapNoticeState.Undecided, false, false)]
    [InlineData(RecapNoticeState.Off, true, false)]
    [InlineData(RecapNoticeState.Off, false, false)]
    public void Enable_on_or_when_undecided_with_the_permission(RecapNoticeState state, bool canPost, bool expected) =>
        Assert.Equal(expected, RecapNoticeAutomation.IsEnabled(state, canPost));

    [Theory]
    [InlineData(RecapNoticeState.On, true, "Il 1° del mese, alle 9")]
    [InlineData(RecapNoticeState.Undecided, true, "Il 1° del mese, alle 9")]
    [InlineData(RecapNoticeState.Undecided, false, "Spento")]
    [InlineData(RecapNoticeState.Off, true, "Spento")]
    [InlineData(RecapNoticeState.On, false, "Consenti le notifiche a Quattrino nelle impostazioni di Android")]
    public void Describe_the_row(RecapNoticeState state, bool canPost, string expected) =>
        Assert.Equal(expected, RecapNoticeAutomation.Describe(state, canPost));

    [Fact]
    public async Task Post_the_closed_month_once_and_not_again()
    {
        _recaps.Available.Add(November);
        var automation = Automation();

        await automation.Sync();
        await automation.Sync();

        var posted = Assert.Single(_board.Posted);
        Assert.Equal("Il tuo novembre è pronto", posted.Title);
        Assert.Equal("recap-month-2026-11", _settings.LastHandledKey);
    }

    [Fact]
    public async Task Arm_the_next_first_whether_or_not_it_posted()
    {
        _recaps.Available.Add(November);

        await Automation().Sync();

        Assert.Equal([new DateTime(2027, 1, 1, 9, 0, 0)], _board.Armed);
    }

    [Fact]
    public async Task Wait_for_the_first_without_posting_before_it()
    {
        _now = new DateTime(2026, 11, 21, 12, 0, 0);
        _recaps.Available.Add(November);

        await Automation().Sync();

        Assert.Empty(_board.Posted);
        Assert.Equal([new DateTime(2026, 12, 1, 9, 0, 0)], _board.Armed);
    }

    [Fact]
    public async Task Disarm_and_post_nothing_when_off()
    {
        _settings.State = RecapNoticeState.Off;
        _recaps.Available.Add(November);

        await Automation().Sync();

        Assert.Empty(_board.Posted);
        Assert.Empty(_board.Armed);
        Assert.Equal(1, _board.Disarmed);
    }

    [Fact]
    public async Task Treat_undecided_with_the_permission_as_on()
    {
        _settings.State = RecapNoticeState.Undecided;
        _recaps.Available.Add(November);

        await Automation().Sync();

        Assert.Single(_board.Posted);
    }

    [Fact]
    public async Task Treat_undecided_without_the_permission_as_off()
    {
        _settings.State = RecapNoticeState.Undecided;
        _board.CanPost = false;
        _recaps.Available.Add(November);

        await Automation().Sync();

        Assert.Empty(_board.Posted);
        Assert.Equal(1, _board.Disarmed);
    }

    [Fact]
    public async Task Mark_a_thin_period_handled_without_posting()
    {
        await Automation().Sync();

        Assert.Empty(_board.Posted);
        Assert.Equal("recap-month-2026-11", _settings.LastHandledKey);
        Assert.Single(_board.Armed);
    }

    [Fact]
    public async Task Retry_on_the_next_sync_when_the_post_failed()
    {
        _recaps.Available.Add(November);
        _board.PostSucceeds = false;
        var automation = Automation();

        await automation.Sync();
        _board.PostSucceeds = true;
        await automation.Sync();

        Assert.Single(_board.Posted);
        Assert.Equal(2, _board.Armed.Count);
    }

    [Fact]
    public async Task Leave_the_period_unhandled_when_the_read_failed()
    {
        _recaps.Failure = new Error("Test.Read", "Lettura non riuscita.");

        await Automation().Sync();

        Assert.Equal("", _settings.LastHandledKey);
        Assert.Empty(_board.Posted);
        Assert.Single(_board.Armed);
    }

    [Fact]
    public async Task Post_only_the_year_on_1_January()
    {
        _now = new DateTime(2027, 1, 1, 9, 0, 0);
        _recaps.Available.Add(Year2026);
        _recaps.Available.Add(December);
        var automation = Automation();

        await automation.Sync();
        await automation.Sync();

        var posted = Assert.Single(_board.Posted);
        Assert.Equal("Il tuo 2026 è pronto", posted.Title);
        Assert.Equal("recap-year-2026", _settings.LastHandledKey);
    }

    [Fact]
    public async Task Fall_back_to_December_when_the_closed_year_has_no_recap()
    {
        _now = new DateTime(2027, 1, 1, 9, 0, 0);
        _recaps.Available.Add(December);
        var automation = Automation();

        await automation.Sync();
        await automation.Sync();

        var posted = Assert.Single(_board.Posted);
        Assert.Equal("Il tuo dicembre è pronto", posted.Title);
        Assert.Equal("recap-year-2026", _settings.LastHandledKey);
    }

    [Fact]
    public async Task Post_nothing_on_1_January_when_neither_the_year_nor_December_has_a_recap()
    {
        _now = new DateTime(2027, 1, 1, 9, 0, 0);

        await Automation().Sync();

        Assert.Empty(_board.Posted);
        Assert.Equal("recap-year-2026", _settings.LastHandledKey);
    }

    [Fact]
    public async Task Write_the_state_then_sync_when_choosing()
    {
        _settings.State = RecapNoticeState.Off;
        _recaps.Available.Add(November);

        await Automation().Choose(RecapNoticeState.On);

        Assert.Equal(RecapNoticeState.On, _settings.State);
        Assert.Single(_board.Posted);
        Assert.Single(_board.Armed);
    }

    [Fact]
    public async Task Cancel_the_alarm_when_choosing_off()
    {
        await Automation().Choose(RecapNoticeState.Off);

        Assert.Equal(RecapNoticeState.Off, _settings.State);
        Assert.Empty(_board.Armed);
        Assert.Equal(1, _board.Disarmed);
    }
}
