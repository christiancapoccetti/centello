namespace Centesimo.Application;

public sealed record ExpenseSpeechDraft(
    long? AmountCents,
    Guid? CategoryId,
    Guid? TagId,
    DateOnly? OccurredOn,
    string Note,
    string Transcription);
