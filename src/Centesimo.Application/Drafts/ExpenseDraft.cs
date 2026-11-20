namespace Centesimo.Application;

/// <param name="Heard">The phrase the voice heard. Always empty for a photo.</param>
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
