namespace Centesimo.Application;

/// <summary>Which tags a card is about, and how a tag card paints its bars on its surface.</summary>
internal static class RecapTagPicks
{
    /// <summary>Tags with enough variable uses in the span and none in the previous period: most used first, at most
    /// as many as the period kind shows.</summary>
    public static IReadOnlyList<TagUse> NewHabits(RecapScope scope)
    {
        var before = scope.Spending.PreviousWhole
            .Where(expense => !expense.IsAutomatic && expense.TagId.HasValue)
            .Select(expense => expense.TagId!.Value)
            .ToHashSet();
        return scope.TagUses
            .Where(use => use.Count >= scope.Rules.MinHabitUses && !before.Contains(use.Tag.TagId))
            .Take(scope.Rules.MaxHabits)
            .ToList();
    }

    /// <summary>A bar the tag's category is shown in: tile ink on a palette surface, the category's own ink on the
    /// ground, where a coral category stays.</summary>
    public static RecapBar InkBar(RecapSurface surface, RecapCategory category, double height, string value = "", string label = "") =>
        surface.IsGround
            ? new RecapBar(height, RecapBarKind.Palette, value, label, CategoryPalette.Resolve(category.ColorKey).Key)
            : new RecapBar(height, RecapBarKind.Strong, value, label);
}
