using Centello.Domain;

namespace Centello.Application;

public interface ICategoryRepository
{
    /// <summary>Stores the category together with its first tags in one unit: a failure leaves neither behind.</summary>
    Task<Result> Add(Category category, IReadOnlyList<Tag> tags, CancellationToken cancellationToken = default);
    Task<Result<Category?>> Get(Guid categoryId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<Category>>> GetAll(CancellationToken cancellationToken = default);
    Task<Result> Update(Category category, CancellationToken cancellationToken = default);
}
