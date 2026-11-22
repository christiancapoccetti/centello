namespace Centello.Application.Tests;

public sealed class MonthWidgetShapes_should_expected_behavior
{
    // The core is the month, the total and the budget sentence of two lines.
    private static readonly MonthWidgetRows Rows = new(Core: 95, Chip: 24, Line: 10, AlertOneLine: 15, AlertFull: 30);

    [Fact]
    public void Should_take_the_compact_shape_on_a_two_by_two_of_a_small_phone() =>
        Assert.Equal(MonthWidgetShape.Compact, MonthWidgetShapes.Of(124, 151));

    [Fact]
    public void Should_take_the_wide_shape_when_the_launcher_gives_three_columns() =>
        Assert.Equal(MonthWidgetShape.Wide, MonthWidgetShapes.Of(186, 151));

    [Fact]
    public void Should_take_the_tall_shape_when_the_launcher_gives_three_rows() =>
        Assert.Equal(MonthWidgetShape.Tall, MonthWidgetShapes.Of(124, 226));

    [Fact]
    public void Should_take_the_roomy_shape_on_a_three_by_three() =>
        Assert.Equal(MonthWidgetShape.Roomy, MonthWidgetShapes.Of(186, 226));

    [Fact]
    public void Should_switch_to_wide_and_tall_at_their_minimum_sizes()
    {
        Assert.Equal(MonthWidgetShape.Compact, MonthWidgetShapes.Of(MonthWidgetShapes.WideMinWidthDp - 1, MonthWidgetShapes.TallMinHeightDp - 1));
        Assert.Equal(MonthWidgetShape.Wide, MonthWidgetShapes.Of(MonthWidgetShapes.WideMinWidthDp, MonthWidgetShapes.TallMinHeightDp - 1));
        Assert.Equal(MonthWidgetShape.Tall, MonthWidgetShapes.Of(MonthWidgetShapes.WideMinWidthDp - 1, MonthWidgetShapes.TallMinHeightDp));
    }

    [Fact]
    public void Should_pad_the_compact_card_less_than_the_others_and_draw_its_total_smaller()
    {
        var compact = MonthWidgetShapes.StyleOf(MonthWidgetShape.Compact);
        var roomy = MonthWidgetShapes.StyleOf(MonthWidgetShape.Roomy);

        Assert.True(compact.PaddingDp < roomy.PaddingDp);
        Assert.True(compact.TotalDp < roomy.TotalDp);
        Assert.True(compact.EuroDp < roomy.EuroDp);
    }

    [Fact]
    public void Should_draw_a_wider_card_with_a_larger_total_than_a_narrow_one() =>
        Assert.True(MonthWidgetShapes.StyleOf(MonthWidgetShape.Wide).TotalDp > MonthWidgetShapes.StyleOf(MonthWidgetShape.Tall).TotalDp);

    [Fact]
    public void Should_leave_the_rows_the_height_less_margin_and_padding()
    {
        var style = MonthWidgetShapes.StyleOf(MonthWidgetShape.Compact);

        Assert.Equal(151 - 2 * (8 + style.PaddingDp), MonthWidgetShapes.RowsHeightDp(151, style));
    }


    [Fact]
    public void Should_show_every_row_and_the_alert_in_full_when_there_is_room() =>
        Assert.Equal(new MonthWidgetFit(true, true, MonthAlertFit.Full), MonthWidgetShapes.Fit(400, Rows));

    [Fact]
    public void Should_drop_the_chip_first_so_the_alert_keeps_its_whole_sentence() =>
        Assert.Equal(new MonthWidgetFit(false, true, MonthAlertFit.Full), MonthWidgetShapes.Fit(95 + 16 + 7 + 30, Rows));

    [Fact]
    public void Should_cut_the_alert_to_one_line_when_the_whole_sentence_does_not_fit_without_the_chip() =>
        Assert.Equal(new MonthWidgetFit(false, true, MonthAlertFit.OneLine), MonthWidgetShapes.Fit(95 + 16 + 7 + 15, Rows));

    [Fact]
    public void Should_drop_the_category_line_after_the_chip_and_the_full_alert() =>
        Assert.Equal(new MonthWidgetFit(false, false, MonthAlertFit.OneLine), MonthWidgetShapes.Fit(95 + 7 + 15, Rows));

    [Fact]
    public void Should_keep_the_line_and_drop_the_alert_when_only_one_of_them_fits() =>
        Assert.Equal(new MonthWidgetFit(false, true, MonthAlertFit.Hidden), MonthWidgetShapes.Fit(95 + 16, Rows));

    [Fact]
    public void Should_keep_only_the_core_when_nothing_else_fits() =>
        Assert.Equal(new MonthWidgetFit(false, false, MonthAlertFit.Hidden), MonthWidgetShapes.Fit(95, Rows));

    [Fact]
    public void Should_keep_the_core_even_when_it_does_not_fit() =>
        Assert.Equal(new MonthWidgetFit(false, false, MonthAlertFit.Hidden), MonthWidgetShapes.Fit(50, Rows));

    [Fact]
    public void Should_never_choose_a_full_alert_that_cannot_be_drawn_in_the_width() =>
        Assert.Equal(new MonthWidgetFit(true, true, MonthAlertFit.OneLine),
            MonthWidgetShapes.Fit(400, Rows with { AlertFull = float.PositiveInfinity }));

    [Fact]
    public void Should_not_show_the_rows_the_data_does_not_have() =>
        Assert.Equal(new MonthWidgetFit(false, false, MonthAlertFit.Hidden),
            MonthWidgetShapes.Fit(400, new MonthWidgetRows(95, 0, 0, 0, 0)));

    [Fact]
    public void Should_show_the_alert_in_full_without_a_chip_when_the_data_has_none() =>
        Assert.Equal(new MonthWidgetFit(false, true, MonthAlertFit.Full), MonthWidgetShapes.Fit(400, Rows with { Chip = 0 }));

    [Fact]
    public void Should_drop_the_chip_before_the_line_when_there_is_no_alert() =>
        Assert.Equal(new MonthWidgetFit(false, true, MonthAlertFit.Hidden),
            MonthWidgetShapes.Fit(95 + 16, Rows with { AlertOneLine = 0, AlertFull = 0 }));
}
