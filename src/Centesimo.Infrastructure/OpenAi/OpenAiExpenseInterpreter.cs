using System.Text;
using Centesimo.Application;
using Centesimo.Domain;

namespace Centesimo.Infrastructure;

public sealed class OpenAiExpenseInterpreter(OpenAiClient client) : IExpenseInterpreter
{
    public async Task<Result<ExpenseDraft>> Interpret(
        string phrase,
        IReadOnlyCollection<DraftCategory> categories,
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        if (phrase.IsEmpty())
            return Result<ExpenseDraft>.Failure(SpeechErrors.EmptyTranscription);

        var catalog = new ExpenseInterpretationCatalog(categories);
        var request = ExpenseInterpretationRequestBuilder.Build(phrase, catalog, today);
        using var content = new StringContent(request.ToJsonString(), Encoding.UTF8, "application/json");
        var response = await client.Post("responses", content, cancellationToken);
        if (response.IsFailure)
            return Result<ExpenseDraft>.Failure(response.Error);

        var interpretation = ExpenseInterpretationResponseParser.Parse(response.Value);
        if (interpretation.IsFailure)
            return Result<ExpenseDraft>.Failure(interpretation.Error);

        return Result<ExpenseDraft>.Success(
            ExpenseInterpretationResolver.Resolve(interpretation.Value, catalog, today, DraftSource.Voice, phrase));
    }
}
