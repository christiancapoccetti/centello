namespace Centello.Application;

internal static class InsightCategories
{
    public static IReadOnlyList<InsightCategory> For(long spent, long? prior, IReadOnlyList<CategorySpend> categories)
    {
        var rows = categories
            .Where(category => category.Spent > 0 || category.Prior > 0)
            .Select(category => Row(category.Category.CategoryId, category.Category.Name,
                category.Category.Icon, category.Category.Color, category.Spent, category.Prior, spent, prior))
            .ToList();

        var otherSpent = spent - rows.Sum(row => row.SpentCents);
        var otherPrior = prior.GetValueOrDefault() - rows.Sum(row => row.ComparedSpentCents.GetValueOrDefault());
        if (otherSpent > 0 || otherPrior > 0)
            rows.Add(Row(null, "Altre categorie", CategoryIcons.Fallback.Key, CategoryPalette.Fallback.Key,
                otherSpent, otherPrior, spent, prior));

        return rows
            .OrderByDescending(row => row.SpentCents)
            .ThenBy(row => row.CategoryId is null)
            .ThenBy(row => row.Name, StringComparer.Ordinal)
            .ThenBy(row => row.CategoryId)
            .ToArray();
    }

    public static InsightComposition Composition(Period period, bool isToDate, IReadOnlyList<InsightCategory> rows)
    {
        var leading = rows
            .Where(row => row.SpentCents > 0)
            .Take(2)
            .ToArray();

        if (leading.Length == 0)
            return new InsightComposition(new BudgetSummary("Nessuna spesa nel periodo selezionato.", "", ""), "");

        var first = leading[0];
        if (leading.Length > 1 && first.SpentCents == leading[1].SpentCents)
            return new InsightComposition(new BudgetSummary("Le voci con la spesa più alta hanno lo stesso importo.", "", ""), "");

        if (first.CategoryId is null)
        {
            var statement = new BudgetSummary("", InsightTexts.Share(first.Share!.Value),
                " della spesa è in categorie archiviate o non disponibili.");
            return new InsightComposition(statement, "");
        }

        var context = first.ComparedShare is { } share
            ? $"{RowLabels.Sentence(InsightTexts.InComparedPeriod(period, isToDate))}, la quota era {ArticleOfShare(share)}."
            : "";
        return new InsightComposition(Statement(first.Name, first.Share!.Value), context);
    }

    private static BudgetSummary Statement(string name, double share) => share switch
    {
        >= 0.645 and < 2d / 3 => new BudgetSummary("Quasi ", "2 € su 3", $" sono andati in {name}."),
        >= 2d / 3 and <= 0.685 => new BudgetSummary("Circa ", "2 € su 3", $" sono andati in {name}."),
        _ => new BudgetSummary("", InsightTexts.Share(share), $" della spesa è andato in {name}.")
    };

    private static string ArticleOfShare(double share) =>
        share == 0.5 ? "la metà" : $"del {InsightTexts.Share(share)}";

    private static InsightCategory Row(Guid? id, string name, string icon, string color, long spent, long prior,
        long total, long? previousTotal) =>
        new(id, name, icon, color, spent, previousTotal is null ? null : prior,
            total > 0 ? (double)spent / total : null,
            previousTotal is > 0 ? (double)prior / previousTotal.Value : null);
}
