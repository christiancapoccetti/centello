using Centesimo.Domain;

namespace Centesimo.Application;

public sealed class CategoryService(ICategoryRepository categories)
{
    /// <summary>The category, archived or not, or null when there is none with this id.</summary>
    public Task<Result<Category?>> Get(Guid categoryId, CancellationToken cancellationToken = default) =>
        categories.Get(categoryId, cancellationToken);

    public async Task<Result<IReadOnlyList<Category>>> GetActive(
        CancellationToken cancellationToken = default)
    {
        var result = await categories.GetAll(cancellationToken);
        if (result.IsFailure)
            return Result<IReadOnlyList<Category>>.Failure(result.Error);

        var active = result.Value
            .Where(category => !category.IsArchived)
            .OrderBy(category => category.Name)
            .ToList();
        return Result<IReadOnlyList<Category>>.Success(active);
    }

    public Task<Result<Category>> Create(string name, string icon, string color, Money? budget = null,
        CancellationToken cancellationToken = default) =>
        Create(name, icon, color, budget, [], cancellationToken);

    /// <summary>Creates the category and its tags as one operation. The color is one of the chromatic palette slots,
    /// <see cref="NextColor"/> being the one to suggest. Tag names are trimmed, blank ones are ignored and names that
    /// differ only by case collapse into the first spelling.</summary>
    public async Task<Result<Category>> Create(string name, string icon, string color, Money? budget,
        IReadOnlyList<string> tags, CancellationToken cancellationToken = default)
    {
        if (name.IsEmpty())
            return Result<Category>.Failure(ApplicationErrors.InvalidName);

        if (!IsChromatic(color))
            return Result<Category>.Failure(ApplicationErrors.InvalidColor);

        var existing = await categories.GetAll(cancellationToken);
        if (existing.IsFailure)
            return Result<Category>.Failure(existing.Error);

        var availability = EnsureNameAvailable(existing.Value, name, null);
        if (availability.IsFailure)
            return Result<Category>.Failure(availability.Error);

        var category = new Category(Guid.NewGuid(), name, icon, color, budget);
        var saved = await categories.Add(category, NewTags(category.CategoryId, tags), cancellationToken);
        return saved.IsFailure
            ? Result<Category>.Failure(saved.Error)
            : Result<Category>.Success(category);
    }

    /// <summary>The color to suggest for the next category: the palette slot the fewest active categories use, the
    /// first in palette order among the ones tied.</summary>
    public async Task<Result<string>> NextColor(CancellationToken cancellationToken = default)
    {
        var existing = await categories.GetAll(cancellationToken);
        if (existing.IsFailure)
            return Result<string>.Failure(existing.Error);

        var leastUsed = CategoryPalette.LeastUsed(
            existing.Value.Where(category => !category.IsArchived).Select(category => category.Color));
        return Result<string>.Success(leastUsed.Key);
    }

    public Task<Result<IReadOnlyList<Category>>> CreateSuggested(CancellationToken cancellationToken = default) =>
        CreateSuggested(SuggestedCategories.All.Select(suggestion => suggestion.Name).ToList(), cancellationToken);

    /// <summary>Creates the suggested categories named in <paramref name="names"/>, in catalog order, and returns the
    /// ones it created. A name that already belongs to a category, archived or not, is skipped, and so is a name the
    /// catalog does not know, so a second call creates nothing. A failure stops at the category that could not be
    /// stored: the ones before it stay and calling again creates the rest.</summary>
    public async Task<Result<IReadOnlyList<Category>>> CreateSuggested(IReadOnlyList<string> names,
        CancellationToken cancellationToken = default)
    {
        var existing = await categories.GetAll(cancellationToken);
        if (existing.IsFailure)
            return Result<IReadOnlyList<Category>>.Failure(existing.Error);

        var taken = existing.Value
            .Select(category => category.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var created = new List<Category>();
        foreach (var suggestion in SuggestedCategories.All.Where(item => names.Contains(item.Name, StringComparer.OrdinalIgnoreCase)))
        {
            if (!taken.Add(suggestion.Name))
                continue;

            var category = new Category(Guid.NewGuid(), suggestion.Name, suggestion.IconKey, suggestion.ColorKey);
            var saved = await categories.Add(category, [], cancellationToken);
            if (saved.IsFailure)
                return Result<IReadOnlyList<Category>>.Failure(saved.Error);

            created.Add(category);
        }

        return Result<IReadOnlyList<Category>>.Success(created);
    }

    /// <summary>Changes name, icon, color and budget. The color is a chromatic palette slot or the one the category
    /// already has, so a grey or legacy color survives an edit that leaves it alone.</summary>
    public async Task<Result> Update(Guid categoryId, string name, string icon, string color,
        Money? budget, CancellationToken cancellationToken = default)
    {
        if (name.IsEmpty())
            return Result.Failure(ApplicationErrors.InvalidName);

        var found = await categories.Get(categoryId, cancellationToken);
        if (found.IsFailure)
            return Result.Failure(found.Error);

        if (found.Value is null)
            return Result.Failure(ApplicationErrors.CategoryNotFound);

        if (color != found.Value.Color && !IsChromatic(color))
            return Result.Failure(ApplicationErrors.InvalidColor);

        var existing = await categories.GetAll(cancellationToken);
        if (existing.IsFailure)
            return Result.Failure(existing.Error);

        var availability = EnsureNameAvailable(existing.Value, name, categoryId);
        if (availability.IsFailure)
            return availability;

        found.Value.UpdateDetails(name, icon, color, budget);
        return await categories.Update(found.Value, cancellationToken);
    }

    public async Task<Result> Archive(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var found = await categories.Get(categoryId, cancellationToken);
        if (found.IsFailure)
            return Result.Failure(found.Error);

        if (found.Value is null)
            return Result.Failure(ApplicationErrors.CategoryNotFound);

        found.Value.Archive();
        return await categories.Update(found.Value, cancellationToken);
    }

    public async Task<Result> Restore(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var found = await categories.Get(categoryId, cancellationToken);
        if (found.IsFailure)
            return Result.Failure(found.Error);

        if (found.Value is null)
            return Result.Failure(ApplicationErrors.CategoryNotFound);

        found.Value.Restore();
        return await categories.Update(found.Value, cancellationToken);
    }
    private static bool IsChromatic(string color) => CategoryPalette.Chromatic.Any(slot => slot.Key == color);

    private static Result EnsureNameAvailable(IReadOnlyList<Category> existing, string name, Guid? excludedCategoryId)
    {
        var normalizedName = name.Trim();
        var duplicateExists = existing.Any(category =>
            category.CategoryId != excludedCategoryId &&
            category.Name.Equals(normalizedName, StringComparison.OrdinalIgnoreCase));
        return duplicateExists
            ? Result.Failure(ApplicationErrors.CategoryNameAlreadyExists)
            : Result.Success();
    }

    private static List<Tag> NewTags(Guid categoryId, IReadOnlyList<string> names) => names
        .Where(name => name.HasValue())
        .Select(name => name.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Select(name => new Tag(Guid.NewGuid(), categoryId, name))
        .ToList();
}
