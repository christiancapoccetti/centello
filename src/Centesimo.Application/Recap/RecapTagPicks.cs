namespace Centesimo.Application;

/// <summary>Which tags a card is about, and how a tag card paints its bars on its surface. The picks answer for the
/// card that shows them: a tag whose card is hidden is not picked.</summary>
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

    /// <summary>The most used tag, when it was used often enough.</summary>
    public static TagUse? TagOfYear(RecapScope scope) =>
        scope.TagUses.FirstOrDefault() is { } use && use.Count >= RecapRules.MinTagOfYearUses ? use : null;

    /// <summary>The tag with the most euros, when it was used often enough and is not already the tag of the year.</summary>
    public static TagUse? Costliest(RecapScope scope)
    {
        var dearest = scope.TagUses.MaxBy(use => use.Cents);
        return dearest is not null && dearest.Count >= RecapRules.MinTagOfYearUses
            && dearest.Tag.TagId != TagOfYear(scope)?.Tag.TagId
                ? dearest
                : null;
    }

    /// <summary>The uses of a tag in each month of the year, as tile-ink bars.</summary>
    public static RecapBars MonthBars(TagUse use)
    {
        var surface = RecapSurface.ForCategory(use.Category.ColorKey);
        var perMonth = use.Expenses
            .GroupBy(expense => expense.OccurredOn.Month)
            .ToDictionary(month => month.Key, month => month.Count());
        var peak = perMonth.Values.Max();
        var bars = Enumerable.Range(1, 12)
            .Select(month => perMonth.GetValueOrDefault(month) is var count and > 0
                ? InkBar(surface, use.Category, (double)count / peak, label: ItalianFormat.MonthInitial(month))
                : new RecapBar(0, RecapBarKind.Stub, Label: ItalianFormat.MonthInitial(month)))
            .ToList();
        var range = perMonth.Values.Min() == peak ? $"{peak} volte" : $"da {perMonth.Values.Min()} a {peak} volte";
        return new RecapBars(bars, [], $"{use.Tag.Name} mese per mese, {range}");
    }

    /// <summary>A bar the tag's category is shown in: tile ink on a palette surface, the category's own ink on the
    /// ground, where a coral category stays.</summary>
    public static RecapBar InkBar(RecapSurface surface, RecapCategory category, double height, string value = "", string label = "") =>
        surface.IsGround
            ? new RecapBar(height, RecapBarKind.Palette, value, label, CategoryPalette.Resolve(category.ColorKey).Key)
            : new RecapBar(height, RecapBarKind.Strong, value, label);
}
