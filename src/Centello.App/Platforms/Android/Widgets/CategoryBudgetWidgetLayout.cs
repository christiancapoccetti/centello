namespace Centello.App;

internal static class CategoryBudgetWidgetLayout
{
    private const int CompactRowHeightDp = 38;
    private const int RoomyRowHeightDp = 48;
    private const int CompactRowLimit = 3;

    public static int RowCapacity(int availableHeightDp)
    {
        var height = Math.Max(0, availableHeightDp);
        var compactCapacity = Math.Min(CompactRowLimit, height / CompactRowHeightDp);
        return Math.Max(compactCapacity, height / RoomyRowHeightDp);
    }
}
