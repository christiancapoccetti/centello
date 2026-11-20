namespace Centesimo.Application;

public interface IExpenseInterpreter
{
    Task<Result<ExpenseDraft>> Interpret(
        string phrase,
        IReadOnlyCollection<DraftCategory> categories,
        DateOnly today,
        CancellationToken cancellationToken = default);

    /// <returns>A draft with every field empty when the photo shows no readable expense. That is not a failure: the
    /// editor opens empty with today's date.</returns>
    Task<Result<ExpenseDraft>> Interpret(
        ExpensePhoto photo,
        IReadOnlyCollection<DraftCategory> categories,
        DateOnly today,
        CancellationToken cancellationToken = default);
}
