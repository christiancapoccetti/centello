using Centello.Domain;

namespace Centello.Application;

public sealed class TagService(ICategoryRepository categories, ITagRepository tags)
{
    public Task<Result<IReadOnlyList<Tag>>> GetActive(Guid categoryId,
        CancellationToken cancellationToken = default) => GetByState(categoryId, false, cancellationToken);

    public Task<Result<IReadOnlyList<Tag>>> GetArchived(Guid categoryId,
        CancellationToken cancellationToken = default) => GetByState(categoryId, true, cancellationToken);

    public async Task<Result<Tag>> Create(Guid categoryId, string name,
        CancellationToken cancellationToken = default)
    {
        var found = await categories.Get(categoryId, cancellationToken);
        if (found.IsFailure)
            return Result<Tag>.Failure(found.Error);

        if (found.Value is null)
            return Result<Tag>.Failure(ApplicationErrors.CategoryNotFound);

        if (found.Value.IsArchived)
            return Result<Tag>.Failure(ApplicationErrors.CategoryArchived);

        if (name.IsEmpty())
            return Result<Tag>.Failure(ApplicationErrors.InvalidName);

        var tag = new Tag(Guid.NewGuid(), categoryId, name);
        var saved = await tags.Add(tag, cancellationToken);
        return saved.IsFailure ? Result<Tag>.Failure(saved.Error) : Result<Tag>.Success(tag);
    }

    public Task<Result> Archive(Guid tagId, CancellationToken cancellationToken = default) =>
        Change(tagId, tag => tag.Archive(), cancellationToken);

    public Task<Result> Restore(Guid tagId, CancellationToken cancellationToken = default) =>
        Change(tagId, tag => tag.Restore(), cancellationToken);

    private async Task<Result<IReadOnlyList<Tag>>> GetByState(Guid categoryId, bool archived,
        CancellationToken cancellationToken)
    {
        var result = await tags.GetByCategory(categoryId, cancellationToken);
        if (result.IsFailure)
            return Result<IReadOnlyList<Tag>>.Failure(result.Error);

        var matching = result.Value
            .Where(tag => tag.IsArchived == archived)
            .OrderBy(tag => tag.Name)
            .ToList();
        return Result<IReadOnlyList<Tag>>.Success(matching);
    }

    private async Task<Result> Change(Guid tagId, Action<Tag> change, CancellationToken cancellationToken)
    {
        var found = await tags.Get(tagId, cancellationToken);
        if (found.IsFailure)
            return Result.Failure(found.Error);

        if (found.Value is null)
            return Result.Failure(ApplicationErrors.TagNotFound);

        change(found.Value);
        return await tags.Update(found.Value, cancellationToken);
    }
}
