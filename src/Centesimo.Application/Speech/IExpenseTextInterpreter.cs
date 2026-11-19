namespace Centesimo.Application;

public interface IExpenseTextInterpreter
{
    Task<Result<ExpenseSpeechDraft>> Interpret(
        string text,
        IReadOnlyCollection<SpeechCategory> categories,
        DateOnly today,
        CancellationToken cancellationToken = default);
}
