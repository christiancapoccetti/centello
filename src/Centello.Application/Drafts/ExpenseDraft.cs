namespace Centello.Application;

public sealed record ExpenseDraft(
    long? AmountCents,
    Guid? CategoryId,
    Guid? TagId,
    DateOnly? OccurredOn,
    string Note,
    DraftSource Source,
    string Heard)
{
    public string Banner => Source.Banner(Heard);
}
