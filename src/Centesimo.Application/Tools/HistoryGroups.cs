using Centesimo.Domain;

namespace Centesimo.Application;

/// <summary>Expenses of one day (month periods) or one month (year periods). Total is "27,00 €".</summary>
public sealed record HistoryGroup(string Label, string Total, IReadOnlyList<HistoryRow> Rows);

/// <summary>Detail is "Pizza · automatica" in a category and "Da Mario · automatica" across categories. It is empty when
/// nothing else is known.</summary>
public sealed record HistoryRow(Guid ExpenseId, string Title, string Detail, string Amount, string IconKey, string ColorKey)
{
    public string Description => RowLabels.Description(Title, Detail, Amount);
}

/// <summary>The grouping behind every expense list: the history and the category detail share it, so a row reads the
/// same everywhere.</summary>
internal static class HistoryGroups
{
    /// <summary>Newest group first and, within a group, newest expense first. A month period groups by day and a year
    /// period by month. Archived or missing categories and tags still name their rows. A list across categories titles a
    /// row with its category; the list of one category passes <paramref name="acrossCategories"/> false and titles a row
    /// with its note. A year period lists its days, because its groups are months.</summary>
    public static IReadOnlyList<HistoryGroup> Build(Period period, DateOnly today, IEnumerable<Expense> expenses,
        IReadOnlyList<Category> categories, IReadOnlyList<Tag> tags, bool acrossCategories = true)
    {
        var categoryById = categories.ToDictionary(category => category.CategoryId);
        var tagById = tags.ToDictionary(tag => tag.TagId);
        var byDay = period.Kind == PeriodKind.Month;
        return expenses
            .OrderByDescending(expense => expense.OccurredOn)
            .ThenByDescending(expense => expense.ExpenseId)
            .GroupBy(expense => byDay ? expense.OccurredOn : new DateOnly(expense.OccurredOn.Year, expense.OccurredOn.Month, 1))
            .Select(group => new HistoryGroup(
                byDay ? ItalianFormat.DayHeader(group.Key, today) : ItalianFormat.MonthTitle(group.Key.Month),
                ItalianFormat.Money(group.Sum(expense => expense.Amount.Cents)),
                group.Select(expense => ToRow(expense, categoryById.GetValueOrDefault(expense.CategoryId),
                    expense.TagId.HasValue ? tagById.GetValueOrDefault(expense.TagId.Value) : null,
                    byDay ? "" : ItalianFormat.RelativeDay(expense.OccurredOn, today), acrossCategories)).ToList()))
            .ToList();
    }

    /// <summary>"Nessuna spesa a novembre", or "Nessuna spesa in Ristoranti nel 2026" when a category is given.
    /// An empty <paramref name="categoryName"/> means every category.</summary>
    public static string EmptyText(Period period, string categoryName, TagFilter tag, IReadOnlyList<Tag> tags)
    {
        var scope = categoryName.HasValue() ? $" in {categoryName}{TagScope(tag, tags)}" : "";
        return $"Nessuna spesa{scope} {ItalianFormat.InPeriod(period)}";
    }

    private static string TagScope(TagFilter tag, IReadOnlyList<Tag> tags) => tag switch
    {
        TagFilter.UntaggedOnly => " · Senza tag",
        TagFilter.SingleTag single when tags.FirstOrDefault(item => item.TagId == single.TagId) is { } found => $" · {found.Name}",
        _ => ""
    };

    private static HistoryRow ToRow(Expense expense, Category? category, Tag? tag, string day, bool acrossCategories)
    {
        var (title, detail) = acrossCategories
            ? RowLabels.OfExpense(day, expense, category, tag)
            : RowLabels.InCategory(expense, category, tag);
        return new HistoryRow(expense.ExpenseId, title, detail, ItalianFormat.Money(expense.Amount.Cents),
            CategoryIcons.Resolve(category?.Icon).Key, CategoryPalette.Resolve(category?.Color).Key);
    }
}
