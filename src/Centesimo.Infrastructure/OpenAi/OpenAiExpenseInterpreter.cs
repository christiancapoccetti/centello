using System.Text;
using System.Text.Json.Nodes;
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
        return await Send(
            ExpenseInterpretationRequestBuilder.Build(phrase, catalog, today),
            catalog,
            today,
            DraftSource.Voice,
            phrase,
            cancellationToken);
    }

    public Task<Result<ExpenseDraft>> Interpret(
        ExpensePhoto photo,
        IReadOnlyCollection<DraftCategory> categories,
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        var catalog = new ExpenseInterpretationCatalog(categories);
        return Send(
            ExpenseInterpretationRequestBuilder.Build(photo, catalog, today),
            catalog,
            today,
            DraftSource.Photo,
            "",
            cancellationToken);
    }

    private async Task<Result<ExpenseDraft>> Send(
        JsonObject request,
        ExpenseInterpretationCatalog catalog,
        DateOnly today,
        DraftSource source,
        string heard,
        CancellationToken cancellationToken)
    {
        using var content = new StringContent(request.ToJsonString(), Encoding.UTF8, "application/json");
        var response = await client.Post("responses", content, cancellationToken);
        if (response.IsFailure)
            return Result<ExpenseDraft>.Failure(response.Error);

        var interpretation = ExpenseInterpretationResponseParser.Parse(response.Value);
        if (interpretation.IsFailure)
            return Result<ExpenseDraft>.Failure(interpretation.Error);

        return Result<ExpenseDraft>.Success(
            ExpenseInterpretationResolver.Resolve(interpretation.Value, catalog, today, source, heard));
    }
}
