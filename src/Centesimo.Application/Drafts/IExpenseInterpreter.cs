namespace Centesimo.Application;

public interface IExpenseInterpreter
{
    Task<Result<ExpenseDraft>> Interpret(
        string phrase,
        IReadOnlyCollection<DraftCategory> categories,
        DateOnly today,
        CancellationToken cancellationToken = default);
}
