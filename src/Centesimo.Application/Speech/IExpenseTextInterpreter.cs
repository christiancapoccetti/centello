namespace Centesimo.Application;

public interface IExpenseTextInterpreter
{
    Task<Result<ExpenseDraft>> Interpret(
        string text,
        IReadOnlyCollection<DraftCategory> categories,
        DateOnly today,
        CancellationToken cancellationToken = default);
}
