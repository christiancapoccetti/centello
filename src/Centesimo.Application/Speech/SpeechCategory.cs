namespace Centesimo.Application;

public sealed record SpeechCategory(Guid CategoryId, string Name, IReadOnlyCollection<SpeechTag> Tags);

public sealed record SpeechTag(Guid TagId, string Name);
