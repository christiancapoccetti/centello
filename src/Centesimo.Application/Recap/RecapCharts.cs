namespace Centesimo.Application;

/// <summary>How one bar is drawn. The App resolves each against the card's surface: on a palette surface Strong is
/// tile ink and Bar is tile ink at 30%; on the ground they are Ink and ChartBar.</summary>
public enum RecapBarKind { Bar, Strong, Outlined, Stub, Palette }

/// <summary>Height is 0..1 of the chart's scale. Value sits above the bar and Label under it, both bold when the bar
/// is Strong or Outlined. ColorKey is read only by Palette bars.</summary>
public sealed record RecapBar(double Height, RecapBarKind Kind, string Value = "", string Label = "", string ColorKey = "");

/// <summary>Days of a month, months of a year, weekdays, 52 weeks, tag ticks and the cumulative year. Previous is the
/// dashed ink step line (one level per slot, 0..1 of the same scale), empty when there is none.</summary>
public sealed record RecapBars(IReadOnlyList<RecapBar> Bars, IReadOnlyList<double> Previous, string Description);

public enum RecapCell { Filled, Empty, None }

/// <summary>The month calendar (7 columns, Monday first, "L M M G V S D", leading None) and the year heatmap
/// (31 columns, one row per month, "G F M ...", None past a month's last day or past Through).</summary>
public sealed record RecapGrid(int Columns, IReadOnlyList<string> ColumnLabels, IReadOnlyList<string> RowLabels,
    IReadOnlyList<RecapCell> Cells, string Description);

/// <summary>One square per small expense in its category's ink, largest category first. The App fits them with
/// <see cref="UnitGrid.Fit"/>.</summary>
public sealed record RecapUnits(IReadOnlyList<string> ColorKeys, string Description);
