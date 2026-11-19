using System.Text;
using Centesimo.Application;
using Centesimo.Domain;

namespace Centesimo.Infrastructure;

public sealed class OpenAiExpenseTextInterpreter(OpenAiClient client) : IExpenseTextInterpreter
{
    public async Task<Result<ExpenseSpeechDraft>> Interpret(
        string text,
        IReadOnlyCollection<SpeechCategory> categories,
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        if (text.IsEmpty())
            return Result<ExpenseSpeechDraft>.Failure(SpeechErrors.EmptyTranscription);

        var catalog = new ExpenseInterpretationCatalog(categories);
        var request = ExpenseInterpretationRequestBuilder.Build(text, catalog, today);
        using var content = new StringContent(request.ToJsonString(), Encoding.UTF8, "application/json");
        var response = await client.Post("responses", content, cancellationToken);
        if (response.IsFailure)
            return Result<ExpenseSpeechDraft>.Failure(response.Error);

        var interpretation = ExpenseInterpretationResponseParser.Parse(response.Value);
        if (interpretation.IsFailure)
            return Result<ExpenseSpeechDraft>.Failure(interpretation.Error);

        return Result<ExpenseSpeechDraft>.Success(
            ExpenseInterpretationResolver.Resolve(interpretation.Value, catalog, today, text));
    }
}
