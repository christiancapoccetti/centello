namespace Centello.Application;

/// <summary>The shape Il mese takes at a given size. The launcher sets how many dp a cell is: two columns are 124 dp on
/// a 360 dp phone and 173 dp on a 411 dp one, so the shape follows the size the launcher reports and not the cell
/// count it advertises.</summary>
public enum MonthWidgetShape
{
    /// <summary>The 2x2: the least room, so the tightest padding and the smallest total.</summary>
    Compact,
    /// <summary>The 3x2: a wider card, so a larger total; as short as the 2x2, so the same tight padding.</summary>
    Wide,
    /// <summary>The 2x3: a taller card, so room for every row.</summary>
    Tall,
    /// <summary>The 3x3: wide and tall.</summary>
    Roomy
}

/// <summary>What a shape sets: the padding inside the card and the sizes of the total and its euro sign, in dp.</summary>
public readonly record struct MonthWidgetStyle(float PaddingDp, float TotalDp, float EuroDp);

/// <summary>How the alert of the category that is over budget is drawn, when a row is left for it.</summary>
public enum MonthAlertFit
{
    /// <summary>No room for it.</summary>
    Hidden,
    /// <summary>On one line, with an ellipsis where it does not fit.</summary>
    OneLine,
    /// <summary>In full, wrapped over as many lines as it needs.</summary>
    Full
}

/// <summary>The heights in dp of the rows of Il mese as they are drawn, and zero for a row the data does not have.
/// <see cref="Core"/> is the month, the total and the budget sentence with the gaps between them: what the widget cannot
/// do without.</summary>
public readonly record struct MonthWidgetRows(float Core, float Chip, float Line, float AlertOneLine, float AlertFull);

/// <summary>Which of the optional rows of Il mese fit under the core of the card.</summary>
public readonly record struct MonthWidgetFit(bool ShowChip, bool ShowLine, MonthAlertFit Alert);

public static class MonthWidgetShapes
{
    // The gaps the optional rows keep above them in widget_month.xml: keep them in step.
    private const float ChipGapDp = 6;
    private const float LineGapDp = 6;
    private const float AlertGapDp = 7;
    private const float OuterMarginDp = 8;

    /// <summary>The narrowest widget that is wide: between two and three columns of the cells launchers draw.</summary>
    public const int WideMinWidthDp = 180;

    /// <summary>The shortest widget that is tall.</summary>
    public const int TallMinHeightDp = 200;

    public static MonthWidgetShape Of(int widthDp, int heightDp) => (widthDp >= WideMinWidthDp, heightDp >= TallMinHeightDp) switch
    {
        (true, true) => MonthWidgetShape.Roomy,
        (true, false) => MonthWidgetShape.Wide,
        (false, true) => MonthWidgetShape.Tall,
        _ => MonthWidgetShape.Compact
    };

    public static MonthWidgetStyle StyleOf(MonthWidgetShape shape) => shape switch
    {
        MonthWidgetShape.Compact => new MonthWidgetStyle(12, 38, 22),
        MonthWidgetShape.Wide => new MonthWidgetStyle(12, 46, 26),
        MonthWidgetShape.Tall => new MonthWidgetStyle(16, 40, 24),
        _ => new MonthWidgetStyle(16, 48, 28)
    };

    /// <summary>The height the card leaves for its rows: what the launcher gives, less the margin and the padding.</summary>
    public static float RowsHeightDp(int heightDp, MonthWidgetStyle style) =>
        heightDp - 2 * (OuterMarginDp + style.PaddingDp);

    /// <summary>The richest set of rows whose heights fit <paramref name="availableDp"/>. The chip goes first, so the
    /// alert keeps its whole sentence; then the alert shrinks to one line, then the category line goes and, last, the
    /// alert; the core is always kept, since a card that shows less than the month, its total and its budget says
    /// nothing. A row that cannot be drawn in the width, such as a full alert with a word wider than the card, has an
    /// infinite height and is never chosen.</summary>
    public static MonthWidgetFit Fit(float availableDp, MonthWidgetRows rows)
    {
        var candidates = new (MonthWidgetFit Fit, float Height)[]
        {
            Candidate(rows, true, true, MonthAlertFit.Full),
            Candidate(rows, false, true, MonthAlertFit.Full),
            Candidate(rows, true, true, MonthAlertFit.OneLine),
            Candidate(rows, false, true, MonthAlertFit.OneLine),
            Candidate(rows, false, false, MonthAlertFit.OneLine),
            Candidate(rows, false, true, MonthAlertFit.Hidden)
        };
        foreach (var (fit, height) in candidates)
            if (height <= availableDp)
                return fit;

        return new MonthWidgetFit(false, false, MonthAlertFit.Hidden);
    }

    private static (MonthWidgetFit, float) Candidate(MonthWidgetRows rows, bool chip, bool line, MonthAlertFit alert)
    {
        var alertDp = alert switch
        {
            MonthAlertFit.Full => rows.AlertFull,
            MonthAlertFit.OneLine => rows.AlertOneLine,
            _ => 0
        };
        var height = rows.Core
            + (chip && rows.Chip > 0 ? ChipGapDp + rows.Chip : 0)
            + (line && rows.Line > 0 ? LineGapDp + rows.Line : 0)
            + (alertDp > 0 ? AlertGapDp + alertDp : 0);
        return (new MonthWidgetFit(chip && rows.Chip > 0, line && rows.Line > 0, alertDp > 0 ? alert : MonthAlertFit.Hidden), height);
    }
}
