namespace Centesimo.Application;

public sealed record ExpenseDraft(
    long? AmountCents,
    Guid? CategoryId,
    Guid? TagId,
    DateOnly? OccurredOn,
    string Note,
    string Heard);
