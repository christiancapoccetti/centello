using Centesimo.Domain;

namespace Centesimo.Application;

/// <summary>Category and tag lookups for cards. An archived category keeps its stored name and colour; one that is
/// gone resolves to the grey fallback, so an expense never loses its line.</summary>
internal sealed class RecapCatalog(IReadOnlyList<Category> categories, IReadOnlyList<Tag> tags)
{
    private static readonly RecapCategory Unknown = new(Guid.Empty, CategoryIcons.Fallback.Label,
        CategoryIcons.Fallback.Key, CategoryPalette.Fallback.Key);

    private readonly Dictionary<Guid, RecapCategory> _categories = categories.ToDictionary(
        category => category.CategoryId,
        category => new RecapCategory(category.CategoryId, category.Name, category.Icon, category.Color));

    private readonly Dictionary<Guid, Tag> _tags = tags.ToDictionary(tag => tag.TagId);

    public RecapCategory Category(Guid categoryId) =>
        _categories.TryGetValue(categoryId, out var category) ? category : Unknown with { CategoryId = categoryId };

    public bool Knows(Guid categoryId) => _categories.ContainsKey(categoryId);

    public Tag? Tag(Guid tagId) => _tags.GetValueOrDefault(tagId);
}
