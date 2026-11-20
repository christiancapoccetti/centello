using System.Text;
using Centesimo.Application;
using Centesimo.Domain;

namespace Centesimo.Infrastructure;

public sealed class OpenAiExpenseTextInterpreter(OpenAiClient client) : IExpenseTextInterpreter
{
    public async Task<Result<ExpenseDraft>> Interpret(
        string text,
        IReadOnlyCollection<DraftCategory> categories,
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        if (text.IsEmpty())
            return Result<ExpenseDraft>.Failure(SpeechErrors.EmptyTranscription);

        var catalog = new ExpenseInterpretationCatalog(categories);
        var request = ExpenseInterpretationRequestBuilder.Build(text, catalog, today);
        using var content = new StringContent(request.ToJsonString(), Encoding.UTF8, "application/json");
        var response = await client.Post("responses", content, cancellationToken);
        if (response.IsFailure)
            return Result<ExpenseDraft>.Failure(response.Error);

        var interpretation = ExpenseInterpretationResponseParser.Parse(response.Value);
        if (interpretation.IsFailure)
            return Result<ExpenseDraft>.Failure(interpretation.Error);

        return Result<ExpenseDraft>.Success(
            ExpenseInterpretationResolver.Resolve(interpretation.Value, catalog, today, text));
    }
}
