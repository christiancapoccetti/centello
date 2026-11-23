namespace Centello.Application;

public sealed record CatalogTag(string Key, DraftTag Tag);

public sealed record CatalogCategory(string Key, DraftCategory Category, IReadOnlyList<CatalogTag> Tags)
{
    public CatalogTag? FindTag(string? key) => Tags.FirstOrDefault(tag => tag.Key == key);
}

/// <summary>
/// Gives each category and tag a short stable key (c1..cN, t1..tM). The language model only ever sees
/// and returns these keys, so ids and names it might invent can never reach a draft.
/// </summary>
public sealed class ExpenseInterpretationCatalog
{
    public ExpenseInterpretationCatalog(IReadOnlyCollection<DraftCategory> categories)
    {
        var tagNumber = 0;
        var items = new List<CatalogCategory>();
        foreach (var category in categories)
        {
            var tags = new List<CatalogTag>();
            foreach (var tag in category.Tags)
                tags.Add(new CatalogTag($"t{++tagNumber}", tag));

            items.Add(new CatalogCategory($"c{items.Count + 1}", category, tags));
        }

        Categories = items;
        PromptLines = [.. items.SelectMany(ToPromptLines)];
    }

    public IReadOnlyList<CatalogCategory> Categories { get; }

    /// <summary>One line per category (<c>c1: Auto</c>) followed by one indented line per tag (<c>  t1: Carburante</c>).</summary>
    public IReadOnlyList<string> PromptLines { get; }

    public CatalogCategory? FindCategory(string? key) => Categories.FirstOrDefault(category => category.Key == key);

    private static IEnumerable<string> ToPromptLines(CatalogCategory category) =>
        category.Tags
            .Select(tag => $"  {tag.Key}: {tag.Tag.Name}")
            .Prepend($"{category.Key}: {category.Category.Name}");
}
