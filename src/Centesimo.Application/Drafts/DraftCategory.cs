namespace Centesimo.Application;

public sealed record DraftCategory(Guid CategoryId, string Name, IReadOnlyCollection<DraftTag> Tags);

public sealed record DraftTag(Guid TagId, string Name);
